#!/usr/bin/env bash
#
# Verifica os arquivos extraídos para Chummer/Controls/, em dois níveis.
#
# Por que existe: o censo compila apenas Backend/ e Chummer.Core/. Os arquivos gerados em
# Controls/ pela extração automática não eram compilados por nada em Linux — só pelo build
# net48 no CI Windows, com minutos de latência.
#
# A primeira versão desta ferramenta checava só SINTAXE, e isso provou ser insuficiente: o
# extrator colocou métodos de CompareTreeNodes dentro de `partial struct ListItem`, gerando
# um arquivo perfeitamente bem formado e semanticamente errado. Só o CI pegou (DEC-029).
#
# Agora compila Backend/ + Chummer.Core/ + Controls/ juntos sob net9.0 e separa:
#
#   ESPERADO   CS0246 / CS0234 / CS1069  — tipos de WinForms e System.Drawing que não
#                                          existem sob net9.0. É o motivo do porte existir.
#   PROBLEMA   todo o resto              — sintaxe quebrada, membro em tipo errado,
#                                          assinatura incompatível. Bugs reais da extração.
#
# Não conseguimos COMPILAR esses arquivos em Linux, mas conseguimos provar que a extração
# não inventou nada além do acoplamento que já sabíamos existir.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK_DIR="${TMPDIR:-/tmp}/chummer-ui"
PROBE="$WORK_DIR/Probe"

export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

mkdir -p "$PROBE"
NOVO="$(cat <<'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>Chummer</RootNamespace>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
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
    <Compile Include="$(R)/Chummer/Backend/**/*.cs" />
    <Compile Include="$(R)/Chummer/7zip/**/*.cs" />
    <Compile Include="$(R)/Chummer/Properties/Annotations.cs" />
    <Compile Include="$(R)/src/Chummer.Core/**/*.cs"
             Exclude="$(R)/src/Chummer.Core/bin/**/*.cs;$(R)/src/Chummer.Core/obj/**/*.cs" />
    <!--
      As três pastas geradas ou tocadas pela extração. Infrastructure e Extensions entram
      porque foram atingidas em cascata pelo bug de DEC-029: chamavam membros que a
      ferramenta havia colocado no tipo errado.
    -->
    <Compile Include="$(R)/Chummer/Controls/Dominio/**/*.cs" />
    <Compile Include="$(R)/Chummer/Controls/Extensions/**/*.cs" />
    <Compile Include="$(R)/Chummer/Controls/Infrastructure/**/*.cs" />
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

cd "$PROBE"
[ "$REST" -eq 1 ] && dotnet restore Probe.csproj -p:R="$REPO_ROOT" --nologo -v:q > /dev/null

set +e
dotnet build Probe.csproj -p:R="$REPO_ROOT" --no-restore --nologo -v:n 2>&1 \
    | tee "$WORK_DIR/build.log" > /dev/null
set -e

TODOS=$(sed -E 's/^[[:space:]]+//; s/^[0-9]+>//; s/ \[[^]]*\.csproj\]$//' "$WORK_DIR/build.log" \
    | grep -oE '^[^(]+\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+: .*' | sort -u || true)

# Classificação por CÓDIGO e por SÍMBOLO.
#
# CS0246 / CS0234 / CS1069 são sempre acoplamento conhecido: tipo não encontrado.
#
# CS0103 é ambíguo e precisa do símbolo para ser decidido. Um enum de WinForms usado como
# valor — `RightToLeft.Inherit` num parâmetro padrão — vira CS0103 e é esperado. Mas
# `MessageBoxButtons` sem o using também vira CS0103 e é bug real. A diferença está no nome.
#
# Esta lista precisa acompanhar TIPOS_UI em scripts/extrair-ui.py.
UI_CONHECIDOS='TreeNode|TreeView|TreeNodeCollection|TreeViewEventArgs|ContextMenuStrip|ToolStrip[A-Za-z]*|Control|Form|IWin32Window|ListViewItem|ListViewGroup|ComboBox|ListBox|ElasticComboBox|ToolTip|RightToLeft|SortOrder|KeyEventArgs|Image|Bitmap|Icon|CursorWait|LoadingBar|ThreadSafeForm|Cursors|Clipboard'

ESPERADO_RE="error (CS0246|CS0234|CS1069):|error CS0103: The name '($UI_CONHECIDOS)'"
PROBLEMAS=$(printf '%s\n' "$TODOS" | grep -vE "$ESPERADO_RE" || true)
N=$(printf '%s' "$PROBLEMAS" | grep -c '' || true)

if [ "$N" -gt 0 ]; then
    printf '\n\033[1;31mFALHA: %s erro(s) que NÃO são acoplamento conhecido\033[0m\n\n' "$N"
    printf '%s\n' "$PROBLEMAS" | sed "s|$REPO_ROOT/||" | head -25
    exit 1
fi

ESPERADOS=$(printf '%s' "$TODOS" | grep -c '' || true)
printf '\n\033[1mOK\033[0m — %s erros, todos de acoplamento conhecido (CS0246/CS0234/CS1069).\n' "$ESPERADOS"
printf 'Nenhum defeito de extração: sintaxe válida e cada membro no tipo certo.\n'
