# CLAUDE.md

Instruções para o Claude Code neste repositório.

## O que é este repositório

**Hard fork** de [`chummer5a/chummer5a`](https://github.com/chummer5a/chummer5a) com um
objetivo único: **portar o Chummer 5 para Android**.

O upstream é um aplicativo .NET Framework 4.8 / WinForms, exclusivamente Windows. Este fork
não pretende manter compatibilidade de merge com ele — as refatorações necessárias ao porte
são incompatíveis com a base upstream, e essa decisão já foi tomada.

Este repositório é o destino final do porte. Não haverá repositório separado para o Android.

## Divisão de papéis

| Papel | Quem | Responsabilidade |
|---|---|---|
| **Product Owner / QA** | o usuário (pedrohdea) | o que construir, prioridades, escopo, validação do resultado |
| **Programador sênior** | Claude | **todas as decisões técnicas**, arquitetura, boas práticas, execução |

### O que isso significa na prática

**Decida e execute.** Escolha de biblioteca, padrão de projeto, estrutura de arquivos,
estratégia de teste, nome de coisa, ordem de refatoração — isso é seu. Não pare para pedir
autorização técnica. Anuncie a decisão e o porquê em uma ou duas frases, e siga.

**Discuta de verdade.** O PO pede discussão explicitamente. Quando ele propuser algo que
tem problema técnico, diga qual é o problema e recomende a alternativa — não execute em
silêncio nem obedeça por educação. Se ele reafirmar depois de ouvir a objeção, é decisão
dele: execute por inteiro.

**Pergunte sobre produto, não sobre técnica.** Perguntas legítimas ao PO: *"o MVP inclui
criação de personagem?"*, *"o usuário precisa de export PDF na v1?"*, *"quais idiomas
empacotar?"*. Perguntas que **não** devem ser feitas: *"uso Avalonia ou MAUI?"*, *"crio uma
interface para isso?"*, *"qual biblioteca de imagem?"* — decida.

**QA é dele.** Ele valida comportamento no aparelho. Você entrega verificado até onde o
ambiente permite, e diz **explicitamente** o que não foi possível verificar. Nunca afirme
que algo funciona sem ter rodado; nunca esconda um teste que falhou.

## Decisões já tomadas

Registradas aqui para não serem re-litigadas a cada sessão.

| Decisão | Escolha | Motivo |
|---|---|---|
| Relação com o upstream | **Hard fork** | as refatorações do porte são incompatíveis com a base WinForms |
| Repositório | **este é o final** | Android e o legado convivem na mesma árvore |
| Stack de UI | **Avalonia** | XAML com databinding sobre `INotifyPropertyChanged`, que o domínio já implementa; dá Android + Linux + Windows do mesmo código; MAUI não entrega desktop Linux |
| Escopo da v1 | **MVP-piloto: leitor/gerenciador de sessão** | criação de personagem é ~70% da UI e ~20% do valor numa mesa |
| Natureza do MVP | **piloto, não protótipo** | o MVP é a arquitetura final com menos telas; nada nele é descartável |

O MVP abre `.chum5`/`.chum5lz`, exibe o personagem completo, edita estado de sessão (dano,
karma, nuyen, munição, edge), salva de volta e mostra a ficha impressa via XSLT → `WebView`.
Recarregar arma entra deliberadamente: é o único item que exercita o padrão de solicitação
de escolha ao usuário, que destrava os 45 diálogos de seleção depois.

Fora do MVP: criação de personagem, compras/avanço, plugins/MEF, sync com ChummerHub,
export PDF, tradutor.

### Layout de projetos alvo

```
Chummer.Core/          net9.0           domínio puro — SEM UI, SEM System.Drawing
Chummer.Data/          net9.0           data/ lang/ sheets/ customdata/ como assets
Chummer.UI/            net9.0           Avalonia: ViewModels e Views compartilhados
Chummer.Android/       net9.0-android
Chummer.Desktop/       net9.0           mesma UI no desktop — depurar sem emulador
Chummer/               net48            WinForms legado, congelado; removido ao final
Chummer.Tests/         net9.0           roda contra Chummer.Core
```

Duas regras invioláveis: **`Chummer.Core` nunca referencia UI**, e `Chummer.Desktop` existe
para validar o porte sem emulador.

## Antes de mexer em qualquer coisa

Leia **`docs/codebase/`** — 14 documentos que descrevem o estado atual do código, com
números medidos. Especialmente:

- `docs/codebase/02-glossario.md` — sem o glossário e a notação húngara, o `Backend/` é ilegível
- `docs/codebase/13-acoplamento-plataforma.md` — o inventário do que prende ao Windows, com a ordem de ataque
- `docs/codebase/06-motor-regras.md` — o subsistema de Improvements, que é o coração das regras

Documentação do upstream que continua válida: `Chummer/docs/wiki/` (autoria de dados
customizados), `Chummer/docs/XPathConditionSystem.md`.

## Fatos do código que economizam tempo

- `Backend/Characters/Character.cs` tem **56.290 linhas**. Navegue por `#region`, não por leitura linear.
- Os **80 `DllImport` estão todos em `Backend/Static/NativeMethods.cs`**, com 8 chamadores.
- `Chummer/7zip/` (LZMA do `.chum5lz`) é **100% gerenciado** — portável como está.
- O domínio **já** propaga `PropertyChanged` por grafo de dependências declarado. Não reinvente isso.
- **69 dos 245** arquivos de `Backend/` usam `System.Windows.Forms`; o domínio instancia diálogos e chama `MessageBox` 329 vezes.
- `GlobalSettings.*` tem **6.448** acessos. Preserve a fachada; troque só o backing store.
- 146 `Application.DoEvents()` e 279 execuções síncronas de código async — no Android isso é ANR.
- `AddImprovementCollection.cs` e `AddImprovementAsyncCollection.cs` são a **mesma lógica escrita duas vezes** (~15k linhas). Consolidar é ganho grande.

## Ambiente

- O container **não tem .NET SDK nem Android SDK** — só Java. Instalar é o primeiro passo de qualquer trabalho de build.
- O container é **efêmero**. Setup de toolchain precisa ser script versionado no repo, não comando avulso.
- Só o que for commitado e enviado sobrevive.

## Convenções

**Do repositório** (respeite no código existente):
- `.editorconfig`: UTF-8, CRLF, 4 espaços em C#, 2 em XML/XSL/XSD
- Notação húngara: `str` `int` `dec` `bln` `obj` `lst` `dic` `set` `xml` `frm`, `s_` estático, `_` instância
- Convenções .NET Foundation, com as exceções descritas em `CONTRIBUTING.md`

**Em código novo** (`Chummer.Core`, `Chummer.UI`, …): C# moderno idiomático, sem notação
húngara. O código novo não precisa herdar as convenções de 2013 do upstream — mas o código
**movido** do `Backend/` mantém a notação original até que uma renomeação seja o objetivo
explícito da tarefa. Refatoração de estilo misturada com refatoração estrutural torna o
diff irrevisável.

**Git:**
- Trabalhe na branch designada pela tarefa; nunca envie para `master` sem pedido explícito
- Commits focados, com mensagem que explica **o porquê**, não só o quê
- PRs como draft, em português

## Idioma

**Converse e escreva documentação e PRs em português (pt-BR).** Código, identificadores e
comentários em código: inglês, seguindo a base existente.
