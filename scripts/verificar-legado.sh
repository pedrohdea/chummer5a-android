#!/usr/bin/env bash
#
# Compila TODO o aplicativo legado em Linux, sob net9.0-windows, com WinForms de verdade.
#
# POR QUE ESTE SCRIPT EXISTE
#
# Até aqui, nada em Linux conseguia enxergar erro dentro de corpo de método. O censo compila
# sob net9.0 sem WinForms, onde sempre há erro de declaração — e o Roslyn não vincula corpos
# quando a fase de declaração falhou (DEC-032). O resultado prático: quatro rodadas de CI
# vermelhas por erros que nenhuma ferramenta local podia ver, com cinco minutos de latência
# cada uma, todas em código que eu mesmo havia acabado de reescrever.
#
# A descoberta que destrava isto: `EnableWindowsTargeting=true` faz o SDK restaurar os
# assemblies de referência do Windows Desktop em Linux. `UseWindowsForms` passa a funcionar,
# `System.Windows.Forms` resolve, os erros de declaração vão a zero — e aí, finalmente, o
# compilador vincula os corpos e mostra o que o CI Windows mostraria.
#
# O QUE ELE É E O QUE NÃO É
#
# É um espelho fiel o suficiente para pegar as classes de erro que já custaram caro:
# incompatibilidade de tipo (DialogResult contra PromptResult), argumento nomeado que não
# existe, membro emitido no tipo errado, membro que sumiu numa extração.
#
# NÃO é o build net48. O alvo é net9.0-windows, e algumas APIs diferem — `ContextMenu`, por
# exemplo, foi removida no .NET Core e não existe aqui, embora exista no net48. Essas
# diferenças estão em DIFERENCAS_CONHECIDAS abaixo, uma a uma, com o motivo. O CI Windows
# continua sendo a única prova de que o net48 compila; este script tira dele o papel de
# primeira linha de defesa.
#
# Uso:
#   ./scripts/verificar-legado.sh            compila e classifica
#   ./scripts/verificar-legado.sh --erros    imprime todos os erros, sem classificar
#   ./scripts/verificar-legado.sh --limpar   descarta a sondagem e recomeça

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK_DIR="${TMPDIR:-/tmp}/chummer-legado"
PROBE="$WORK_DIR/Probe"
MODO="classificar"

while [ $# -gt 0 ]; do
    case "$1" in
        --erros) MODO="erros"; shift ;;
        --limpar) rm -rf "$PROBE"; shift ;;
        -h|--help) sed -n '2,32p' "$0"; exit 0 ;;
        *) echo "opção desconhecida: $1" >&2; exit 2 ;;
    esac
done

export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

mkdir -p "$PROBE"

# Os arquivos que o Chummer.csproj remove da compilação são código morto — referenciam tipos
# que não existem mais em lugar nenhum do repositório. A lista é lida do próprio csproj em
# vez de copiada, para não sair de sincronia quando o legado mudar.
EXCLUIDOS=$(grep -oE '<Compile Remove="[^"]+"' "$REPO_ROOT/Chummer/Chummer.csproj" \
    | sed 's/<Compile Remove="//; s/"$//' | tr '\\' '/' \
    | sed "s|^|\$(R)/Chummer/|" | paste -sd';' -)

NOVO="$(cat <<CSPROJ
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <!-- LiveCharts.WinForms.CartesianChart deriva de ElementHost, que vem de
         WindowsFormsIntegration — a ponte WinForms/WPF. Sem UseWPF o tipo base não resolve
         e ExpenseChart inteiro vira erro de declaração. -->
    <UseWPF>true</UseWPF>
    <!-- É esta propriedade que faz o pacote de referência do Windows Desktop ser
         restaurado em Linux. Sem ela o alvo -windows nem resolve. -->
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <RootNamespace>Chummer</RootNamespace>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <!-- WFO1000 é um ANALISADOR do WinForms do .NET moderno, inexistente no net48, e sua
         severidade padrão é erro. Suprimir analisador é legítimo aqui e suprimir erro de
         declaração não seria: analisador não interfere na vinculação de corpos de método,
         erro de declaração cega o compilador inteiro (DEC-032). -->
    <NoWarn>\$(NoWarn);CS1591;WFO1000</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Ben.Demystifier" Version="0.4.1" />
    <PackageReference Include="HtmlRenderer.Core" Version="1.5.2" />
    <PackageReference Include="HtmlRenderer.WinForms" Version="1.5.2" />
    <PackageReference Include="itext" Version="9.7.0" />
    <PackageReference Include="LiveCharts.WinForms" Version="0.9.7.1" />
    <PackageReference Include="Microsoft.ApplicationInsights.NLogTarget" Version="2.23.0" />
    <PackageReference Include="Microsoft.ApplicationInsights.PerfCounterCollector" Version="2.23.0" />
    <PackageReference Include="Microsoft.Extensions.ObjectPool" Version="10.0.9" />
    <PackageReference Include="Microsoft.IO.RecyclableMemoryStream" Version="3.0.1" />
    <PackageReference Include="Microsoft.VisualStudio.Threading" Version="18.7.23" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageReference Include="NLog" Version="6.1.4" />
    <PackageReference Include="RtfPipe" Version="2.0.7677.4303" />
    <PackageReference Include="System.ComponentModel.Composition" Version="10.0.9" />
    <PackageReference Include="System.Drawing.Common" Version="9.0.0" />
    <PackageReference Include="XoshiroPRNG.Net" Version="1.6.0" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="\$(R)/Chummer/**/*.cs" Exclude="$EXCLUIDOS" />
    <Compile Include="\$(R)/src/Chummer.Core/**/*.cs"
             Exclude="\$(R)/src/Chummer.Core/bin/**/*.cs;\$(R)/src/Chummer.Core/obj/**/*.cs" />
    <!-- Tipos que o net48 tem e o net9 não. Precisam ser DECLARADOS, não filtrados da
         saída: erro de declaração impede o Roslyn de vincular corpos de método. -->
    <Compile Include="\$(R)/scripts/probe/WinFormsRemovedTypes.cs" />
  </ItemGroup>
