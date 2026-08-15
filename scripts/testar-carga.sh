#!/usr/bin/env bash
#
# Compila e RODA a sondagem de carga: abre .chum5 de Chummer.Tests/TestFiles/ com o domínio
# real e diz quantos carregam. É o spike de DEC-037 levado até a execução.
#
#   ./scripts/testar-carga.sh            # compila e roda sobre os menores primeiro
#   ./scripts/testar-carga.sh --erros    # só compila, e lista os erros agrupados
#   ./scripts/testar-carga.sh Skink      # roda só os arquivos cujo nome casa
#
# Ver docs/po/decisoes.md, DEC-037.

set -euo pipefail
RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
DIR="${TMPDIR:-/tmp}/chummer-carga"; PROBE="$DIR/Probe"
export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
mkdir -p "$PROBE"

python3 "$RAIZ/scripts/gerar-stubs-dialogos.py" > /dev/null

NOVO="$(cat "$RAIZ/scripts/probe/carregar.csproj.in")"
if [ ! -f "$PROBE/Probe.csproj" ] || [ "$NOVO" != "$(cat "$PROBE/Probe.csproj")" ]; then
    printf '%s\n' "$NOVO" > "$PROBE/Probe.csproj"; REST=1
else REST=0; fi

cd "$PROBE"
[ "$REST" -eq 1 ] && dotnet restore Probe.csproj -p:R="$RAIZ" --nologo -v:q > /dev/null
set +e
dotnet build Probe.csproj -p:R="$RAIZ" --no-restore --nologo -v:n -c Release 2>&1 \
    | tee "$DIR/build.log" > /dev/null
set -e

ERROS=$(sed -E 's/^[[:space:]]+//; s/^[0-9]+>//; s/ \[[^]]*\.csproj\]$//' "$DIR/build.log" \
    | grep -oE '^[^(]+\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+: .*' | sed "s|$RAIZ/||" | sort -u || true)
N=$(printf '%s' "$ERROS" | grep -c '' || true)
[ -z "$ERROS" ] && N=0

if [ "$N" -gt 0 ] || [ "${1:-}" = "--erros" ]; then
    printf '\n\033[1m%s erro(s) de compilação\033[0m\n\n' "$N"
    printf '%s\n' "$ERROS" | sed 's/ (are you missing.*//' | head -40
    [ "$N" -gt 0 ] && exit 1
fi

exec dotnet "$PROBE/bin/Release/net9.0/CarregarChum5.dll" "$RAIZ" "$@"
