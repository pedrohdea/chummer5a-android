#!/usr/bin/env python3
"""Passo 1 da consolidacao: funde AddImprovementAsyncCollection em AddImprovementCollection.

O QUE ELE FAZ, EXATAMENTE

Os dois arquivos sao a mesma logica escrita duas vezes: 348 membros cada, na mesma ordem,
com correspondencia 1:1 de nome (medido — a unica excecao e o construtor). Este passo NAO
funde corpo nenhum: ele so poe os dois conjuntos de membros na MESMA classe, para que o
passo seguinte (o `blnSync` de verdade) possa fundir par a par.

Regras:
  * membro async byte-identico ao sync  -> descartado (o sync ja esta la)
  * construtor async                     -> descartado (identico exceto pelo nome)
  * GetSpiritOrSpriteRatingDivisor       -> fica a versao async (superconjunto: aceita token)
  * demais membros async cujo nome colide -> renomeados com sufixo `Async`
  * chamadas internas aos renomeados      -> reescritas nos corpos async

CONSERVACAO DE MEMBROS (DEC-033): a conta e verificada e a ferramenta aborta se nao fechar.
"""
import os
import re
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), 'lib'))
from csmembros import parse_class  # noqa: E402

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SYNC = os.path.join(RAIZ, 'Chummer', 'Backend', 'Improvements', 'AddImprovementCollection.cs')
ASYNC = os.path.join(RAIZ, 'Chummer', 'Backend', 'Improvements', 'AddImprovementAsyncCollection.cs')

# Membros async que substituem o par sync em vez de conviverem com ele: mesma assinatura
# fora um `CancellationToken token = default` no fim, mesmo tipo de retorno (nao Task).
SUBSTITUEM_O_SYNC = {'GetSpiritOrSpriteRatingDivisor'}

BANNER = '\n'.join([
    '        // ------------------------------------------------------------------',
    '        // Metade assincrona, vinda de AddImprovementAsyncCollection.cs.',
    '        // Aqui ela ainda e codigo duplicado: cada metodo abaixo e o gemeo do',
    '        // sincrono de mesmo nome. A fusao em `blnSync` acontece por par, no',
    '        // passo seguinte.',
    '        // ------------------------------------------------------------------',
])


class Erro(Exception):
    pass


