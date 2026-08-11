#!/usr/bin/env bash
#
# Censo de erros da extração do núcleo (Etapa 2).
#
# Compila todo o Backend/ legado sob net9.0 e agrega os erros. O objetivo NÃO é fazer
# compilar — é medir o acoplamento à plataforma e servir de barra de progresso da Etapa 2.
#
# DOIS MODOS, porque são dois usos diferentes (DEC-022):
#
#   ./scripts/censo-erros.sh                    modo completo: relatório e total
#   ./scripts/censo-erros.sh --rapido           laço interno: só os primeiros erros
#   ./scripts/censo-erros.sh --rapido Weapon    idem, filtrado por arquivo
#
# O modo completo mede. O modo rápido guia a próxima correção. Confundir os dois faz o
# desenvolvedor esperar 27 s para ler 367 erros quando precisava de 4 s e de 3 erros.
#
# Opções:
#   --rapido [padrão]   não gera relatório; imprime os N primeiros erros, opcionalmente
#                       filtrados por um padrão de caminho de arquivo
#   --limite N          quantos erros o modo rápido imprime (padrão: 15)
#   --limpar            descarta o projeto de sondagem persistente e recomeça do zero
#   --dir CAMINHO       onde manter a sondagem (padrão: ${TMPDIR:-/tmp}/chummer-censo)
#
# Saída do modo completo: <dir>/censo.md e os erros crus em <dir>/erros.txt

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK_DIR="${TMPDIR:-/tmp}/chummer-censo"
PROBE_DIR=""
MODO="completo"
FILTRO=""
LIMITE=15
LIMPAR=0

while [ $# -gt 0 ]; do
    case "$1" in
        --rapido|--fast) MODO="rapido"; shift
            if [ $# -gt 0 ] && [ "${1#--}" = "$1" ]; then FILTRO="$1"; shift; fi ;;
        --limite) LIMITE="$2"; shift 2 ;;
        --limpar|--clean) LIMPAR=1; shift ;;
        --dir) WORK_DIR="$2"; shift 2 ;;
        -h|--help) sed -n '2,28p' "$0"; exit 0 ;;
        *) WORK_DIR="$1"; shift ;;   # compatibilidade: primeiro posicional é o diretório
    esac
done
PROBE_DIR="$WORK_DIR/Probe"

export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

log() { printf '\n\033[1m==> %s\033[0m\n' "$*"; }

[ "$LIMPAR" -eq 1 ] && rm -rf "$PROBE_DIR"
mkdir -p "$PROBE_DIR"

# ---------------------------------------------------------------------------
# Projeto de sondagem — PERSISTENTE
#
# O .csproj é reescrito só quando muda de conteúdo. Isso é o que permite ao dotnet reusar
# obj/ e pular o restore, e é de longe o maior ganho de velocidade: medido, o restore e a
# recriação respondiam por 23 dos 27 segundos de uma execução. A compilação em si leva 4 s.
#
# Alvo net9.0 sem sufixo -windows: é essa ausência que faz o compilador recusar código
# acoplado a WinForms, que é exatamente o que queremos contar.
#
# Os pacotes NuGet incluídos são os que JÁ são portáveis. Os não portáveis
# (HtmlRenderer.WinForms, LiveCharts.WinForms, WkHtmlToPdf, MEF) ficam de fora de
# propósito — os erros que eles geram SÃO o sinal.
# ---------------------------------------------------------------------------
NOVO_CSPROJ="$(cat <<'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>Chummer</RootNamespace>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Ben.Demystifier" Version="0.4.1" />
    <PackageReference Include="Microsoft.Extensions.ObjectPool" Version="10.0.9" />
    <PackageReference Include="Microsoft.IO.RecyclableMemoryStream" Version="3.0.1" />
    <PackageReference Include="Microsoft.VisualStudio.Threading" Version="18.7.23" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageReference Include="NLog" Version="6.1.4" />
    <PackageReference Include="XoshiroPRNG.Net" Version="1.6.0" />
    <PackageReference Include="itext" Version="9.7.0" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="$(ChummerBackend)/**/*.cs" />
    <Compile Include="$(ChummerSevenZip)/**/*.cs" />
    <!--
      Annotations.cs vive em Chummer/Properties/, fora do Backend/, mas define os atributos
      (NotNull, ItemNotNull, ...) usados por todo o Backend. Sem ele, o censo se enche de
      milhares de CS0246 que são artefato da sondagem e não acoplamento de plataforma.
    -->
    <Compile Include="$(ChummerAnnotations)" />
    <!--
      Código já migrado para o Chummer.Core precisa entrar na sondagem. Sem isto, o censo
      compila um conjunto incompleto e reporta erros fantasma para os tipos que saíram do
      Backend/ — medindo regressão onde houve progresso.
    -->
    <Compile Include="$(ChummerCore)/**/*.cs"
             Exclude="$(ChummerCore)/bin/**/*.cs;$(ChummerCore)/obj/**/*.cs" />
  </ItemGroup>
</Project>
CSPROJ
)"
if [ ! -f "$PROBE_DIR/Probe.csproj" ] || [ "$NOVO_CSPROJ" != "$(cat "$PROBE_DIR/Probe.csproj")" ]; then
    printf '%s\n' "$NOVO_CSPROJ" > "$PROBE_DIR/Probe.csproj"
    RESTAURAR=1
else
    RESTAURAR=0
fi

