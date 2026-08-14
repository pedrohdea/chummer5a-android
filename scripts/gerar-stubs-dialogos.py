#!/usr/bin/env python3
"""
Gera stubs dos diálogos de seleção, para o SPIKE de compilar o domínio para Android.

ISTO NÃO É O DESENHO FINAL. Deixe isso claro antes de qualquer coisa.

O desenho final é a abstração de solicitação/resposta de DEC-003 e DEC-026: o domínio emite
um pedido de escolha, a camada de apresentação resolve. Esses stubs são um ATALHO com
propósito único e prazo curto — responder, em horas em vez de semanas, a pergunta:

    carregar um .chum5 no Android toca algum diálogo?

A hipótese é que não. Carregar é parser XML e construção de objetos; os diálogos aparecem ao
CRIAR e MODIFICAR coisas que exigem escolha do usuário. Se a hipótese estiver certa, dá para
ter um APK que abre uma ficha muito antes de a abstração de seleção estar pronta — e cada
stub que for chamado em tempo de execução lança, apontando exatamente o que falta abstrair.

Por que gerar em vez de escrever à mão: as assinaturas são extraídas dos formulários REAIS
em Chummer/Forms/. Escrever 26 classes e 57 membros à mão é onde se erra o tipo de retorno e
se descobre no CI cinco minutos depois.

Saída: src/Chummer.Compat/DialogStubs.g.cs

O diretório src/Chummer.Compat/ é deliberado: o projeto legado compila `src/Chummer.Core/**`
(DEC-013), então qualquer coisa fora dessa pasta é automaticamente invisível para ele. Sem
isso, os stubs colidiriam com os tipos WinForms de verdade no build net48.
"""

import os
import re
import sys
import collections

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
BACKEND = os.path.join(RAIZ, 'Chummer', 'Backend')
# As metades de UI extraídas ainda chamam diálogo, e a sondagem executável as
# compila junto com o domínio — então elas também entram no levantamento.
DOMINIO_UI = (os.path.join(RAIZ, 'Chummer', 'Controls', 'Dominio'),
              os.path.join(RAIZ, 'Chummer', 'Controls', 'Extensions'))
FORMS = os.path.join(RAIZ, 'Chummer', 'Forms')
SAIDA = os.path.join(RAIZ, 'src', 'Chummer.Compat', 'DialogStubs.g.cs')

RE_USO = re.compile(r'ThreadSafeForm<([A-Za-z0-9_]+)>')
RE_MEMBRO_USADO = re.compile(r'\.MyForm\.([A-Za-z0-9_]+)')

# Membros que o domínio alcança SEM passar por .MyForm.: inicializador de objeto
# (`new SelectText { Description = ... }`) e variável local tipada (`SelectItem frm = ...;
# frm.Opacity`). A primeira versão do gerador só olhava `.MyForm.X` e perdia 5 membros —
# o compilador cobrou depois, que é exatamente o que este gerador existe para evitar.
RE_INICIALIZADOR = r'new\s+{tipo}\s*(?:\([^()]*\))?\s*\{{([^{{}}]*)\}}'
RE_ATRIB_INIC = re.compile(r'(?:[,{]|^)\s*([A-Za-z_]\w*)\s*=(?!=)', re.M)


def arquivos(raiz, ext='.cs'):
    for dir_, _, nomes in os.walk(raiz):
        if '/obj/' in dir_ or '/bin/' in dir_:
            continue
        for n in nomes:
            if n.endswith(ext):
                yield os.path.join(dir_, n)


def ler(p):
    with open(p, encoding='utf-8-sig', errors='replace') as f:
        return f.read()


def levantar_uso():
    """Quais diálogos o domínio usa, e quais membros de cada um."""
    tipos, membros = set(), set()
    textos = []
    for raiz in (BACKEND,) + DOMINIO_UI:
        for p in arquivos(raiz):
            t = ler(p)
            textos.append(t)
            # 'T' vem da própria declaração de ThreadSafeForm<T>, não é diálogo.
            tipos.update(x for x in RE_USO.findall(t) if len(x) > 1)
            membros.update(RE_MEMBRO_USADO.findall(t))

    for t in textos:
        for tipo in tipos:
            for corpo in re.finditer(RE_INICIALIZADOR.format(tipo=tipo), t):
                membros.update(RE_ATRIB_INIC.findall(corpo.group(1)))
            # variável local declarada com o tipo do diálogo
            for var in re.findall(rf'\b{tipo}\s+([a-z][A-Za-z0-9_]*)\s*[=;,)]', t):
                membros.update(re.findall(rf'\b{var}\.([A-Za-z_]\w*)', t))
    return tipos, membros


