#!/usr/bin/env python3
"""Passo 2 da consolidacao: troca um par sync/async por um nucleo `blnSync` unico.

    python3 scripts/fundir-par.py NOME arquivo-com-o-nucleo.cs

Ele encontra os membros `NOME` e `NOMEAsync` em AddImprovementCollection.cs e os
substitui por:

    public void NOME(XmlNode bonusNode)              -> Utils.SafelyRunSynchronously(...)
    private async Task NOMECoreAsync(bool blnSync,   -> o texto que voce escreveu
                                     XmlNode, token)
    public Task NOMEAsync(XmlNode, token)            -> return NOMECoreAsync(false, ...)

O comentario de linha que precedia o metodo sincrono e preservado nos tres.

VERIFICACOES (aborta sem escrever se qualquer uma falhar):
  * existe exatamente um `NOME` e um `NOMEAsync`
  * o nucleo declara `NOMECoreAsync` e recebe `bool blnSync`
  * a saida reabre com 3 membros no lugar dos 2, e nenhum outro membro mudou
  * o balanco de #region nao muda
"""
import os
import re
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), 'lib'))
from csmembros import parse_class  # noqa: E402

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ALVO = os.path.join(RAIZ, 'Chummer', 'Backend', 'Improvements', 'AddImprovementCollection.cs')


def comentario_de_cabecalho(m):
    """As linhas de comentario que precedem a assinatura, para nao se perderem."""
    fora = []
    for l in m.lines:
        s = l.strip()
        if s.startswith('//'):
            fora.append(l)
            continue
        break
    return fora


def main():
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2
    nome, caminho_nucleo = sys.argv[1], sys.argv[2]
    nucleo = open(caminho_nucleo, encoding='utf-8').read().rstrip('\n')

    if 'bool blnSync' not in nucleo:
        print('ABORTADO: o nucleo nao recebe `bool blnSync`', file=sys.stderr)
        return 1
    if nome + 'CoreAsync' not in nucleo:
        print('ABORTADO: o nucleo nao declara %sCoreAsync' % nome, file=sys.stderr)
        return 1

    texto = open(ALVO, encoding='utf-8').read()
    linhas = texto.split('\n')
    _, mem, _, _ = parse_class(ALVO)

    achados = [m for m in mem if m.name == nome]
    achados_a = [m for m in mem if m.name == nome + 'Async']
    if len(achados) != 1 or len(achados_a) != 1:
        print('ABORTADO: esperava 1 %s e 1 %sAsync, achei %d e %d'
              % (nome, nome, len(achados), len(achados_a)), file=sys.stderr)
        return 1
    ms, ma = achados[0], achados_a[0]
    if ms.start > ma.start:
        print('ABORTADO: o membro sincrono viria depois do assincrono', file=sys.stderr)
        return 1

    cab = comentario_de_cabecalho(ms)
    cab_a = comentario_de_cabecalho(ma)

    stub_sync = cab + [
        '        public void %s(XmlNode bonusNode)' % nome,
        '        {',
        '            Utils.SafelyRunSynchronously(() => %sCoreAsync(true, bonusNode));' % nome,
        '        }',
        '',
    ] + nucleo.split('\n')

    stub_async = cab_a + [
        '        public Task %sAsync(XmlNode bonusNode, CancellationToken token = default)' % nome,
        '        {',
        '            return %sCoreAsync(false, bonusNode, token);' % nome,
        '        }',
    ]

    novas = (linhas[:ms.start] + stub_sync + linhas[ms.end + 1:ma.start]
             + stub_async + linhas[ma.end + 1:])
    novo_texto = '\n'.join(novas)

    regiao_antes = (len(re.findall(r'^\s*#region\b', texto, re.M)),
                    len(re.findall(r'^\s*#endregion\b', texto, re.M)))
    regiao_depois = (len(re.findall(r'^\s*#region\b', novo_texto, re.M)),
                     len(re.findall(r'^\s*#endregion\b', novo_texto, re.M)))
    if regiao_antes != regiao_depois:
        print('ABORTADO: balanco de #region mudou %s -> %s' % (regiao_antes, regiao_depois),
              file=sys.stderr)
        return 1

    destino = ALVO + '.novo'
    with open(destino, 'w', encoding='utf-8', newline='\n') as f:
        f.write(novo_texto)

    _, mem_novo, _, _ = parse_class(destino)
    if len(mem_novo) != len(mem) + 1:
        os.unlink(destino)
        print('ABORTADO: a saida tem %d membros, esperado %d'
              % (len(mem_novo), len(mem) + 1), file=sys.stderr)
        return 1
    nomes_novos = [m.name for m in mem_novo]
    for exigido in (nome, nome + 'Async', nome + 'CoreAsync'):
        if nomes_novos.count(exigido) != 1:
            os.unlink(destino)
            print('ABORTADO: a saida tem %d membros chamados %s'
                  % (nomes_novos.count(exigido), exigido), file=sys.stderr)
            return 1
    # nenhum OUTRO membro pode ter mudado
    intocados_antes = {}
    for m in mem:
        if m.name in (nome, nome + 'Async'):
            continue
        intocados_antes.setdefault(m.name, []).append([l.rstrip() for l in m.lines])
    intocados_depois = {}
    for m in mem_novo:
        if m.name in (nome, nome + 'Async', nome + 'CoreAsync'):
            continue
        intocados_depois.setdefault(m.name, []).append([l.rstrip() for l in m.lines])
    if intocados_antes != intocados_depois:
        so_antes = set(intocados_antes) - set(intocados_depois)
        so_depois = set(intocados_depois) - set(intocados_antes)
        mudados = [k for k in set(intocados_antes) & set(intocados_depois)
                   if intocados_antes[k] != intocados_depois[k]]
        os.unlink(destino)
        print('ABORTADO: outros membros mudaram. so antes=%s so depois=%s mudados=%s'
              % (sorted(so_antes)[:5], sorted(so_depois)[:5], mudados[:5]), file=sys.stderr)
        return 1

    os.replace(destino, ALVO)
    print('%s: %d + %d linhas -> %d (nucleo) + 5 + 4  |  membros %d -> %d'
          % (nome, len(ms.lines), len(ma.lines), len(nucleo.split('\n')),
             len(mem), len(mem_novo)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
