#!/usr/bin/env bash
#
# Mede o andamento do porte, registra no histórico e DIAGNOSTICA quando ele para.
#
#   ./scripts/progresso.sh            mede, registra e compara com a medição anterior
#   ./scripts/progresso.sh --rapido   só o que é instantâneo (sem compilar)
#   ./scripts/progresso.sh --historico  imprime a série inteira
#
# Por que existe: "o projeto está andando devagar" é sensação. Sensação não tem remédio.
# Este script transforma andamento em série temporal — e quando a agulha não anda, ele diz
# as causas prováveis em ordem de frequência REAL neste projeto, com o remédio de cada uma.
#
# O histórico vive em docs/po/progresso.csv, versionado. Ele precisa sobreviver ao contêiner,
# senão a próxima sessão não tem com o que comparar e o diagnóstico não existe.

set -euo pipefail

RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
CSV="$RAIZ/docs/po/progresso.csv"
MODO="completo"

case "${1:-}" in
    --rapido) MODO="rapido" ;;
    --historico)
        [ -f "$CSV" ] && column -s, -t "$CSV" || echo "sem histórico ainda"
        exit 0 ;;
    -h|--help) sed -n '2,16p' "$0"; exit 0 ;;
esac

export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# ---------------------------------------------------------------------------
# Medição
# ---------------------------------------------------------------------------
DATA=$(date +%Y-%m-%d)
COMMITS=$(git -C "$RAIZ" rev-list --count HEAD 2>/dev/null || echo 0)
BACKEND=$(find "$RAIZ/Chummer/Backend" -name '*.cs' -exec cat {} + 2>/dev/null | wc -l)
CORE=$(find "$RAIZ/src/Chummer.Core" -name '*.cs' -not -path '*/obj/*' -exec cat {} + 2>/dev/null | wc -l)
TSF=$(grep -ro 'ThreadSafeForm' --include=*.cs "$RAIZ/Chummer/Backend" 2>/dev/null | wc -l)
UI=$(find "$RAIZ/src" -path '*Chummer.UI*' -name '*.cs' -not -path '*/obj/*' -exec cat {} + 2>/dev/null | wc -l)
APK=$(find "$RAIZ/src" -name '*.apk' 2>/dev/null | head -1)
APK=$([ -n "$APK" ] && echo sim || echo nao)

if [ "$MODO" = "completo" ]; then
    CENSO=$("$RAIZ/scripts/censo-erros.sh" --rapido --limite 0 2>/dev/null \
            | grep -oE 'Total: [0-9]+' | grep -oE '[0-9]+' || echo -1)
    # A saída tem escape ANSI antes do número; remover antes de casar.
    ANDROID=$("$RAIZ/scripts/verificar-android.sh" 2>/dev/null \
            | sed -E 's/\x1b\[[0-9;]*m//g' | grep -oE '^[0-9]+ erro' \
            | grep -oE '^[0-9]+' || echo -1)
else
    CENSO=-1; ANDROID=-1
fi

# ---------------------------------------------------------------------------
# Histórico
# ---------------------------------------------------------------------------
if [ ! -f "$CSV" ]; then
    mkdir -p "$(dirname "$CSV")"
    echo "data,commits,backend_linhas,core_linhas,censo,android,threadsafeform,ui_linhas,apk" > "$CSV"
fi
ANTERIOR=$(tail -1 "$CSV")
echo "$DATA,$COMMITS,$BACKEND,$CORE,$CENSO,$ANDROID,$TSF,$UI,$APK" >> "$CSV"

delta() { # $1 antes  $2 depois  $3 sentido(-1 menor é melhor, 1 maior é melhor)
    [ "$1" = "-1" ] || [ "$2" = "-1" ] && { printf '     —'; return; }
    local d=$(( $2 - $1 ))
    if [ "$d" -eq 0 ]; then printf '     0'
    elif [ $(( d * $3 )) -gt 0 ]; then printf '\033[32m%+6d\033[0m' "$d"
    else printf '\033[31m%+6d\033[0m' "$d"; fi
}

