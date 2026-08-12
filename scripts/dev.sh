#!/usr/bin/env bash
#
# Ponto de entrada único do desenvolvimento. Tudo se faz por aqui.
#
#   ./scripts/dev.sh check      ANTES DE TODO COMMIT — roda o que precisa passar
#   ./scripts/dev.sh setup      instala o SDK .NET e o que mais for preciso
#   ./scripts/dev.sh build      compila o porte (src/)
#   ./scripts/dev.sh legado     compila o app legado INTEIRO em Linux (~30 s)
#   ./scripts/dev.sh ui         valida o código extraído para Controls/
#   ./scripts/dev.sh censo      mede o acoplamento restante (relatório)
#   ./scripts/dev.sh erros      os primeiros erros do censo, ~1 s (laço interno)
#   ./scripts/dev.sh erros Gear    idem, filtrado por arquivo
#   ./scripts/dev.sh apk        gera o APK (exige Android SDK; ver docs/DESENVOLVIMENTO.md)
#   ./scripts/dev.sh status     onde o porte está, em números
#
# Por que existe: as ferramentas do projeto medem coisas diferentes e é fácil rodar a
# errada. `check` é a resposta para "o que eu preciso rodar antes de commitar" — e ela
# inclui `legado`, que é a única que enxerga erro dentro de corpo de método (DEC-032).

set -euo pipefail

RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="${DOTNET_ROOT:-/usr/share/dotnet}:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

titulo() { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
erro()   { printf '\n\033[1;31m%s\033[0m\n' "$*" >&2; }

cmd="${1:-check}"; shift || true

case "$cmd" in

  setup)
    exec "$RAIZ/scripts/setup-dev.sh" "$@"
    ;;

  build)
    titulo "Compilando o porte"
    # De dentro de src/: o dotnet resolve o global.json pelo diretório de trabalho, e o da
    # raiz fixa o SDK 8 do build legado (DEC-012). Compilar da raiz quebra com MSB3823.
    cd "$RAIZ/src" && dotnet build Chummer.Port.sln --nologo "$@"
    ;;

  legado)
    exec "$RAIZ/scripts/verificar-legado.sh" "$@"
    ;;

  ui)
    exec "$RAIZ/scripts/verificar-ui.sh" "$@"
    ;;

  censo)
    exec "$RAIZ/scripts/censo-erros.sh" "$@"
    ;;

  erros)
    exec "$RAIZ/scripts/censo-erros.sh" --rapido "$@"
    ;;

  check)
    # A ordem é do mais barato para o mais caro, para falhar cedo.
    titulo "1/3 · auditoria estrutural das parciais extraídas"
    python3 "$RAIZ/scripts/probe/auditar-parciais.py" "$RAIZ"

    titulo "2/3 · código extraído para Controls/"
    "$RAIZ/scripts/verificar-ui.sh"

    titulo "3/3 · build legado completo (o único que vê corpo de método)"
    "$RAIZ/scripts/verificar-legado.sh"

    printf '\n\033[1mTudo passou.\033[0m Pode commitar.\n'
    ;;

  apk)
    # O setup instala em ~/android-sdk; adota automaticamente se estiver lá.
    if [ -z "${ANDROID_HOME:-}" ] && [ -d "$HOME/android-sdk/platforms" ]; then
        export ANDROID_HOME="$HOME/android-sdk" ANDROID_SDK_ROOT="$HOME/android-sdk"
    fi
    if [ -z "${ANDROID_HOME:-}${ANDROID_SDK_ROOT:-}" ]; then
      erro "Android SDK não encontrado (ANDROID_HOME/ANDROID_SDK_ROOT vazios)."
      cat >&2 <<'FIM'

O workload .NET de Android instala normalmente aqui:

    dotnet workload install android

Mas ele traz só o compilador e os runtimes. Empacotar um APK exige também o SDK do
Google (platform + build-tools), que este contêiner não tem e não conseguiu baixar.

Rode:  ./scripts/setup-dev.sh --android

Ele baixa o SDK do Google descobrindo a URL no índice do repositório. Medido: o APK
sai deste contêiner em ~50 s, sem CI.

Ver docs/DESENVOLVIMENTO.md, seção "APK".
FIM
      exit 1
    fi
    titulo "Gerando APK"
    cd "$RAIZ/src" && dotnet publish Chummer.Android/Chummer.Android.csproj \
        -c Release -f net9.0-android --nologo "$@"
    ;;

  status)
    titulo "Onde o porte está"
    printf '%-46s %s\n' "erros de DECLARAÇÃO restantes (censo)" \
      "$("$RAIZ/scripts/censo-erros.sh" --rapido --limite 0 2>/dev/null | grep -oE 'Total: [0-9]+' | grep -oE '[0-9]+' || echo '?')"
    printf '%-46s %s\n' "ThreadSafeForm no Backend (corpo, não medido pelo censo)" \
      "$(grep -ro 'ThreadSafeForm' --include=*.cs "$RAIZ/Chummer/Backend" | wc -l)"
    printf '%-46s %s\n' "arquivos Backend/ com using WinForms" \
      "$(grep -rl 'using System.Windows.Forms' --include=*.cs "$RAIZ/Chummer/Backend" | wc -l)"
    printf '%-46s %s\n' "linhas ainda em Chummer/Backend/" \
      "$(find "$RAIZ/Chummer/Backend" -name '*.cs' -exec cat {} + | wc -l)"
    printf '%-46s %s\n' "linhas já em src/Chummer.Core/" \
      "$(find "$RAIZ/src/Chummer.Core" -name '*.cs' -not -path '*/obj/*' -exec cat {} + 2>/dev/null | wc -l)"
    ;;

  -h|--help|help)
    sed -n '2,20p' "$0"
    ;;

  *)
    erro "subcomando desconhecido: $cmd"
    sed -n '2,20p' "$0" >&2
    exit 2
    ;;
esac
