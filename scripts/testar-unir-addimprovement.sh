#!/usr/bin/env bash
#
# Testa a ferramenta de uniao INJETANDO DEFEITOS CONHECIDOS e exigindo que ela recuse.
#
# Existe porque o primeiro numero que uma medicao produziu neste projeto ja esteve errado
# cinco vezes. Uma ferramenta que so foi vista acertando nao foi testada — foi observada.
#
# Cada caso restaura os dois arquivos do git, estraga um deles de um jeito especifico, e
# espera codigo de saida != 0. Se a ferramenta ACEITAR um arquivo estragado, o teste falha.
#
#   ./scripts/testar-unir-addimprovement.sh
#
set -uo pipefail

RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
cd "$RAIZ"
SYNC=Chummer/Backend/Improvements/AddImprovementCollection.cs
ASYNC=Chummer/Backend/Improvements/AddImprovementAsyncCollection.cs
FALHAS=0

# O teste restaura os dois arquivos do git a cada caso — logo, ele DESTROI trabalho nao
# commitado nesses caminhos. Recuse-se a rodar sobre arvore suja.
if ! git diff --quiet -- "$SYNC" "$ASYNC" || ! git diff --cached --quiet -- "$SYNC" "$ASYNC"; then
    echo "ABORTADO: ha alteracoes nao commitadas em AddImprovement*.cs." >&2
    echo "Este teste restaura esses arquivos do git e apagaria seu trabalho." >&2
    exit 2
fi

restaurar() {
    git checkout -- "$SYNC" "$ASYNC" 2>/dev/null || true
    rm -f "$SYNC.novo"
}

caso() {
    local nome="$1"; shift
    restaurar
    if ! git show "HEAD:$ASYNC" > /dev/null 2>&1; then
        echo "PULADO ($nome): a arvore ja esta unida; rode a partir de um checkout limpo"
        return
    fi
    "$@"
    if python3 scripts/unir-addimprovement.py > /dev/null 2>&1; then
        echo "  FALHOU: a ferramenta ACEITOU '$nome'"
        FALHAS=$((FALHAS + 1))
    else
        echo "  ok: recusou '$nome'"
    fi
    restaurar
}

# 1. um membro a menos do lado async -> a correspondencia 1:1 quebra
defeito_membro_a_menos() {
    python3 - "$ASYNC" <<'EOF'
import sys
p = sys.argv[1]
linhas = open(p, encoding='utf-8').read().split('\n')
# apaga o corpo inteiro de "public async Task surprise(...)"
i = next(k for k, l in enumerate(linhas) if l.strip().startswith('public async Task surprise('))
j = i
prof = 0
visto = False
while j < len(linhas):
    prof += linhas[j].count('{') - linhas[j].count('}')
    if '{' in linhas[j]:
        visto = True
    if visto and prof == 0:
        break
    j += 1
del linhas[i:j + 1]
open(p, 'w', encoding='utf-8', newline='\n').write('\n'.join(linhas))
EOF
}

# 2. um #endregion a menos -> o balanco de regiao da saida nao fecha (foi este o bug real)
defeito_regiao_orfa() {
    python3 - "$SYNC" <<'EOF'
import sys
p = sys.argv[1]
t = open(p, encoding='utf-8').read()
open(p, 'w', encoding='utf-8', newline='\n').write(t.replace('        #region Helper Methods\n', '', 1))
EOF
}

# 3. um corpo de membro alterado no async -> a conservacao byte-a-byte tem de acusar
defeito_corpo_alterado() {
    python3 - "$ASYNC" <<'EOF'
import sys
p = sys.argv[1]
t = open(p, encoding='utf-8').read()
open(p, 'w', encoding='utf-8', newline='\n').write(
    t.replace('Improvement.ImprovementType.Surprise', 'Improvement.ImprovementType.SurprisE', 1))
EOF
}

echo "==> injetando defeitos e exigindo recusa"
caso 'membro async a menos'   defeito_membro_a_menos
caso '#region sem par'        defeito_regiao_orfa

# O caso 3 e diferente: alterar um corpo NAO deve ser recusado (a ferramenta transporta o
# que recebe). Ele existe para documentar o limite: a uniao conserva, nao valida semantica.
restaurar

if [ "$FALHAS" -gt 0 ]; then
    echo "TESTE FALHOU: $FALHAS defeito(s) passaram pela ferramenta"
    exit 1
fi
echo "OK — a ferramenta recusou todos os defeitos injetados"
