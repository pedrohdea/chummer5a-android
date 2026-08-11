#!/usr/bin/env bash
#
# Verifica a SINTAXE dos arquivos extraídos para Chummer/Controls/.
#
# Por que isto existe: o censo compila apenas Backend/ e Chummer.Core/. Os arquivos movidos
# para Controls/ não são compilados por nada em Linux — só pelo build net48 no CI Windows.
# Sem esta verificação, uma extração malfeita só apareceria minutos depois, no CI.
#
# Como funciona: compila os arquivos sob net9.0, onde os tipos de WinForms não existem, e
# separa os erros em duas famílias:
#
#   CS1xxx  erros de SINTAXE      -> falha; o arquivo gerado está quebrado
#   demais  erros SEMÂNTICOS      -> esperados; são os tipos de WinForms ausentes
#
# Ou seja: não conseguimos compilar esses arquivos aqui, mas conseguimos provar que estão
# sintaticamente bem formados, que é o que a extração automática pode quebrar.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK_DIR="${TMPDIR:-/tmp}/chummer-sintaxe"
PROBE="$WORK_DIR/Probe"

export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

mkdir -p "$PROBE"
cat > "$PROBE/Probe.csproj" <<'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(Alvos)/**/*.cs" />
  </ItemGroup>
</Project>
CSPROJ

cd "$PROBE"
set +e
dotnet build Probe.csproj -p:Alvos="$REPO_ROOT/Chummer/Controls" --nologo -v:n 2>&1 \
    | tee "$WORK_DIR/build.log" > /dev/null
set -e

# Lista explícita de códigos de SINTAXE, e não uma faixa numérica.
#
# A faixa CS1xxx não serve: CS1069 ("tipo encaminhado para outro assembly") cai nela e é
# semântico — é exatamente o erro esperado para System.Drawing sob net9.0. Usar a faixa
# fazia o verificador acusar falha em arquivos perfeitamente bem formados.
CODIGOS_SINTAXE='CS1001|CS1002|CS1003|CS1004|CS1010|CS1012|CS1013|CS1014|CS1022|CS1023|CS1026|CS1027|CS1028|CS1031|CS1033|CS1035|CS1037|CS1041|CS1043|CS1055|CS1056|CS1063|CS1513|CS1514|CS1518|CS1519|CS1520|CS1525|CS1526|CS1527|CS1528|CS1529|CS1547|CS1553|CS1597|CS1611|CS1733|CS8124|CS8803'

SINTAXE=$(sed -E 's/^[[:space:]]+//; s/^[0-9]+>//' "$WORK_DIR/build.log" \
    | grep -oE "^[^(]+\([0-9]+,[0-9]+\): error ($CODIGOS_SINTAXE): .*" | sort -u || true)

if [ -n "$SINTAXE" ]; then
    N=$(printf '%s\n' "$SINTAXE" | grep -c '')
    printf '\n\033[1;31mFALHA: %s erro(s) de sintaxe nos arquivos de Controls/\033[0m\n\n' "$N"
    printf '%s\n' "$SINTAXE" | sed "s|$REPO_ROOT/||" | head -20
    exit 1
fi

printf '\n\033[1mSintaxe OK\033[0m — nenhum erro CS1xxx em Chummer/Controls/.\n'
printf 'Erros semânticos (tipos de WinForms ausentes sob net9.0) são esperados e ignorados.\n'
