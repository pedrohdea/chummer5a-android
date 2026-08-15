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

# TUDO daqui para baixo roda DE DENTRO de src/, e isso é essencial: o dotnet resolve o
# global.json a partir do diretório de trabalho, não do caminho do projeto. src/global.json
# exige o SDK 9; o global.json da raiz exige o SDK 8 do build legado. Rodar `dotnet workload`
# da raiz seleciona o SDK errado e o comando morre com "A compatible .NET SDK was not found"
# — que foi exatamente o que aconteceu, silenciosamente, até 15/08. Ver DEC-012.
cd "$(dirname "$0")/../src"

# ---------------------------------------------------------------------------
# Workload Android (opcional — só é necessário para produzir o APK)
# ---------------------------------------------------------------------------
if [ "$INSTALL_ANDROID" -eq 1 ]; then
    log "Instalando workload Android"
    dotnet workload install android --skip-sign-check

    # Exigir a evidência em vez de confiar no código de saída: `dotnet workload` já devolveu
    # 0 depois de falhar em selecionar o SDK. Sem esta guarda o setup "passa" e o defeito só
    # reaparece lá na frente, como XA5300 na hora de empacotar.
    if ! dotnet workload list | grep -qi '^android'; then
        echo "Workload android NÃO ficou instalado — abortando." >&2
        dotnet workload list >&2
        exit 1
    fi

    # ------------------------------------------------------------------
    # SDK do Google. O workload acima traz só compilador e runtimes; sem o SDK
    # o build para com XA5300 e nenhum APK sai.
    #
    # A URL é DESCOBERTA no índice do repositório, nunca chutada: o nome do
    # arquivo é `commandlinetools-linux-<build>_latest.zip` — sem hífen entre
    # "commandline" e "tools". Chutando com hífen dá 404 em todas as versões, e
    # foi o que me fez concluir, errado, que o SDK não entrava neste contêiner.
    # ------------------------------------------------------------------
    SDK="${ANDROID_HOME:-$HOME/android-sdk}"
    if [ ! -d "$SDK/platforms" ]; then
        echo "Instalando o Android SDK em $SDK ..."
        TMP=$(mktemp -d)
        curl -sSL -o "$TMP/repo.xml" https://dl.google.com/android/repository/repository2-3.xml
        PKG=$(grep -oE 'commandlinetools-linux-[0-9]+_latest\.zip' "$TMP/repo.xml" \
              | sort -t- -k3 -n | tail -1)
        [ -n "$PKG" ] || { echo "não achei o commandlinetools no índice" >&2; exit 1; }
        curl -sSL -o "$TMP/cmdline.zip" "https://dl.google.com/android/repository/$PKG"
        mkdir -p "$SDK/cmdline-tools"
        unzip -q "$TMP/cmdline.zip" -d "$TMP/x"
        rm -rf "$SDK/cmdline-tools/latest"
        mv "$TMP/x/cmdline-tools" "$SDK/cmdline-tools/latest"
        rm -rf "$TMP"
        export ANDROID_HOME="$SDK" ANDROID_SDK_ROOT="$SDK"
        export PATH="$SDK/cmdline-tools/latest/bin:$PATH"
        yes 2>/dev/null | sdkmanager --licenses > /dev/null 2>&1 || true
        sdkmanager --install "platform-tools" "platforms;android-35" "build-tools;35.0.0" \
            > /dev/null 2>&1
    fi
    echo "Android SDK: $SDK"
    echo "Exporte antes de compilar:  export ANDROID_HOME=$SDK ANDROID_SDK_ROOT=$SDK"
    log "Workloads instalados"
    dotnet workload list
else
    echo
    echo "Workload Android não instalado (use --android quando for gerar APK)."
fi

# ---------------------------------------------------------------------------
# Verificação
# ---------------------------------------------------------------------------
log "Restaurando e compilando src/Chummer.Port.sln"
dotnet build Chummer.Port.sln --nologo

log "Ambiente pronto"
