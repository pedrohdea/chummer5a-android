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

**Nunca fique bloqueado.** Mesmo as perguntas de produto não param o trabalho — ver o
protocolo da pasta do PO, logo abaixo. O PO trabalha em paralelo e pode demorar a
responder; o desenvolvimento segue sob premissa registrada.

**QA é dele.** Ele valida comportamento no aparelho. Você entrega verificado até onde o
ambiente permite, e diz **explicitamente** o que não foi possível verificar. Nunca afirme
que algo funciona sem ter rodado; nunca esconda um teste que falhou.

## Protocolo da pasta do PO — leia antes de qualquer sessão de trabalho

**`docs/po/`** é o mecanismo que permite PO e programador trabalharem em paralelo. Quatro
arquivos: `pendencias.md` (perguntas abertas ao PO), `premissas.md` (o que foi assumido para
não travar), `qa-roteiro.md` (o que precisa de teste humano), `decisoes.md` (decisões
técnicas e o porquê).

**A regra:** quando aparecer uma pergunta cuja resposta é do PO —

1. registre a pendência em `pendencias.md`;
2. **escolha a resposta mais provável**, registre em `premissas.md` com o que muda se estiver
   errada e o custo de reverter (🟢 baixo · 🟡 médio · 🔴 alto);
3. **siga trabalhando** sob essa premissa.

Única exceção: premissa 🔴 (retrabalho de semanas) — não construa por cima dela sem resposta;
trabalhe em outra frente enquanto isso.

**Mantenha os arquivos vivos.** Toda sessão: leia `pendencias.md` e `premissas.md` antes de
começar; ao terminar, registre pendências novas, premissas novas, decisões tomadas e itens
de QA que a entrega criou. Itens resolvidos mudam de status e vão para o fim do arquivo —
nunca são apagados.

## Decisões já tomadas

Resumo. O registro completo, com motivos e alternativas descartadas, está em
`docs/po/decisoes.md`.

| Decisão | Escolha | Motivo |
|---|---|---|
| Relação com o upstream | **Hard fork** | as refatorações do porte são incompatíveis com a base WinForms |
| Repositório | **este é o final** | Android e o legado convivem na mesma árvore |
| Stack de UI | **Avalonia** | XAML com databinding sobre `INotifyPropertyChanged`, que o domínio já implementa; dá Android + Linux + Windows do mesmo código; MAUI não entrega desktop Linux |
| Escopo da v1 | **MVP-piloto: leitor/gerenciador de sessão** | criação de personagem é ~70% da UI e ~20% do valor numa mesa |
| Natureza do MVP | **piloto, não protótipo** | o MVP é a arquitetura final com menos telas; nada nele é descartável |
| **Objetivo final** | **APK executável e testado, com criação de personagem completa** | o MVP é marco intermediário, não destino (DEC-007) |

O MVP abre `.chum5`/`.chum5lz`, exibe o personagem completo, edita estado de sessão (dano,
karma, nuyen, munição, edge, **magias sustentadas**, **rolagem de dados** — PREM-013), salva
de volta e mostra a ficha impressa via XSLT → `WebView`. Recarregar arma entra
deliberadamente: é o único item que exercita o padrão de solicitação de escolha ao usuário,
que destrava os 45 diálogos de seleção depois.

Fora do MVP: criação de personagem, compras/avanço, plugins/MEF, sync com ChummerHub,
export PDF, tradutor.

**Criação de personagem cobre apenas o método Prioridade** (PREM-012). Soma-para-Dez, Karma
e Life Modules estão fora do escopo do projeto — mas o `Character` continua **abrindo**
personagens criados por qualquer método, o que PREM-010 exige.

### Layout de projetos alvo

```
src/Directory.Build.props            propriedades comuns — fica em src/, NUNCA na raiz
src/Chummer.Core/       net9.0       domínio puro — SEM UI, SEM System.Drawing
src/Chummer.Data/       net9.0       data/ lang/ sheets/ customdata/ como assets
src/Chummer.UI/         net9.0       Avalonia: ViewModels e Views compartilhados
src/Chummer.Android/    net9.0-android
src/Chummer.Desktop/    net9.0       mesma UI no desktop — depurar sem emulador
Chummer/                net48        WinForms legado, congelado; removido ao final
Chummer.Tests/          net48        legado; gera os artefatos dourados no CI Windows
```

Solução do porte: **`Chummer.Port.sln`** (só projetos portáveis, compila em Linux). A
`Chummer.sln` original segue intocada e exige Windows.