printf '\n\033[1mAndamento do porte — %s\033[0m\n\n' "$DATA"
if [ -n "$ANTERIOR" ] && [ "${ANTERIOR%%,*}" != "data" ]; then
    IFS=, read -r a_data a_com a_back a_core a_cen a_and a_tsf a_ui a_apk <<< "$ANTERIOR"
    printf '  %-34s %8s  %s\n' "métrica" "agora" "desde $a_data"
    printf '  %-34s %8s  ' "commits"                 "$COMMITS"; delta "$a_com"  "$COMMITS"  1;  echo
    printf '  %-34s %8s  ' "linhas em Backend/ (cai)" "$BACKEND"; delta "$a_back" "$BACKEND" -1; echo
    printf '  %-34s %8s  ' "linhas em Chummer.Core/"  "$CORE";    delta "$a_core" "$CORE"     1;  echo
    printf '  %-34s %8s  ' "erros de declaração"      "$CENSO";   delta "$a_cen"  "$CENSO"   -1; echo
    printf '  %-34s %8s  ' "distância até o Android"  "$ANDROID"; delta "$a_and"  "$ANDROID" -1; echo
    printf '  %-34s %8s  ' "ThreadSafeForm no Backend" "$TSF";    delta "$a_tsf"  "$TSF"     -1; echo
    printf '  %-34s %8s  ' "linhas de UI Avalonia"    "$UI";      delta "$a_ui"   "$UI"       1;  echo
    printf '  %-34s %8s\n' "APK gerado"               "$APK"
    AVANCOU=$(( (a_back - BACKEND) + (CORE - a_core) + (a_tsf - TSF) + (UI - a_ui) ))
    NOVOS_COMMITS=$(( COMMITS - a_com ))
else
    printf '  primeira medição — sem comparação. Rode de novo amanhã.\n'
    AVANCOU=1; NOVOS_COMMITS=1
fi

# ---------------------------------------------------------------------------
# Diagnóstico
#
# A lista de causas está em ordem de frequência REAL neste projeto, não em ordem teórica.
# Cada uma tem um comando que confirma ou descarta, e um remédio.
# ---------------------------------------------------------------------------
if [ "$AVANCOU" -le 0 ] && [ "$NOVOS_COMMITS" -le 0 ]; then
    printf '\n\033[1;31mANDAMENTO ZERO desde a última medição.\033[0m\n'
    cat <<'FIM'

Causas prováveis, em ordem de frequência real neste projeto:

1. TRABALHO PERDIDO COM O CONTÊINER — a mais comum, e a mais cara.
   Um agente terminou, não empurrou, e o contêiner foi reciclado.
   Confirma:  git fetch origin && git branch -r | grep claude/
   Remédio:   integre a branch órfã ANTES de qualquer trabalho novo.
              Refazer o que já estava feito é o desperdício mais caro daqui.

2. CI VERMELHO BLOQUEANDO — nada avança porque a base está quebrada.
   Confirma:  ./scripts/dev.sh check
   Remédio:   conserte antes de tudo. Falha de CI tem prioridade sobre qualquer etapa.

3. FERRAMENTA DE MEDIÇÃO QUEBRADA — já aconteceu CINCO vezes neste projeto.
   O número não anda porque o medidor parou, não porque o trabalho parou.
   Confirma:  injete um defeito conhecido e exija que a ferramenta falhe.
   Remédio:   conserte o medidor primeiro. Medir errado é pior que não medir.

4. BLOQUEADO ESPERANDO O PO — nunca deveria acontecer.
   Confirma:  grep -c 'ABERTA' docs/po/pendencias.md
   Remédio:   o protocolo é registrar a pendência, escolher a resposta mais provável
              como premissa, e SEGUIR. Só premissa 🔴 justifica parar, e mesmo assim
              trabalha-se em outra frente.

5. FRENTE ERRADA — trabalhando no que não está no caminho crítico.
   Confirma:  leia docs/po/estado-atual.md, seção "próximo passo".
   Remédio:   o caminho crítico é: zerar declarações -> medir corpos -> metade de
              seleção -> mover o Backend. Etapa 2.5 roda em paralelo e não compete.

FIM
elif [ "$AVANCOU" -le 0 ]; then
    printf '\n\033[1;33mHouve commits, mas nenhuma métrica andou.\033[0m\n'
    printf 'Normal quando a sessão foi de ferramental, documentação ou medição.\n'
    printf 'Suspeito se acontecer duas medições seguidas — aí o trabalho não está no\n'
    printf 'caminho crítico. Ver docs/po/estado-atual.md.\n'
else
    printf '\n\033[1;32mAndamento confirmado.\033[0m\n'
fi

printf '\nHistórico: %s\n' "${CSV#$RAIZ/}"
