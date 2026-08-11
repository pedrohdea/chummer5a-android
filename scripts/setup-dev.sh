#!/usr/bin/env bash
#
# Prepara o ambiente de desenvolvimento do porte para Android.
#
# O container de desenvolvimento é efêmero: nada sobrevive entre sessões. Este script
# é a fonte da verdade da toolchain — se algo precisa estar instalado para o build
# funcionar, precisa estar aqui, e não em um comando avulso digitado uma vez.
#
# Uso:
#   ./scripts/setup-dev.sh            # SDK .NET (suficiente para Chummer.Core e testes)
#   ./scripts/setup-dev.sh --android  # + workload Android (necessário para gerar APK)
#
# Idempotente: rodar de novo não quebra nada.

set -euo pipefail

DOTNET_CHANNEL="9.0"
DOTNET_ROOT="${DOTNET_ROOT:-/usr/share/dotnet}"
INSTALL_ANDROID=0

for arg in "$@"; do
    case "$arg" in
        --android) INSTALL_ANDROID=1 ;;
        -h|--help) sed -n '2,15p' "$0"; exit 0 ;;
        *) echo "Argumento desconhecido: $arg" >&2; exit 2 ;;
    esac
done

log() { printf '\n\033[1m==> %s\033[0m\n' "$*"; }

# ---------------------------------------------------------------------------
# SDK .NET
# ---------------------------------------------------------------------------
if [ -x "$DOTNET_ROOT/dotnet" ]; then
    log "SDK .NET já presente em $DOTNET_ROOT"
else
    log "Instalando SDK .NET $DOTNET_CHANNEL em $DOTNET_ROOT"
    tmp_script="$(mktemp)"
    # -L é obrigatório: dot.net responde com redirecionamento.
    curl -sSL --max-time 120 https://dot.net/v1/dotnet-install.sh -o "$tmp_script"
    chmod +x "$tmp_script"
    "$tmp_script" --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_ROOT" --no-path
    rm -f "$tmp_script"
fi

# Deixa o dotnet no PATH de qualquer shell, sem depender de perfil.
if [ ! -e /usr/local/bin/dotnet ]; then
    ln -sf "$DOTNET_ROOT/dotnet" /usr/local/bin/dotnet
fi

export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

log "SDKs instalados"
dotnet --list-sdks

# ---------------------------------------------------------------------------
# Workload Android (opcional — só é necessário para produzir o APK)
# ---------------------------------------------------------------------------
if [ "$INSTALL_ANDROID" -eq 1 ]; then
    log "Instalando workload Android"
    dotnet workload install android --skip-sign-check
    log "Workloads instalados"
    dotnet workload list
else
    echo
    echo "Workload Android não instalado (use --android quando for gerar APK)."
fi

# ---------------------------------------------------------------------------
# Verificação
# ---------------------------------------------------------------------------
log "Restaurando e compilando Chummer.Port.sln"
cd "$(dirname "$0")/.."
dotnet build Chummer.Port.sln --nologo

log "Ambiente pronto"
