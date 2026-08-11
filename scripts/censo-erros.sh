#!/usr/bin/env bash
#
# Censo de erros da Etapa 1.2/1.3 do porte.
#
# Arrasta todo o Backend/ legado para dentro de um projeto net9.0 descartável, tenta
# compilar, e agrega os erros por código e por arquivo. O objetivo NÃO é fazer compilar —
# é medir com precisão o tamanho do acoplamento à plataforma, para dimensionar a Etapa 2.
#
# O projeto de sondagem é descartável e vive fora da árvore do repositório, para não
# poluir a solução nem herdar src/Directory.Build.props (cujo TreatWarningsAsErrors
# adicionaria ruído ao censo).
#
# Uso:
#   ./scripts/censo-erros.sh [diretório-de-trabalho]
#
# Saída: relatório em <diretório-de-trabalho>/censo.md e os erros crus em erros.txt

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK_DIR="${1:-${TMPDIR:-/tmp}/chummer-censo}"
PROBE_DIR="$WORK_DIR/Probe"

export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

log() { printf '\n\033[1m==> %s\033[0m\n' "$*"; }

rm -rf "$PROBE_DIR"
mkdir -p "$PROBE_DIR"

# ---------------------------------------------------------------------------
# Projeto de sondagem
#
# Alvo net9.0 sem sufixo -windows: é essa ausência que faz o compilador recusar todo
# código acoplado a WinForms, que é exatamente o que queremos contar.
#
# Os pacotes NuGet incluídos são os que JÁ são portáveis. Sem eles, o censo se encheria
# de erros de "tipo não encontrado" que são ruído de dependência, e não acoplamento de
# plataforma. Os pacotes não portáveis (HtmlRenderer.WinForms, LiveCharts.WinForms,
# WkHtmlToPdf, MEF) ficam de fora deliberadamente — os erros que eles geram SÃO o sinal.
# ---------------------------------------------------------------------------
cat > "$PROBE_DIR/Probe.csproj" <<'CSPROJ'
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
    <ErrorLog>censo.sarif</ErrorLog>
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
  </ItemGroup>
</Project>
CSPROJ

log "Compilando o Backend legado sob net9.0 (espera-se que falhe — esse é o ponto)"
set +e
dotnet build "$PROBE_DIR/Probe.csproj" \
    -p:ChummerBackend="$REPO_ROOT/Chummer/Backend" \
    -p:ChummerSevenZip="$REPO_ROOT/Chummer/7zip" \
    -p:ChummerAnnotations="$REPO_ROOT/Chummer/Properties/Annotations.cs" \
    --nologo -v:n 2>&1 | tee "$WORK_DIR/build.log" > /dev/null
set -e

# O MSBuild emite cada erro duas vezes, uma com prefixo de nó ("  1>arquivo.cs(...)") e
# outra sem ("      arquivo.cs(...)"). Normalizar espaço à esquerda E o prefixo de nó é o
# que faz o sort -u realmente deduplicar; sem isso o total sai exatamente dobrado.
# O caminho do .csproj no fim de cada linha também é removido: é constante e só polui.
sed -E 's/^[[:space:]]+//; s/^[0-9]+>//; s/ \[[^]]*\.csproj\]$//' "$WORK_DIR/build.log" \
    | grep -oE '^[^(]+\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+: .*' \
    | sed "s|$REPO_ROOT/||" | sort -u > "$WORK_DIR/erros.txt" || true

TOTAL=$(wc -l < "$WORK_DIR/erros.txt")

# ---------------------------------------------------------------------------
# Relatório
# ---------------------------------------------------------------------------
{
    echo "# Censo de erros — Backend legado sob net9.0"
    echo
    echo "Gerado por \`scripts/censo-erros.sh\` em $(date +%Y-%m-%d)."
    echo
    echo "**Total de erros distintos: $TOTAL**"
    echo
    echo "## Por código de erro"
    echo
    echo '| Código | Ocorrências | Significado |'
    echo '|---|---|---|'
    grep -oE 'error [A-Z]+[0-9]+' "$WORK_DIR/erros.txt" | sed 's/error //' \
        | sort | uniq -c | sort -rn \
        | while read -r n code; do echo "| \`$code\` | $n | |"; done
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
