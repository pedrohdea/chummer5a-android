#!/usr/bin/env python3
"""
Extrai membros dependentes de WinForms de uma classe de domínio para uma classe parcial.

Contexto: várias classes de `Chummer/Backend/` misturam regras de Shadowrun com métodos que
constroem `TreeNode`, recebem `TreeView`/`ContextMenuStrip` ou `Control`. Os métodos de UI
não estão agrupados no fim do arquivo — estão interleaved com o domínio, então recortar por
linha não funciona.

Esta ferramenta separa por membro, produzindo:

  Chummer/Backend/<...>/Tipo.cs        domínio, com `partial` acrescentado à declaração
  Chummer/Controls/Dominio/Tipo.UI.cs  a metade que depende de WinForms

No projeto legado as duas metades voltam a ser uma classe só, então nenhum chamador muda —
o que preserva o build que gera os artefatos dourados (DEC-013). No `Chummer.Core`, só a
metade de domínio existe.

Uso:
    python3 scripts/extrair-ui.py Chummer/Backend/Equipment/Weapon.cs

Segurança: a extração é abortada se as chaves de qualquer uma das metades não fecharem.
Vale mais recusar do que produzir um arquivo sutilmente quebrado numa base de 350 mil linhas.
"""

import os
import re
import sys

# Tipos cuja presença numa assinatura marca o membro como dependente de UI.
#
# `Color`, `Point`, `Size` e `Rectangle` NÃO entram: vivem em System.Drawing.Primitives,
# que faz parte do framework compartilhado e existe sob net9.0. Só `Image`, `Bitmap` e
# `Icon` exigem System.Drawing.Common.
TIPOS_UI = [
    'TreeNode', 'TreeView', 'TreeNodeCollection', 'ContextMenuStrip', 'ToolStripItem',
    'ToolStripMenuItem', 'Control', 'Form', 'IWin32Window', 'ListViewItem', 'ListViewGroup',
    'ComboBox', 'ListBox', 'ElasticComboBox', 'ToolTip', 'DialogResult', 'MessageBoxButtons',
    'MessageBoxIcon', 'CursorWait', 'LoadingBar', 'ThreadSafeForm', 'RightToLeft',
    'Image', 'Bitmap', 'Icon',
]
RE_TIPO_UI = re.compile(r'\b(' + '|'.join(TIPOS_UI) + r')\b')

RE_MEMBRO = re.compile(r'^\s{8}(\[.*\]\s*)?((public|private|internal|protected)\b.*)')