def assinaturas_reais(tipos, membros):
    """Extrai as assinaturas verdadeiras dos formulários em Chummer/Forms/.

    Só os membros que o domínio realmente chama entram no stub. Copiar a classe inteira
    arrastaria a implementação WinForms junto, que é justamente o que se quer evitar.
    """
    achado = collections.defaultdict(dict)
    for p in arquivos(FORMS):
        t = ler(p)
        for tipo in tipos:
            if not re.search(rf'\bclass\s+{tipo}\b', t):
                continue
            for m in membros:
                # propriedade ou método público, capturando o tipo de retorno
                pat = re.compile(
                    rf'^\s*public\s+(?!class|struct)([\w<>,\[\]\?\. ]+?)\s+{m}\s*(\(|\{{|=>|$)',
                    re.M)
                mm = pat.search(t)
                if mm and m not in achado[tipo]:
                    retorno = mm.group(1).strip()
                    ehmetodo = mm.group(2) == '('
                    achado[tipo][m] = (retorno, ehmetodo)
    return achado


def valor_padrao(tipo):
    t = tipo.replace('?', '').strip()
    if t in ('void',):
        return None
    if t in ('string',):
        return '""'
    if t in ('bool',):
        return 'false'
    if t in ('int', 'decimal', 'double', 'float', 'long', 'short', 'byte'):
        return '0'
    return 'default'


# Enums aninhados nos formulários reais, alcançados como SelectArt.Mode.Art. Não saem do
# levantamento de membros porque não são membros de instância; a lista vem do formulário.
ANINHADOS = {
    'SelectArt': {'Mode': ['Art', 'Enhancement', 'Enchantment', 'Ritual']},
}