</Project>
CSPROJ
)"
if [ ! -f "$PROBE/Probe.csproj" ] || [ "$NOVO" != "$(cat "$PROBE/Probe.csproj")" ]; then
    printf '%s\n' "$NOVO" > "$PROBE/Probe.csproj"
    REST=1
else
    REST=0
fi

# Roda de fora da árvore do repositório: o dotnet resolve o global.json pelo diretório de
# trabalho, e o da raiz fixa o SDK 8 do build legado (DEC-012).
cd "$PROBE"
[ "$REST" -eq 1 ] && dotnet restore Probe.csproj -p:R="$REPO_ROOT" --nologo -v:q > /dev/null

set +e
dotnet build Probe.csproj -p:R="$REPO_ROOT" --no-restore --nologo -v:n 2>&1 \
    | tee "$WORK_DIR/build.log" > /dev/null
set -e

ERROS=$(sed -E 's/^[[:space:]]+//; s/^[0-9]+>//; s/ \[[^]]*\.csproj\]$//' "$WORK_DIR/build.log" \
    | grep -oE '^[^(]+\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+: .*' \
    | sed "s|$REPO_ROOT/||" | sort -u || true)

if [ "$MODO" = "erros" ]; then
    printf '%s\n' "$ERROS"
    exit 0
fi

# Falso zero: se o build nem começou, "0 erros" é indistinguível de "tudo compila".
if [ -z "$ERROS" ] && ! grep -q "Build succeeded" "$WORK_DIR/build.log"; then
    echo "ERRO: nenhum erro encontrado, mas o build também não teve sucesso." >&2
    tail -20 "$WORK_DIR/build.log" >&2
    exit 1
fi

# Não há lista de exceções, e isso é deliberado.
#
# A primeira versão ignorava o CS0246 de ContextMenu como "diferença conhecida". Mas esse é
# um erro de DECLARAÇÃO, e enquanto existir um deles o Roslyn não vincula corpo de método
# nenhum (DEC-032) — o script anunciava "corpos vinculados" sem analisar corpo algum.
# Diferenças entre net48 e net9 são resolvidas declarando o tipo em
# scripts/probe/WinFormsRemovedTypes.cs, para que os erros de declaração cheguem de fato a
# zero. Filtrar a saída esconderia o sintoma e manteria a cegueira.
# DIFERENÇAS net48 QUE SÓ PODEM SER FILTRADAS AQUI: as de CORPO DE MÉTODO.
#
# A distinção é a razão de ser deste script. Um erro de DECLARAÇÃO impede o Roslyn de
# vincular corpos (DEC-032), então filtrá-lo da saída manteria o script cego enquanto ele
# anuncia sucesso — foi exatamente o que aconteceu com ContextMenu. Já um erro de corpo não
# afeta a análise de nada, e filtrá-lo custa apenas aquele ponto.
#
#   CS1929 GetAccessControl — no net48 é método de Directory; no .NET moderno virou extensão
#                             sobre DirectoryInfo, em System.IO.FileSystem.AccessControl.
#                             Ocorre no corpo de Utils.CreateDirectoryWithPermissions.
DIFERENCAS_DE_CORPO="error CS1929: 'Directory' does not contain a definition for 'GetAccessControl'"

REAIS=$(printf '%s\n' "$ERROS" | grep -vE "$DIFERENCAS_DE_CORPO" || true)
N=$(printf '%s' "$REAIS" | grep -c '' || true)

if [ "$N" -gt 0 ]; then
    printf '\n\033[1;31mFALHA: %s erro(s) no build legado\033[0m\n\n' "$N"
    printf '%s\n' "$REAIS" | head -40
    exit 1
fi

printf '\n\033[1mOK\033[0m — o aplicativo legado inteiro compila sob net9.0-windows.\n'
printf 'Corpos de método vinculados: erros de tipo, de argumento nomeado e de membro em\n'
printf 'tipo errado apareceriam aqui — e nenhum erro de declaração restou para cegá-los.\n'