def limpa(linha):
    """Remove literais de string/char e comentários, para a contagem de chaves.

    Interpolação não é interpretada: tudo entre aspas é tratado como opaco. Falha apenas se
    um buraco de interpolação contiver aspas, que é raro — e a verificação de balanceamento
    no fim pega o caso."""
    saida, i, n = [], 0, len(linha)
    while i < n:
        c = linha[i]
        if c == '/' and i + 1 < n and linha[i + 1] == '/':
            break
        if c == '@' and i + 1 < n and linha[i + 1] == '"':
            i += 2
            while i < n:
                if linha[i] == '"':
                    if i + 1 < n and linha[i + 1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                i += 1
            continue
        if c == '"':
            i += 1
            while i < n:
                if linha[i] == '\\':
                    i += 2
                    continue
                if linha[i] == '"':
                    i += 1
                    break
                i += 1
            continue
        if c == "'":
            i += 1
            while i < n:
                if linha[i] == '\\':
                    i += 2
                    continue
                if linha[i] == "'":
                    i += 1
                    break
                i += 1
            continue
        saida.append(c)
        i += 1
    return ''.join(saida)


def balanceado(linhas):
    return sum(limpa(l).count('{') - limpa(l).count('}') for l in linhas) == 0


def fim_do_membro(linhas, inicio):
    """Devolve o índice (exclusivo) do fim do membro que começa em `inicio`.

    Trata corpo com chaves e membros de expressão (`=> ...;`)."""
    prof, viu_chave = 0, False
    i = inicio
    while i < len(linhas):
        c = limpa(linhas[i])
        if not viu_chave and '{' not in c and c.rstrip().endswith(';'):
            return i + 1                      # membro de expressão, numa ou mais linhas
        prof += c.count('{') - c.count('}')
        if '{' in c:
            viu_chave = True
        if viu_chave and prof <= 0:
            return i + 1
        i += 1
    return len(linhas)


def inicio_com_docs(linhas, i):
    """Recua para incluir comentários /// e atributos que precedem o membro."""
    j = i
    while j > 0:
        anterior = linhas[j - 1].strip()
        if anterior.startswith('///') or anterior.startswith('//') or \
           (anterior.startswith('[') and anterior.endswith(']')):
            j -= 1
        else:
            break
    return j


def extrair(caminho, destino_dir='Chummer/Controls/Dominio'):
    linhas = open(caminho, encoding='utf-8-sig').read().split('\n')

    m = re.search(r'^(\s*)(public|internal)(\s+sealed)?(\s+abstract)?\s+class\s+(\w+)',
                  '\n'.join(linhas), re.M)
    if not m:
        print(f'  {caminho}: declaração de classe não encontrada, ignorado')
        return None
    tipo = m.group(5)

    # Localiza os membros dependentes de UI.
    marcados, i = [], 0
    while i < len(linhas):
        mm = RE_MEMBRO.match(linhas[i])
        if mm:
            fim = fim_do_membro(linhas, i)
            # A assinatura vai da declaração até a primeira `{` ou `=>`.
            assinatura = []
            for k in range(i, min(fim, i + 12)):
                assinatura.append(linhas[k])
                if '{' in limpa(linhas[k]) or '=>' in linhas[k]:
                    break
            if RE_TIPO_UI.search(limpa(' '.join(assinatura))):
                marcados.append((inicio_com_docs(linhas, i), fim))
            i = fim
        else:
            i += 1

    if not marcados:
        print(f'  {tipo}: nenhum membro de UI encontrado')
        return None

    # Funde intervalos que se tocam e recorta.
    marcados.sort()
    fundidos = [list(marcados[0])]
    for a, b in marcados[1:]:
        if a <= fundidos[-1][1]:
            fundidos[-1][1] = max(fundidos[-1][1], b)
        else:
            fundidos.append([a, b])

    extraidas, restantes, corte = [], [], 0
    for a, b in fundidos:
        restantes.extend(linhas[corte:a])
        extraidas.extend(linhas[a:b])
        corte = b
    restantes.extend(linhas[corte:])

    if not balanceado(extraidas):
        print(f'  {tipo}: ABORTADO — chaves não fecham no trecho extraído')
        return None

    cabecalho = []
    for l in linhas:
        cabecalho.append(l)
        if l.startswith('using ') or l.strip() == '':
            continue
        if l.startswith('namespace'):
            break
    licenca = []
    for l in linhas:
        if l.startswith('using '):
            break
        licenca.append(l)
    usings = [l for l in linhas if l.startswith('using ')]
    ns = next((l for l in linhas if l.startswith('namespace')), 'namespace Chummer')

    # --- metade de domínio: `partial` na classe ---
    #
    # O `using System.Windows.Forms` só é removido se o que sobrou realmente não usar mais
    # nada de WinForms. A detecção de membros olha a ASSINATURA; um método como
    # `Remove(bool blnConfirmDelete)` não tem tipo de UI na assinatura mas usa
    # `MessageBoxButtons` no corpo, e portanto continua precisando do using.
    #
    # Remover o using cedo demais quebrou o build net48 em 17 arquivos, e — pior — fez o
    # censo parecer melhor do que a realidade, escondendo acoplamento que continua lá.
    corpo_restante = '\n'.join(restantes).split('namespace ', 1)[-1]
    ainda_usa_ui = RE_TIPO_UI.search(corpo_restante) or re.search(
        r'\b(MessageBoxButtons|MessageBoxIcon|MessageBoxDefaultButton|Cursors|SendKeys|Clipboard)\b',
        corpo_restante)

    dominio = []
    for l in restantes:
        if l.startswith('using System.Windows.Forms;') and not ainda_usa_ui:
            continue
        l = re.sub(r'^(\s*)(public|internal)((?:\s+sealed)?(?:\s+abstract)?)\s+class\s+' + tipo + r'\b',
                   r'\1\2\3 partial class ' + tipo, l)
        dominio.append(l)
    if not balanceado(dominio):
        print(f'  {tipo}: ABORTADO — chaves não fecham no que sobrou')
        return None
    open(caminho, 'w', encoding='utf-8').write('\n'.join(dominio))

    # --- metade de UI ---
    os.makedirs(destino_dir, exist_ok=True)
    saida = os.path.join(destino_dir, f'{tipo}.UI.cs')
    nota = (f'// Metade dependente de WinForms de {tipo}, separada do domínio durante a\n'
            f'// extração do núcleo (DEC-023). A metade de regras vive em {caminho}.\n'
            f'//\n'
            f'// As duas são `partial class {tipo}`: no projeto legado voltam a ser uma classe\n'
            f'// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.\n')
    # A declaração da classe parcial precisa ser emitida aqui. Esquecê-la deixa o arquivo
    # com uma chave de fechamento a mais e produz CS1022 — foi o primeiro defeito que o
    # verificador de sintaxe pegou.
    corpo = '\n'.join(licenca) + '\n' + nota + '\n' + '\n'.join(usings) + '\n\n' + ns + '\n{\n'
    corpo += f'    public partial class {tipo}\n    {{\n'
    corpo += '\n'.join(extraidas).rstrip() + '\n    }\n}\n'
    open(saida, 'w', encoding='utf-8').write(corpo)

    print(f'  {tipo}: {len(extraidas)} linhas extraídas -> {saida}')
    return saida


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    for arg in sys.argv[1:]:
        extrair(arg)
