#!/usr/bin/env python3
"""
Confere que todo tipo parcial emitido em `*.UI.cs` corresponde a um tipo de TOPO.

Por que existe: o extrator montava o mapa de tipos aceitando qualquer indentação, então um
tipo ANINHADO entrava no mapa como se fosse de topo. `GetStockIcon` foi emitido dentro de
`partial struct SHSTOCKICONINFO` — um struct que na verdade vive dentro de `NativeMethods` —
criando no namespace um tipo de topo homônimo que não é o mesmo tipo. Os membros aninhados
que o método usa deixaram de resolver.

Por que o verificador de compilação não pegou: `verificar-ui.sh` classifica TODO `CS0246`
como acoplamento esperado, porque é assim que os tipos de WinForms ausentes se manifestam.
O `CS0246` de `SHSTOCKICONID` — um enum de domínio — caiu no mesmo balde. Classificar
CS0246 por símbolo, como já se faz com CS0103, resolveria de forma mais geral, mas
distinguir "tipo de domínio que sumiu" de "tipo de plataforma que nunca existiu" exige uma
lista de todos os tipos do repositório e erra em ambas as direções durante a migração.

Esta verificação é estrutural em vez de semântica, e por isso não tem falso positivo: ou o
tipo existe declarado a quatro espaços em algum arquivo que não é gerado, ou não existe.
"""

import re
import pathlib
import sys

RE_EMITIDA = re.compile(
    r'^    (?:public|internal)(?:\s+\w+)*\s+partial\s+(?:class|struct)\s+(\w+)\s*$', re.M)
RE_TOPO = re.compile(
    r'^    (?:public|internal)'
    r'(?:\s+(?:sealed|abstract|static|readonly|unsafe|partial))*'
    r'\s+(?:class|struct|interface)\s+(\w+)\b', re.M)

raiz = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else '.')

topo = set()
for p in (raiz / 'Chummer').rglob('*.cs'):
    if '/obj/' in str(p) or '/bin/' in str(p) or p.name.endswith('.UI.cs'):
        continue
    topo |= set(RE_TOPO.findall(p.read_text(encoding='utf-8-sig', errors='replace')))

problemas = []
for p in sorted((raiz / 'Chummer' / 'Controls').rglob('*.UI.cs')):
    for nome in RE_EMITIDA.findall(p.read_text(encoding='utf-8-sig', errors='replace')):
        if nome not in topo:
            problemas.append((p, nome))

if problemas:
    print('\033[1;31mFALHA: parcial emitida para tipo que não é de topo\033[0m\n')
    for p, nome in problemas:
        print(f'  {p}: "partial ... {nome}" — {nome} não é declarado a quatro espaços '
              f'em nenhum arquivo não gerado. Provavelmente é um tipo aninhado.')
    sys.exit(1)

print('Parciais emitidas: todas correspondem a tipos de topo.')