def main():
    pro_s, mem_s, cauda_s, epi_s = parse_class(SYNC)
    pro_a, mem_a, cauda_a, epi_a = parse_class(ASYNC)

    if len(mem_s) != len(mem_a):
        raise Erro('contagem de membros diverge: %d sync x %d async' % (len(mem_s), len(mem_a)))

    identicos = set()
    for a, b in zip(mem_s, mem_a):
        if [l.rstrip() for l in a.lines] == [l.rstrip() for l in b.lines]:
            identicos.add(a.name)

    nomes_sync = {m.name for m in mem_s}
    renomes = {}          # nome async antigo -> novo
    descartados = []      # membros async que nao vao para a saida
    substituidos = []     # membros sync que saem para dar lugar ao async

    for a, b in zip(mem_s, mem_a):
        if b.name == 'AddImprovementAsyncCollection':
            descartados.append(b)
            continue
        if b.name in identicos:
            descartados.append(b)
            continue
        if b.name in SUBSTITUEM_O_SYNC:
            substituidos.append(a.name)
            continue
        if b.name in nomes_sync:
            renomes[b.name] = b.name + 'Async'

    # ------------------------------------------------------------------ verificacoes
    for novo in renomes.values():
        if novo in nomes_sync:
            raise Erro('rename colide com membro sync existente: %s' % novo)
    dup = [k for k in renomes if k + 'Async' in renomes]
    if dup:
        raise Erro('rename ambiguo: %s' % dup)

    # ------------------------------------------------------------------ transformacao
    #
    # Contagem de substituicoes por nome. Um rename so pode acertar a propria declaracao
    # mais as chamadas internas que MEDIMOS antes; qualquer excedente e sinal de que o
    # regex pegou outra coisa (um literal, um simbolo homonimo) e a ferramenta aborta.
    contagem = {}

    def reescrever(texto):
        """Aplica os renomes a um corpo async (declaracao e chamadas)."""
        for antigo, novo in renomes.items():
            texto, n = re.subn(r'(?<![\w.])' + re.escape(antigo) + r'(?=\s*\()', novo, texto)
            if n:
                contagem[antigo] = contagem.get(antigo, 0) + n
        return texto

    # quantas chamadas internas cada nome renomeado tem, medido no arquivo async ORIGINAL
    texto_async_bruto = '\n'.join(b.text for b in mem_a)
    esperado_por_nome = {}
    for antigo in renomes:
        esperado_por_nome[antigo] = len(
            re.findall(r'(?<![\w.])' + re.escape(antigo) + r'(?=\s*\()', texto_async_bruto))

    saida_async = []
    for b in mem_a:
        if b in descartados:
            continue
        if b.name in SUBSTITUEM_O_SYNC:
            continue
        saida_async.append((b, reescrever(b.text)))

    # os que substituem o sync entram no lugar do sync, na posicao original
    substitutos = {}
    for a, b in zip(mem_s, mem_a):
        if b.name in SUBSTITUEM_O_SYNC:
            substitutos[a.name] = reescrever(b.text)

    divergentes = {k: (contagem.get(k, 0), v) for k, v in esperado_por_nome.items()
                   if contagem.get(k, 0) != v}
    if divergentes:
        raise Erro('contagem de renomes diverge do medido: %s' % divergentes)
    sem_declaracao = [k for k, v in esperado_por_nome.items() if v < 1]
    if sem_declaracao:
        raise Erro('renome sem declaracao correspondente: %s' % sem_declaracao)

    # ------------------------------------------------------------------ montagem
    partes = []
    partes.append('\n'.join(pro_s))
    for m in mem_s:
        partes.append(substitutos.get(m.name, m.text))
    partes.append('\n'.join(cauda_s))
    for i, (_, texto) in enumerate(saida_async):
        partes.append(BANNER + '\n' + texto if i == 0 else texto)
    partes.append('\n'.join(cauda_a))
    partes.append('\n'.join(epi_s))

    novo_texto = '\n\n'.join(p for p in partes if p != '')
    if not novo_texto.endswith('\n'):
        novo_texto += '\n'

    # prologo precisa de System.Threading / System.Threading.Tasks
    for u in ('using System.Threading;', 'using System.Threading.Tasks;'):
        if u not in novo_texto:
            novo_texto = novo_texto.replace('using System.Windows.Forms;',
                                            u + '\n' + 'using System.Windows.Forms;', 1)

    destino = SYNC + '.novo'
    with open(destino, 'w', encoding='utf-8', newline='\n') as f:
        f.write(novo_texto)

    # ------------------------------------------------------------------ conservacao
    #
    # Balanco de #region. Custou um build: as linhas soltas entre o ultimo membro e a chave
    # da classe (justamente um `#endregion`) nao pertencem a membro nenhum, e sumiram na
    # primeira versao desta ferramenta. A saida ficou com `#region` sem par — CS1038.
    saida_bruta = open(destino, encoding='utf-8').read()
    abre = len(re.findall(r'^\s*#region\b', saida_bruta, re.M))
    fecha = len(re.findall(r'^\s*#endregion\b', saida_bruta, re.M))
    esperado_regiao = (
        len(re.findall(r'^\s*#region\b', open(SYNC, encoding='utf-8').read(), re.M))
        + len(re.findall(r'^\s*#region\b', open(ASYNC, encoding='utf-8').read(), re.M)))
    if abre != fecha or abre != esperado_regiao:
        raise Erro('CONSERVACAO FALHOU: #region %d x #endregion %d na saida (esperado %d de '
                   'cada); saida preservada em %s' % (abre, fecha, esperado_regiao, destino))

    _, mem_novo, cauda_novo, _ = parse_class(destino)
    esperado = len(mem_s) + len(mem_a) - len(descartados) - len(SUBSTITUEM_O_SYNC & set(substitutos))
    if len(mem_novo) != esperado:
        os.unlink(destino)
        raise Erro('CONSERVACAO FALHOU: saida tem %d membros, esperado %d '
                   '(sync %d + async %d - descartados %d - substituidos %d)'
                   % (len(mem_novo), esperado, len(mem_s), len(mem_a), len(descartados),
                      len(substitutos)))

    # todo membro sync tem de sair byte-identico (exceto os substituidos)
    novos_por_nome = {}
    for m in mem_novo:
        novos_por_nome.setdefault(m.name, []).append(m)
    def chegou_intacto(cands, alvo):
        """O membro pode ter ganhado linhas soltas na frente (um #endregion, o banner);
        o que nao pode e ter perdido ou mudado uma linha sua."""
        alvo = [l.rstrip() for l in alvo]
        for c in cands:
            linhas = [l.rstrip() for l in c.lines]
            if len(linhas) >= len(alvo) and linhas[len(linhas) - len(alvo):] == alvo:
                return True
        return False

    faltando = []
    for m in mem_s:
        if m.name in substitutos:
            continue
        if not chegou_intacto(novos_por_nome.get(m.name, []), m.lines):
            faltando.append(('sync', m.name))
    for b, texto in saida_async:
        nome = renomes.get(b.name, b.name)
        if not chegou_intacto(novos_por_nome.get(nome, []), texto.split('\n')):
            faltando.append(('async', nome))
    if faltando:
        raise Erro('CONSERVACAO FALHOU: membros nao encontrados intactos na saida: %s '
                   '(saida preservada em %s para diagnostico)' % (faltando[:20], destino))

    os.replace(destino, SYNC)
    os.unlink(ASYNC)

    print('membros sync         : %d' % len(mem_s))
    print('membros async        : %d' % len(mem_a))
    print('  identicos, descartados: %d' % len(identicos))
    print('  construtor descartado : 1')
    print('  substituem o sync     : %d (%s)' % (len(substitutos), ', '.join(sorted(substitutos))))
    print('  renomeados com Async  : %d' % len(renomes))
    print('membros na saida     : %d (esperado %d)  OK' % (len(mem_novo), esperado))
    print('AddImprovementAsyncCollection.cs removido')


if __name__ == '__main__':
    try:
        main()
    except Erro as e:
        print('ABORTADO: %s' % e, file=sys.stderr)
        sys.exit(1)
