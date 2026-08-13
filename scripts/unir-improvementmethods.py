#!/usr/bin/env python3
"""Acompanha unir-addimprovement.py: aponta GetAsyncMethod para a classe unida.

Depois que AddImprovementAsyncCollection deixou de existir, `GetAsyncMethod` passa a
receber um `AddImprovementCollection` e a devolver os metodos com sufixo `Async`.

Verificacao: todo alvo do switch precisa existir, com o sufixo, na classe unida — se um
so faltar, a ferramenta aborta sem escrever nada.
"""
import os
import re
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), 'lib'))
from csmembros import parse_class  # noqa: E402

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLASSE = os.path.join(RAIZ, 'Chummer', 'Backend', 'Improvements', 'AddImprovementCollection.cs')
METODOS = os.path.join(RAIZ, 'Chummer', 'Backend', 'Improvements', 'ImprovementMethods.cs')


def main():
    txt = open(METODOS, encoding='utf-8').read()
    _, mem, _, _ = parse_class(CLASSE)
    nomes = {m.name for m in mem}

    marca = 'public static Func<XmlNode, CancellationToken, Task> GetAsyncMethod'
    if marca not in txt:
        print('nada a fazer: GetAsyncMethod ja aponta para a classe unida')
        return 0
    i = txt.index(marca)
    head, tail = txt[:i], txt[i:]

    alvos = set(re.findall(r'objImprovementAsyncAdder\.(\w+);', tail))
    alvos |= set(re.findall(r'AddImprovementAsyncCollection\.(\w+);', tail))
    if not alvos:
        print('nada a fazer: switch ja reescrito')
        return 0
    # idempotencia: se todo alvo JA tem o sufixo e existe na classe unida, nao ha o que fazer
    if all(n.endswith('Async') and n in nomes for n in alvos):
        print('nada a fazer: switch ja aponta para os metodos Async da classe unida')
        return 0
    faltando = sorted(n for n in alvos if n + 'Async' not in nomes)
    if faltando:
        print('ABORTADO: sem correspondente Async na classe unida: %s' % faltando[:10],
              file=sys.stderr)
        return 1

    tail, n1 = re.subn(r'(objImprovementAsyncAdder\.)(\w+);', r'\1\2Async;', tail)
    tail, n2 = re.subn(r'AddImprovementAsyncCollection\.(\w+);',
                       r'AddImprovementCollection.\1Async;', tail)
    tail = tail.replace('AddImprovementAsyncCollection objImprovementAsyncAdder',
                        'AddImprovementCollection objImprovementAsyncAdder')
    novo = head + tail
    novo = novo.replace('Gets an AddImprovementAsyncCollection function based on its name.',
                        'Gets an asynchronous AddImprovementCollection function based on its name.')
    if 'AddImprovementAsyncCollection' in novo:
        print('ABORTADO: sobrou referencia a AddImprovementAsyncCollection', file=sys.stderr)
        return 1
    with open(METODOS, 'w', encoding='utf-8', newline='\n') as f:
        f.write(novo)
    print('GetAsyncMethod reescrito: %d de instancia, %d estatico (%d alvos distintos)'
          % (n1, n2, len(alvos)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