# O build roda a partir de $PROBE_DIR, que fica FORA da árvore do repositório. Isso é
# essencial: o dotnet resolve o global.json pelo diretório de trabalho, e o da raiz fixa o
# SDK 8 do build legado (DEC-012).
cd "$PROBE_DIR"

PROPS=(
    -p:ChummerBackend="$REPO_ROOT/Chummer/Backend"
    -p:ChummerSevenZip="$REPO_ROOT/Chummer/7zip"
    -p:ChummerAnnotations="$REPO_ROOT/Chummer/Properties/Annotations.cs"
    -p:ChummerCore="$REPO_ROOT/src/Chummer.Core"
)

if [ "$RESTAURAR" -eq 1 ]; then
    [ "$MODO" = "completo" ] && log "Restaurando pacotes (primeira execução ou projeto alterado)"
    dotnet restore Probe.csproj "${PROPS[@]}" --nologo -v:q > /dev/null
fi

[ "$MODO" = "completo" ] && log "Compilando o Backend sob net9.0 (espera-se que falhe — esse é o ponto)"
set +e
dotnet build Probe.csproj "${PROPS[@]}" --no-restore --nologo -v:n 2>&1 \
    | tee "$WORK_DIR/build.log" > /dev/null
set -e

# O MSBuild emite cada erro duas vezes, uma com prefixo de nó ("  1>arquivo.cs(...)") e
# outra sem. Normalizar espaço à esquerda E o prefixo de nó é o que faz o sort -u
# deduplicar; sem isso o total sai exatamente dobrado.
sed -E 's/^[[:space:]]+//; s/^[0-9]+>//; s/ \[[^]]*\.csproj\]$//' "$WORK_DIR/build.log" \
    | grep -oE '^[^(]+\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+: .*' \
    | sed "s|$REPO_ROOT/||" | sort -u > "$WORK_DIR/erros.txt" || true

TOTAL=$(wc -l < "$WORK_DIR/erros.txt")

# Guarda contra falso zero.
#
# Se o build nem chegou a compilar — SDK errado, restore falhou, projeto inválido — o log
# não contém erros CS e o censo reportaria "0 erros", indistinguível de "tudo compila".
# Numa ferramenta cujo propósito é medir progresso, esse falso positivo é pior do que não
# medir: faria o porte parecer concluído.
if [ "$TOTAL" -eq 0 ] && ! grep -q "Build succeeded" "$WORK_DIR/build.log"; then
    echo >&2
    echo "ERRO: nenhum erro CS encontrado, mas o build também não teve sucesso." >&2
    echo "A compilação provavelmente nem começou. Últimas linhas do log:" >&2
    echo >&2
    tail -20 "$WORK_DIR/build.log" >&2
    exit 1
fi

# ---------------------------------------------------------------------------
# Modo rápido — guia a próxima correção, não mede
# ---------------------------------------------------------------------------
if [ "$MODO" = "rapido" ]; then
    if [ -n "$FILTRO" ]; then
        SELECAO=$(grep -- "$FILTRO" "$WORK_DIR/erros.txt" || true)
        N=$(printf '%s' "$SELECAO" | grep -c '' || true)
        printf '\n\033[1m%s erros em "%s"  (total no projeto: %s)\033[0m\n\n' "$N" "$FILTRO" "$TOTAL"
    else
        SELECAO=$(cat "$WORK_DIR/erros.txt")
        printf '\n\033[1mTotal: %s erros\033[0m\n\n' "$TOTAL"
    fi
    printf '%s\n' "$SELECAO" | head -n "$LIMITE"
    RESTANTES=$(( $(printf '%s' "$SELECAO" | grep -c '' || true) - LIMITE ))
    [ "$RESTANTES" -gt 0 ] && printf '\n... e mais %s. Use --limite N para ver mais.\n' "$RESTANTES"
    exit 0
fi

# ---------------------------------------------------------------------------
# Modo completo — relatório
# ---------------------------------------------------------------------------
{
    echo "# Censo de erros — Backend sob net9.0"
    echo
    echo "Gerado por \`scripts/censo-erros.sh\` em $(date +%Y-%m-%d)."
    echo
    echo "**Total de erros distintos: $TOTAL**"
    echo
    echo "## Por código de erro"
    echo
    echo '| Código | Ocorrências |'
    echo '|---|---|'
    grep -oE 'error [A-Z]+[0-9]+' "$WORK_DIR/erros.txt" | sed 's/error //' \
        | sort | uniq -c | sort -rn \
        | while read -r n code; do echo "| \`$code\` | $n |"; done
    echo
    echo "## Por arquivo (30 maiores)"
    echo
    echo '| Arquivo | Erros |'
    echo '|---|---|'
    sed 's/(.*//' "$WORK_DIR/erros.txt" | sort | uniq -c | sort -rn | head -30 \
        | while read -r n file; do echo "| \`$file\` | $n |"; done
    echo
    echo "## Tipos e namespaces ausentes mais citados"
    echo
    echo '| Símbolo | Ocorrências |'
    echo '|---|---|'
    grep -oE "name '[A-Za-z0-9_]+'" "$WORK_DIR/erros.txt" | sed "s/name '//;s/'//" \
        | sort | uniq -c | sort -rn | head -25 \
        | while read -r n sym; do echo "| \`$sym\` | $n |"; done
} > "$WORK_DIR/censo.md"

log "Relatório: $WORK_DIR/censo.md   ($TOTAL erros)"
