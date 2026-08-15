#!/usr/bin/env bash
#
# Instala o APK num emulador JÁ EM EXECUÇÃO, lança o app e exige que ele fique de pé.
#
#   ./scripts/verificar-apk.sh <caminho-do-apk> [png-de-saida]
#
# Responde a UMA pergunta, que nenhuma outra ferramenta do projeto responde: o app ABRE?
# `dev.sh check` prova que compila; `dev.sh tela` prova que a UI desenha no desktop. Nenhum
# dos dois toca no runtime do Android — empacotamento, ABI, carregamento das .so nativas,
# ciclo de vida da Activity. É onde um app que compila perfeitamente morre na inicialização.
#
# NÃO roda no contêiner de desenvolvimento: não há /dev/kvm aqui e o emulador por software
# não aguenta o `adb install`. Roda no CI, onde o runner Linux do GitHub tem KVM. Ver DEC-052.
#
# O critério é deliberadamente severo — três provas independentes, porque cada uma sozinha
# mente:
#   1. o processo existe DEPOIS da espera  (sozinho: um app que abre e trava passa)
#   2. logcat não tem FATAL EXCEPTION      (sozinho: crash silencioso passa)
#   3. a activity está em foco             (sozinho: não distingue app de tela preta)

set -euo pipefail

APK="${1:?uso: verificar-apk.sh <caminho-do-apk> [png-de-saida]}"
PNG="${2:-}"
PACOTE="com.chummer5a.android"
ACTIVITY="$PACOTE/com.chummer5a.android.MainActivity"
# O app tem de continuar vivo depois disto. Um crash de inicialização do Mono acontece
# tipicamente no primeiro segundo; 10 s dá margem confortável num emulador sem aceleração.
ESPERA=10

[ -f "$APK" ] || { echo "APK não encontrado: $APK" >&2; exit 1; }

echo "==> Aparelho"
adb wait-for-device
adb shell 'while [ "$(getprop sys.boot_completed)" != "1" ]; do sleep 1; done'
adb shell getprop ro.build.version.release
adb shell getprop ro.product.cpu.abi

echo "==> Instalando $(basename "$APK") ($(stat -c%s "$APK") bytes)"
adb uninstall "$PACOTE" > /dev/null 2>&1 || true
adb install -r "$APK"

# Limpar ANTES de lançar: senão o logcat traz lixo de execuções anteriores e a busca por
# FATAL EXCEPTION acusa um crash que não é deste lançamento.
adb logcat -c || true

echo "==> Lançando $ACTIVITY"
adb shell am start -W -n "$ACTIVITY"

echo "==> Esperando ${ESPERA}s"
sleep "$ESPERA"

FALHOU=0

# 1. O processo continua vivo.
PID="$(adb shell pidof "$PACOTE" | tr -d '\r' || true)"
if [ -n "$PID" ]; then
    echo "OK   processo vivo (pid $PID)"
else
    echo "FALHA  o processo morreu — o app não continuou de pé" >&2
    FALHOU=1
fi

# 2. Nenhum crash no log — Java OU NATIVO.
#
# Procurar só por FATAL EXCEPTION tem ponto cego, e ele nos mordeu na primeira execução de
# verdade: o app morreu em 3 s SEM exceção Java nenhuma, porque um SIGSEGV no runtime Mono ou
# no Skia não passa pelo AndroidRuntime. Silêncio no filtro estreito parecia "sem crash".
LOG="$(adb logcat -d 2>/dev/null || true)"
CRASH="$(adb logcat -b crash -d 2>/dev/null || true)"
PADRAO='FATAL EXCEPTION|AndroidRuntime: .*Exception|Fatal signal|signal [0-9]+ \(SIG|F DEBUG|tombstone|SIGSEGV|SIGABRT|Process .* has died|Force finishing activity'
if printf '%s\n%s' "$LOG" "$CRASH" | grep -qE "$PADRAO"; then
    echo "FALHA  crash no logcat:" >&2
    printf '%s\n%s' "$LOG" "$CRASH" | grep -E -A 25 "$PADRAO" | head -60 >&2
    FALHOU=1
else
    echo "OK   sem crash (Java ou nativo) no logcat"
fi

# 3. A activity está em foco. É o que separa "o processo existe" de "há app na tela".
if adb shell dumpsys activity activities 2>/dev/null | grep -q "$PACOTE"; then
    echo "OK   activity presente na pilha"
else
    echo "FALHA  a activity não está na pilha de atividades" >&2
    FALHOU=1
fi

# A captura sai SEMPRE, inclusive na falha: uma tela branca ou preta é o diagnóstico, e é
# precisamente o sintoma que a armadilha do TopLevel do Avalonia produz.
if [ -n "$PNG" ]; then
    mkdir -p "$(dirname "$PNG")"
    adb exec-out screencap -p > "$PNG" 2>/dev/null || true
    if [ -s "$PNG" ]; then
        echo "captura: $PNG ($(stat -c%s "$PNG") bytes)"
    else
        echo "captura falhou" >&2
    fi
fi

# O que o app registrou por conta própria durante a inicialização.
echo
echo "--- linhas do app ---"
printf '%s' "$LOG" | grep -iE 'chummer|mono|avalonia|dotnet|skia' | tail -30 || true

if [ "$FALHOU" -ne 0 ]; then
    # Numa falha, o log FILTRADO é justamente o que não basta — se bastasse, a falha já teria
    # sido diagnosticada acima. Despeja o bruto: um crash nativo aparece aqui e em nenhum
    # filtro que eu soubesse escrever de antemão.
    echo
    echo "--- logcat bruto, últimas 200 linhas ---" >&2
    printf '%s' "$LOG" | tail -200 >&2
    echo
    echo "--- buffer de crash ---" >&2
    printf '%s' "$CRASH" | tail -60 >&2
    echo
    echo "--- o sistema matou o processo? ---" >&2
    adb shell dumpsys activity exit-info "$PACOTE" 2>/dev/null | head -40 >&2 || true

    echo
    echo "O APK NÃO passou." >&2
    exit 1
fi

echo
echo "O APK instala, abre e continua de pé."