def emitir(achado, tipos):
    L = []
    L.append('// <auto-generated />')
    L.append('// GERADO por scripts/gerar-stubs-dialogos.py — não edite à mão.')
    L.append('//')
    L.append('// SPIKE, não desenho final. Ver o cabeçalho do gerador e DEC-037.')
    L.append('//')
    L.append('// Cada membro lança NotSupportedException. Isso é intencional: se carregar um')
    L.append('// .chum5 no Android atingir um destes, a exceção diz exatamente qual diálogo')
    L.append('// precisa ser abstraído de verdade — que é a informação que este spike existe')
    L.append('// para produzir.')
    L.append('')
    L.append('using System;')
    L.append('using System.Threading;')
    L.append('using System.Threading.Tasks;')
    L.append('using System.Collections.Generic;')
    L.append('using System.Drawing;')
    L.append('using System.Xml;')
    L.append('using Chummer.Backend.Equipment;')
    L.append('')
    L.append('namespace Chummer')
    L.append('{')
    L.append('    /// <summary>Substituto de System.Windows.Forms.DialogResult.</summary>')
    L.append('    public enum DialogResult')
    L.append('    {')
    L.append('        None = 0, OK = 1, Cancel = 2, Abort = 3,')
    L.append('        Retry = 4, Ignore = 5, Yes = 6, No = 7')
    L.append('    }')
    L.append('')
    L.append('    /// <summary>')
    L.append('    /// Superfície que os diálogos herdam de Form/Control e que o domínio usa.')
    L.append('    /// Não é reimplementação: só o suficiente para vincular, lançando se chamado.')
    L.append('    /// </summary>')
    L.append('    public abstract class DialogStubBase : IDisposable')
    L.append('    {')
    L.append('        public double Opacity')
    L.append('        {')
    L.append('            get => throw new NotSupportedException("Diálogo não abstraído: " + GetType().Name);')
    L.append('            set => throw new NotSupportedException("Diálogo não abstraído: " + GetType().Name);')
    L.append('        }')
    L.append('')
    L.append('        public string Text')
    L.append('        {')
    L.append('            get => throw new NotSupportedException("Diálogo não abstraído: " + GetType().Name);')
    L.append('            set => throw new NotSupportedException("Diálogo não abstraído: " + GetType().Name);')
    L.append('        }')
    L.append('')
    L.append('        public object Tag')
    L.append('        {')
    L.append('            get => throw new NotSupportedException("Diálogo não abstraído: " + GetType().Name);')
    L.append('            set => throw new NotSupportedException("Diálogo não abstraído: " + GetType().Name);')
    L.append('        }')
    L.append('')
    L.append('        public virtual void Dispose() { }')
    L.append('    }')
    L.append('')
    L.append('    /// <summary>')
    L.append('    /// Espelha as extensões DoThreadSafe* de WinFormsExtensions, que o domínio chama')
    L.append('    /// sobre o formulário. Sem afinidade de thread não há o que proteger; lançam porque')
    L.append('    /// chegar aqui significa que um diálogo real foi tocado.')
    L.append('    /// </summary>')
    L.append('    public static class DialogStubExtensions')
    L.append('    {')
    for nome, assinatura, retorno in (
            ('DoThreadSafe', 'Action<T> funcToRun', 'void'),
            ('DoThreadSafe', 'Action<T, CancellationToken> funcToRun', 'void'),
            ('DoThreadSafeAsync', 'Action<T> funcToRun', 'Task'),
            ('DoThreadSafeAsync', 'Action<T, CancellationToken> funcToRun', 'Task'),
            ('DoThreadSafeFunc', 'Func<T, TResult> funcToRun', 'TResult'),
            ('DoThreadSafeFunc', 'Func<T, CancellationToken, TResult> funcToRun', 'TResult'),
            ('DoThreadSafeFuncAsync', 'Func<T, TResult> funcToRun', 'Task<TResult>'),
            ('DoThreadSafeFuncAsync', 'Func<T, CancellationToken, TResult> funcToRun', 'Task<TResult>')):
        genericos = '<T, TResult>' if 'TResult' in retorno else '<T>'
        L.append(f'        public static {retorno} {nome}{genericos}(this T objForm, {assinatura},')
        L.append('            CancellationToken token = default) where T : DialogStubBase')
        L.append('            => throw new NotSupportedException(')
        L.append('                "Diálogo de seleção ainda não abstraído: " + typeof(T).Name);')
        L.append('')
    L.append('    }')
    L.append('')
    L.append('    /// <summary>')
    L.append('    /// Substituto de ThreadSafeForm&lt;T&gt;. A afinidade de thread do WinForms não')
    L.append('    /// existe no destino, então não há nada a proteger — sobra a forma da API.')
    L.append('    /// </summary>')
    L.append('    public sealed class ThreadSafeForm<T> : IDisposable where T : DialogStubBase, new()')
    L.append('    {')
    L.append('        public T MyForm { get; private set; }')
    L.append('')
    L.append('        private ThreadSafeForm(T objForm) { MyForm = objForm; }')
    L.append('')
    L.append('        public static ThreadSafeForm<T> Get(Func<T> funcCreator)')
    L.append('            => new ThreadSafeForm<T>(funcCreator());')
    L.append('')
    L.append('        public static Task<ThreadSafeForm<T>> GetAsync(Func<T> funcCreator,')
    L.append('            CancellationToken token = default)')
    L.append('            => Task.FromResult(new ThreadSafeForm<T>(funcCreator()));')
    L.append('')
    L.append('        public static Task<ThreadSafeForm<T>> GetAsync(Func<CancellationToken, T> funcCreator,')
    L.append('            CancellationToken token = default)')
    L.append('            => Task.FromResult(new ThreadSafeForm<T>(funcCreator(token)));')
    L.append('')
    L.append('        public static Task<ThreadSafeForm<T>> GetCoreAsync(bool blnSync, Func<T> funcCreator,')
    L.append('            CancellationToken token = default)')
    L.append('            => Task.FromResult(new ThreadSafeForm<T>(funcCreator()));')
    L.append('')
    L.append('        public Task<DialogResult> ShowDialogSafeCoreAsync(bool blnSync, object objParent = null,')
    L.append('            CancellationToken token = default)')
    L.append('            => throw new NotSupportedException(')
    L.append('                "Diálogo de seleção ainda não abstraído: " + typeof(T).Name);')
    L.append('')
    L.append('        public DialogResult ShowDialogSafe(object objParent = null,')
    L.append('            CancellationToken token = default)')
    L.append('            => throw new NotSupportedException(')
    L.append('                "Diálogo de seleção ainda não abstraído: " + typeof(T).Name);')
    L.append('')
    L.append('        public Task<DialogResult> ShowDialogSafeAsync(object objParent = null,')
    L.append('            CancellationToken token = default)')
    L.append('            => throw new NotSupportedException(')
    L.append('                "Diálogo de seleção ainda não abstraído: " + typeof(T).Name);')
    L.append('')
    L.append('        public void Dispose() { }')
    L.append('    }')
    L.append('')

    # LoadingBar não é diálogo de escolha: é barra de progresso, e Character.Load a
    # percorre passo a passo. Lançar aqui mataria TODA carga e o spike não mediria nada.
    # Por isso ela é a única cujo stub é no-op de verdade, escrita à mão.
    L.append('    /// <summary>Stub de LoadingBar. Barra de progresso: no-op, nunca lança.</summary>')
    L.append('    public sealed class LoadingBar : DialogStubBase')
    L.append('    {')
    L.append('        public enum ProgressBarTextPatterns')
    L.append('        {')
    L.append('            Saving = 0, Loading = 1, Initializing = 2, Scanning = 3, Printing = 4')
    L.append('        }')
    L.append('')
    L.append('        public string CharacterFile { get; set; } = string.Empty;')
    L.append('')
    L.append('        public Task SetCharacterFileAsync(string value, CancellationToken token = default)')
    L.append('        {')
    L.append('            CharacterFile = value;')
    L.append('            return Task.CompletedTask;')
    L.append('        }')
    L.append('')
    L.append('        public void Reset(int intMaxProgressBarValue = 100) { }')
    L.append('')
    L.append('        public Task ResetAsync(int intMaxProgressBarValue = 100,')
    L.append('            CancellationToken token = default) => Task.CompletedTask;')
    L.append('')
    L.append('        public void PerformStep(string strStepName = "",')
    L.append('            ProgressBarTextPatterns eUseTextPattern = ProgressBarTextPatterns.Loading) { }')
    L.append('')
    L.append('        public Task PerformStepAsync(string strStepName = "",')
    L.append('            ProgressBarTextPatterns eUseTextPattern = ProgressBarTextPatterns.Loading,')
    L.append('            CancellationToken token = default) => Task.CompletedTask;')
    L.append('    }')
    L.append('')

    for tipo in sorted(tipos):
        if tipo == 'LoadingBar':
            continue
        membros = achado.get(tipo, {})
        L.append(f'    /// <summary>Stub de {tipo}. Membros usados pelo domínio: {len(membros)}.</summary>')
        L.append(f'    public sealed class {tipo} : DialogStubBase')
        L.append('    {')
        L.append(f'        public {tipo}() {{ }}')
        for enum_nome, valores in ANINHADOS.get(tipo, {}).items():
            L.append(f'        public enum {enum_nome} {{ {", ".join(valores)} }}')
        L.append(f'        public {tipo}(params object[] args) {{ }}')
        for nome in sorted(membros):
            if nome in ('Opacity', 'DoThreadSafe', 'DoThreadSafeAsync',
                        'DoThreadSafeFunc', 'DoThreadSafeFuncAsync', 'Dispose'):
                continue  # herdado de DialogStubBase
            retorno, ehmetodo = membros[nome]
            lanca = (f'throw new NotSupportedException("{tipo}.{nome} não abstraído");')
            if ehmetodo:
                L.append(f'        public {retorno} {nome}(params object[] args) => {lanca}')
            elif retorno == 'void':
                continue
            else:
                L.append(f'        public {retorno} {nome}')
                L.append('        {')
                L.append(f'            get => {lanca}')
                L.append(f'            set => {lanca}')
                L.append('        }')
        L.append('    }')
        L.append('')

    L.append('}')
    return '\n'.join(L) + '\n'


def main():
    tipos, membros = levantar_uso()
    achado = assinaturas_reais(tipos, membros)

    # Um diálogo usado só via ShowDialogSafe, sem nenhum .MyForm.X, não tem membro a
    # descobrir — mas o tipo precisa existir mesmo assim, senão ThreadSafeForm<X> não
    # compila. A primeira versão omitia esses quatro e o stub não fechava.
    sem_assinatura = sorted(t for t in tipos if t not in achado)
    print(f'diálogos usados pelo domínio: {len(tipos)}')
    print(f'membros distintos chamados via .MyForm.: {len(membros)}')
    print(f'diálogos com assinatura encontrada em Chummer/Forms/: {len(achado)}')
    if sem_assinatura:
        print(f'SEM assinatura encontrada ({len(sem_assinatura)}): {", ".join(sem_assinatura)}')

    os.makedirs(os.path.dirname(SAIDA), exist_ok=True)
    with open(SAIDA, 'w', encoding='utf-8') as f:
        f.write(emitir(achado, tipos))
    total = sum(len(v) for v in achado.values())
    print(f'\ngerado: {os.path.relpath(SAIDA, RAIZ)}  ({len(achado)} classes, {total} membros)')


if __name__ == '__main__':
    sys.exit(main())
