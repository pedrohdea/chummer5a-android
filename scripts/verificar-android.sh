#!/usr/bin/env bash
#
# Compila o DOMÍNIO com os stubs de diálogo, no alvo do Android. Mede quanto falta para o
# Chummer.Core existir num APK.
#
# É o spike de DEC-037: em vez de extrair todo o acoplamento de UI antes de compilar para
# Android (semanas), fornecer stubs que compilam e lançam se chamados (horas), e descobrir
# se carregar um .chum5 sequer toca um diálogo.
#
# Diferença para as outras três ferramentas:
#   censo-erros.sh      domínio SEM stubs, mede o acoplamento a remover de verdade
#   verificar-ui.sh     valida o que a extração produziu
#   verificar-legado.sh prova que o app WinForms continua compilando
#   ESTE                mede a distância até o domínio rodar no Android
#
# RESSALVA (DEC-032): enquanto houver erro de declaração, corpos de método não são
# vinculados. O número aqui é piso, não teto — só depois de zerar é que o resto aparece.

set -euo pipefail
RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
DIR="${TMPDIR:-/tmp}/chummer-android"; PROBE="$DIR/Probe"
export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
mkdir -p "$PROBE"

python3 "$RAIZ/scripts/gerar-stubs-dialogos.py" > /dev/null

NOVO="$(cat "$RAIZ/scripts/probe/android.csproj.in")"
if [ ! -f "$PROBE/Probe.csproj" ] || [ "$NOVO" != "$(cat "$PROBE/Probe.csproj")" ]; then
    printf '%s\n' "$NOVO" > "$PROBE/Probe.csproj"; REST=1
else REST=0; fi

cd "$PROBE"
[ "$REST" -eq 1 ] && dotnet restore Probe.csproj -p:R="$RAIZ" --nologo -v:q > /dev/null
set +e
dotnet build Probe.csproj -p:R="$RAIZ" --no-restore --nologo -v:n 2>&1 | tee "$DIR/build.log" > /dev/null
set -e

ERROS=$(sed -E 's/^[[:space:]]+//; s/^[0-9]+>//; s/ \[[^]]*\.csproj\]$//' "$DIR/build.log" \
    | grep -oE '^[^(]+\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+: .*' | sed "s|$RAIZ/||" | sort -u || true)
N=$(printf '%s' "$ERROS" | grep -c '' || true)

if [ -z "$ERROS" ] && ! grep -q "Build succeeded" "$DIR/build.log"; then
    echo "ERRO: build não começou." >&2; tail -20 "$DIR/build.log" >&2; exit 1
fi

printf '\n\033[1m%s erro(s) entre o domínio e o Android\033[0m\n\n' "$N"
printf '%s\n' "$ERROS" | sed 's/ (are you missing.*//' | head -25
[ "$N" -eq 0 ] && printf '\n\033[1mO domínio compila no alvo do Android.\033[0m\n'
printf '\nRessalva: só a fase de declaração é vinculada enquanto houver erro. Ver DEC-032.\n'
exit 0