Duas regras invioláveis: **`Chummer.Core` nunca referencia UI**, e `Chummer.Desktop` existe
para validar o porte sem emulador.

`Directory.Build.props` **em `src/` e não na raiz** é deliberado (DEC-008): na raiz, ele
injetaria propriedades net9 no projeto legado net48 e quebraria o build que gera os
artefatos dourados.

A primeira regra é imposta pelo compilador, não por disciplina (DEC-009): `Chummer.Core`
tem alvo `net9.0` sem sufixo `-windows`, então `System.Windows.Forms` não existe ali.
Código acoplado a WinForms arrastado para dentro **falha na compilação**. Não adicione
`<UseWindowsForms>`, `<UseWPF>` nem alvo `-windows` a esse projeto.

### Ambiente de build

```bash
./scripts/setup-dev.sh              # SDK .NET + verificação
./scripts/setup-dev.sh --android    # + workload Android (para gerar APK)
./scripts/censo-erros.sh            # mede: relatório completo (barra de progresso da Etapa 2)
./scripts/censo-erros.sh --rapido   # guia: primeiros erros, ~1 s (laço interno)
./scripts/censo-erros.sh --rapido Weapon   # idem, filtrado por arquivo
./scripts/verificar-ui.sh           # valida o que foi extraído para Controls/
./scripts/verificar-legado.sh       # compila o app legado INTEIRO em Linux (~30 s)
```

**Rode `verificar-legado.sh` antes de todo commit que mexa em código legado** (DEC-036). É
a única ferramenta local que enxerga erro dentro de corpo de método: ela compila sob
`net9.0-windows` com WinForms de verdade, então os erros de declaração chegam a zero e o
Roslyn vincula os corpos. O censo **não** faz isso — ele mede acoplamento de declaração, e
o compilador não vincula corpos enquanto houver erro de declaração (DEC-032). Foi essa
cegueira que deixou quatro rodadas de CI vermelhas passarem por ferramentas locais verdes.

**Dois `global.json`, e a distinção é crítica** (DEC-012): a raiz fixa o SDK 8 do build
legado e **o porte não mexe nela**; `src/global.json` fixa o SDK 9. O `dotnet` resolve o
`global.json` pelo **diretório de trabalho**, então compile sempre de dentro de `src/` —
da raiz, o SDK errado é selecionado e o build legado quebra com `MSB3823`.

O container é efêmero: **toda dependência de toolchain vai nesse script**, nunca num
comando avulso. CI do porte: `.github/workflows/port-build.yml` (Linux).

## O plano

**`docs/po/plano.md`** — as 10 etapas, com estado. Toda etapa termina com a subetapa
**reavaliar o plano na totalidade** (DEC-017), e o resultado de cada reavaliação fica no
histórico do próprio arquivo. Não pule: das quatro reavaliações feitas até agora, três
mudaram o rumo.

## Antes de mexer em qualquer coisa

Leia **`docs/codebase/`** — 14 documentos que descrevem o estado atual do código, com
números medidos. Especialmente:

- `docs/codebase/02-glossario.md` — sem o glossário e a notação húngara, o `Backend/` é ilegível
- `docs/codebase/13-acoplamento-plataforma.md` — o inventário do que prende ao Windows, com a ordem de ataque
- `docs/codebase/06-motor-regras.md` — o subsistema de Improvements, que é o coração das regras
- `docs/codebase/14-censo-de-erros.md` — a medição: 733 erros, dos quais só 451 são domínio

Documentação do upstream que continua válida: `Chummer/docs/wiki/` (autoria de dados
customizados), `Chummer/docs/XPathConditionSystem.md`.

## Estratégia de teste

Espinha dorsal: **teste diferencial contra o build legado** (DEC-004). O build net48 roda em
CI no `windows-latest` e gera artefatos dourados para os 34 personagens de
`Chummer.Tests/TestFiles/`; o `Chummer.Core` gera os mesmos e um job compara. Divergência é
regressão até prova em contrário — o porte preserva até os defeitos atuais (PREM-002).

A suíte existente já é uma boa base de caracterização: carrega todo o XML, carrega os 34
personagens, faz round-trip de save com diff XMLUnit (com filtros para as não-determinâncias
conhecidas) e imprime em todos os idiomas.

A peça de maior retorno a construir: `PrintToXmlTextWriter` produz uma projeção com **todos
os valores de regra já calculados**. Hoje o `Test05` gera e descarta. Transformar isso em
artefato dourado dá detecção de regressão de regra por propriedade, quase de graça.

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
