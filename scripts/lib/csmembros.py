#!/usr/bin/env python3
"""Leitor de membros de topo de uma classe C#, por profundidade de chave.

Existe para as ferramentas de consolidacao do porte. NAO e um parser de C#: e um
localizador de fronteiras de membro que so precisa acertar onde um membro comeca e
termina. Tudo o mais (corpo, comentarios, atributos) e transportado literalmente.

A garantia que as ferramentas construidas sobre este modulo devem dar e a de
CONSERVACAO DE MEMBROS (DEC-033): o que entra tem de sair, contado.
"""
import re


def scrub(line, state):
    """Devolve a linha sem strings, chars e comentarios — so para contar chaves.

    `state` e um dict com a chave 'block', que sobrevive entre linhas (comentario /* */).
    """
    out = []
    i = 0
    n = len(line)
    while i < n:
        if state['block']:
            j = line.find('*/', i)
            if j < 0:
                break
            state['block'] = False
            i = j + 2
            continue
        c = line[i]
        if c == '/' and i + 1 < n and line[i + 1] == '/':
            break
        if c == '/' and i + 1 < n and line[i + 1] == '*':
            state['block'] = True
            i += 2
            continue
        if c == '@' and i + 1 < n and line[i + 1] == '"':
            i += 2
            while i < n:
                if line[i] == '"':
                    if i + 1 < n and line[i + 1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                i += 1
            out.append('""')
            continue
        if c == '"':
            i += 1
            while i < n:
                if line[i] == '\\':
                    i += 2
                    continue
                if line[i] == '"':
                    i += 1
                    break
                i += 1
            out.append('""')
            continue
        if c == "'":
            i += 1
            while i < n:
                if line[i] == '\\':
                    i += 2
                    continue
                if line[i] == "'":
                    i += 1
                    break
                i += 1
            out.append("''")
            continue
        out.append(c)
        i += 1
    return ''.join(out)


class Member(object):
    __slots__ = ('name', 'start', 'end', 'lines')

    def __init__(self, name, start, end, lines):
        self.name = name
        self.start = start
        self.end = end
        self.lines = lines

    @property
    def text(self):
        return '\n'.join(self.lines)

    def __repr__(self):
        return '<Member %s L%d-%d>' % (self.name, self.start + 1, self.end + 1)


NAME_RE_M = re.compile(r'\b([A-Za-z_]\w*)\s*(?:<[^<>()]*>)?\s*\(')
NAME_RE_F = re.compile(r'\b([A-Za-z_]\w*)\s*(?:\{|=>|=|;)')
KEYWORDS = {'if', 'for', 'foreach', 'while', 'switch', 'return', 'using', 'lock', 'catch',
            'else', 'try', 'do', 'get', 'set', 'new', 'throw', 'yield', 'await'}


def _extract_name(line):
    s = scrub(line, {'block': False}).strip()
    m = NAME_RE_M.search(s)
    if m and m.group(1) not in KEYWORDS:
        return m.group(1)
    m = NAME_RE_F.search(s)
    if m and m.group(1) not in KEYWORDS:
        return m.group(1)
    return s[:40]


def parse_class(path):
    """Devolve (prologo, membros, cauda, epilogo).

    prologo: linhas ate (e incluindo) a que abre a classe
    membros: lista de Member, na ordem do arquivo
    cauda  : linhas soltas entre o ultimo membro e o `}` da classe — tipicamente um
             `#endregion`. Elas NAO pertencem a membro nenhum e ja custaram um build:
             largadas, o arquivo fica com `#region` sem par (CS1038).
    epilogo: linhas a partir da que fecha a classe
    Linhas em branco entre membros sao descartadas — e por isso que a reconstrucao nao e
    byte-a-byte, mas cada MEMBRO e.
    """
    text = open(path, encoding='utf-8-sig').read()
    lines = text.split('\n')
    state = {'block': False}
    depth = 0
    members = []
    prologue_end = None
    class_end = None
    tail_start = None
    cur_start = None
    pending_start = None
    member_open = False
    i = 0
    n = len(lines)

    while i < n:
        raw = lines[i]
        clean = scrub(raw, state)
        stripped = raw.strip()
        opens = clean.count('{')
        closes = clean.count('}')

        if depth < 2:
            depth += opens - closes
            if depth == 2 and prologue_end is None:
                prologue_end = i
            i += 1
            continue

        if depth == 2 and not member_open:
            if stripped == '':
                i += 1
                continue
            if stripped.startswith(('//', '/*', '*', '[', '#')):
                if pending_start is None:
                    pending_start = i
                i += 1
                continue
            if stripped.startswith('}'):
                depth += opens - closes
                if depth < 2:
                    class_end = i
                    tail_start = pending_start
                    break
                i += 1
                continue
            cur_start = pending_start if pending_start is not None else i
            pending_start = None
            member_open = True

        newdepth = depth + opens - closes
        if member_open:
            terminated = False
            if newdepth == 2 and depth == 2:
                cs = clean.rstrip()
                if cs.endswith(';') or cs.endswith('}'):
                    terminated = True
            elif newdepth == 2 and depth > 2:
                terminated = True
            if newdepth < 2:
                class_end = i
                break
            if terminated:
                members.append(Member(_extract_name(lines[cur_start if pending_start is None else cur_start]),
                                      cur_start, i, lines[cur_start:i + 1]))
                # o nome tem de sair da primeira linha de CODIGO, nao de um comentario
                m = members[-1]
                for l in m.lines:
                    s = l.strip()
                    if s and not s.startswith(('//', '/*', '*', '[', '#')):
                        m.name = _extract_name(l)
                        break
                member_open = False
                cur_start = None
        depth = newdepth
        i += 1

    if class_end is None:
        class_end = n - 1
    cauda = [l for l in lines[tail_start:class_end] if l.strip()] if tail_start is not None else []
    return lines[:prologue_end + 1], members, cauda, lines[class_end:]
