# Decisões técnicas

Registro das decisões de arquitetura e engenharia, com o motivo. Serve para o PO acompanhar
o rumo sem ler código, e para sessões futuras não redecidirem o já decidido.

Decisões técnicas são do programador (ver `CLAUDE.md`). Estão aqui para **transparência**,
não para aprovação — mas o PO pode contestar qualquer uma.

Status: `VIGENTE` · `SUPERSEDIDA`

---

## DEC-001 — Hard fork, sem compatibilidade de merge com o upstream · VIGENTE
**2026-08-11**

As refatorações necessárias ao porte (remover WinForms do domínio, eliminar o caminho
síncrono, trocar `System.Drawing`) são incompatíveis com a base upstream, que continua sendo
um aplicativo WinForms. Tentar manter merge possível travaria justamente as mudanças que
o porte exige.

**Custo aceito:** correções de regras feitas pelo upstream não chegam automaticamente.
Mitigação: os arquivos de `data/` são independentes de código e podem ser atualizados
isoladamente — que é onde mora a maior parte das correções de regra do upstream.

---

## DEC-002 — Avalonia como stack de UI · VIGENTE
**2026-08-11**

Alternativas consideradas: .NET MAUI, Android nativo (Kotlin) consumindo o núcleo.

**Por que Avalonia:**
- XAML com databinding sobre `INotifyPropertyChanged` — que o domínio do Chummer **já
  implementa de forma rigorosa**, com propagação declarativa por grafo de dependências
  (ver `docs/codebase/07-concorrencia.md`). A parte difícil de ligar domínio a UI moderna
  já está pronta.
- Um só código para Android, Linux e Windows. O porte Linux nativo, que o projeto nunca
  teve, sai como subproduto.
- Permite `Chummer.Desktop`, que é o que torna possível depurar o porte sem emulador.

**Por que não MAUI:** não entrega desktop Linux, o que elimina o `Chummer.Desktop` e amarra
o ciclo de desenvolvimento ao emulador.

**Por que não Kotlin:** descartaria as ~350.000 linhas de regras do `Backend/`.

---

## DEC-003 — MVP é leitor/gerenciador de sessão, construído como piloto · VIGENTE
**2026-08-11**

O MVP **não** inclui criação de personagem. Inclui: abrir `.chum5`/`.chum5lz`, exibir o
personagem completo, editar estado de sessão, salvar, e ver a ficha impressa.

**Por que este recorte:** a criação de personagem é ~70% da superfície de UI (só
`CharacterCreate` + `CharacterCareer` + `CharacterShared` somam 66.412 linhas) e ~20% do
valor numa mesa de jogo.

**"Piloto" é literal:** o MVP é a arquitetura final com menos telas, não um protótipo. Nada
nele é descartável. Por isso ele inclui deliberadamente **recarregar arma** — o único item
do escopo que exercita o padrão de solicitação de escolha ao usuário, que é o que destrava
os 45 diálogos de seleção depois.

**Descoberta que forçou este formato:** carregar um `.chum5` instancia 30 tipos distintos do
domínio. Não existe fatia fina do `Backend/` — os ~350.000 LOC do núcleo são custo
obrigatório da primeira entrega. O que dá para fatiar é a UI.

---

## DEC-004 — Teste diferencial contra o build legado como espinha dorsal · VIGENTE
**2026-08-11**

A pergunta de um porte não é "passa nos testes?" mas "**se comporta como o antigo?**".

**Mecanismo:** o build legado (net48) roda em CI no `windows-latest` e gera artefatos
dourados para os 34 personagens de teste. O `Chummer.Core` (net9) gera os mesmos. Um job
compara. Divergência = regressão até prova em contrário (ver PREM-002).

**A peça de maior retorno:** `PrintToXmlTextWriter` produz uma projeção do personagem com
**todos os valores calculados já resolvidos** — pools, limites, Essência, iniciativa,
defesas. Hoje o `Test05_LoadThenPrint` gera isso e só verifica que não explodiu. Transformar
essa saída em artefato dourado dá detecção de regressão de regra com granularidade de
propriedade, reaproveitando máquina que já existe.

Sem isso, o modo de falha típico do porte é silencioso: o arquivo salva e carrega
perfeitamente, e a Agilidade está errada.

**Ruído esperado, a ser triado separadamente de regressão real:**
- **Ordenação de strings** — .NET Framework usa NLS, .NET moderno usa ICU. O Chummer ordena
  listas por nome traduzido em 6 idiomas. Vai gerar diff que não é bug.
- **Formatação numérica** — convenções de cultura mudaram entre as versões, e o Chummer
  formata nuyen, Essência e custos com cultura configurável.

---

## DEC-005 — Convenções de código: húngara no código movido, C# moderno no novo · VIGENTE
**2026-08-11**

Código **movido** do `Backend/` preserva a notação húngara original (`strNome`, `blnAtivo`,
`objPersonagem`). Código **novo** (`Chummer.Core`, `Chummer.UI`) usa C# moderno idiomático.

**Por quê:** misturar refatoração de estilo com refatoração estrutural produz diffs
irrevisáveis. Num porte cujo maior risco é quebrar regras de Shadowrun silenciosamente, a
legibilidade do diff é a principal defesa — e é o que permite ao PO exercer QA na revisão.

Renomeação em massa, se for feita, é tarefa própria e isolada.

---

## DEC-006 — Ordem de ataque do desacoplamento · VIGENTE
**2026-08-11**

Derivada da relação esforço/risco medida em `docs/codebase/13-acoplamento-plataforma.md`:

1. `NativeMethods.cs` — os 80 `DllImport` estão num único arquivo com 8 chamadores. Volume alto, esforço trivial.
2. `GlobalSettings` / Registro — substituição do *backing store* atrás de fachada estável (6.448 acessos exigem preservar a superfície).
3. `System.Drawing` — poucos pontos de contato, já mediados por `IHasMugshots` e `ImageExtensions`.
4. Tipos de UI em assinaturas (`LoadingBar`, `CursorWait`) → abstrações de progresso.
5. `MessageBox` + diálogos `Select*` → abstração de solicitação de interação.
6. `TreeNode` fora do domínio.
7. Caminho síncrono e `DoEvents` — o mais arriscado, porque mexe na semântica de concorrência de um sistema com locks caseiros.

Os itens 1–4 são de baixo risco e não dependem de decisão pendente: **podem começar a
qualquer momento**. Os itens 5–7 dependem do desenho da camada de plataforma.

---

## DEC-007 — Objetivo final inclui criação de personagem completa · VIGENTE
**2026-08-11**

O alvo do projeto é **APK executável e testado, com criação de personagem completa**.

Isso não altera o recorte do MVP (DEC-003) — altera o que ele significa. O MVP-leitor passa
a ser **marco intermediário**, não destino. A criação de personagem (as 4 telas de método de
construção, os 45 diálogos de seleção, `CharacterCreate`) entra no escopo obrigatório, na
Etapa 8 do plano.

Reforça a natureza de piloto do MVP: como o destino inclui as 45 telas de seleção, a
abstração de solicitação de escolha ao usuário construída no MVP deixa de ser preparação
especulativa e passa a ser caminho crítico.

---

## DEC-008 — Projetos do porte ficam em `src/`, isolados do build legado · VIGENTE
**2026-08-11**

Os projetos novos ficam em `src/`, com um `Directory.Build.props` **nessa pasta**, não na
raiz.

**Por quê:** o MSBuild para a busca no primeiro `Directory.Build.props` que encontra subindo
a árvore. Colocado em `src/`, ele se aplica a todo projeto do porte e a nenhum projeto
legado — `Chummer/`, `ChummerHub/` e `Chummer.Tests/` continuam compilando exatamente como
antes. Um `Directory.Build.props` na raiz injetaria propriedades net9 no projeto net48 e
quebraria o build legado, que é justamente o gerador dos artefatos dourados (DEC-004).

Supersede o layout de pastas na raiz descrito na primeira versão do `CLAUDE.md`.

---

## DEC-009 — A regra "núcleo sem UI" é imposta pelo compilador · VIGENTE
**2026-08-11**

`Chummer.Core` tem alvo `net9.0`, **não** `net9.0-windows`. Consequência: `System.Windows.Forms`
e `System.Drawing.Common` não existem no projeto, e qualquer código acoplado a WinForms
arrastado para dentro dele **falha na compilação**.

Verificado na prática nesta iteração: uma sonda com `using System.Windows.Forms` produziu
`error CS0234` e derrubou o build, como desejado. A sonda foi removida em seguida.

**Por que importa:** durante a extração dos ~350.000 LOC do `Backend/`, a tentação de
"resolver depois" um acoplamento é constante. Com o alvo sem sufixo `-windows`, não há
"depois" — o compilador recusa. A regra deixa de depender de disciplina.

---

## DEC-010 — `global.json` passa a `rollForward: latestMajor` · SUPERSEDIDA por DEC-012
**2026-08-11**

~~`latestMajor` mantém 8.0.401 como piso e libera o SDK 9 para o porte.~~

**Estava errada, e quebrou o CI.** `latestMajor` não significa "aceite 8 ou mais" — significa
**"use o major mais novo disponível na máquina"**. No runner do GitHub, que tem o SDK 10
pré-instalado, o build legado passou a ser compilado com SDK 10 mesmo com um
`setup-dotnet` fixando `8.0.x`: o `rollForward` anula o pin. O resultado foi `MSB3823`
("Non-string resources require GenerateResourceUsePreserializedResources"), porque o SDK 10
não processa `.resx` com recursos binários de um projeto net48.

Substituída por DEC-012.

---

## DEC-012 — Dois `global.json`, um por mundo · VIGENTE
**2026-08-11**

- **Raiz:** `8.0.401` / `latestFeature` — exatamente como o upstream. O build legado, que
  gera os artefatos dourados, fica intocado e reproduzível.
- **`src/global.json`:** `9.0.100` / `latestFeature` — o mundo do porte.

O `dotnet` resolve o `global.json` **a partir do diretório de trabalho**, não do caminho do
projeto. Por isso `Chummer.Port.sln` foi movida para dentro de `src/`, e tanto
`scripts/setup-dev.sh` quanto `port-build.yml` compilam com `src/` como diretório de
trabalho. Rodar da raiz selecionaria o SDK errado.

Verificado: na raiz o `dotnet` exige 8.0.4xx; em `src/` resolve 9.0.316.

**A lição que fica:** o `global.json` da raiz é território do build legado. O porte não
mexe nele. Qualquer necessidade de SDK novo se resolve dentro de `src/`.

---

## DEC-011 — CI do porte em Linux, separado do build legado · VIGENTE
**2026-08-11**

Novo workflow `.github/workflows/port-build.yml`, em `ubuntu-latest`, compilando
`Chummer.Port.sln`.

Roda em push para `master` e `claude/**` e em pull request, com filtro de caminho — não
dispara para mudanças que só tocam documentação. O build legado continua exclusivamente no
Windows, sem alteração.

**Efeito colateral relevante:** antes desta iteração, **nenhum workflow do repositório
disparava em pull request** (CodeQL só em push para master com filtro em `.sln`/`.csproj`,
nightly por cron, notificação por release). O PR #1 tinha zero verificações. A partir de
agora há CI de verdade nos PRs do porte.

---

## DEC-013 — Uma fonte da verdade, duas compilações · VIGENTE
**2026-08-11**

Durante a Etapa 2, um arquivo migrado tem **um único arquivo-fonte** — em
`src/Chummer.Core/` — e **duas compilações**: `net9.0` no `Chummer.Core` e `net48` no
projeto legado, que o inclui por link.

```xml
<Compile Include="..\src\Chummer.Core\**\*.cs"
         Exclude="..\src\Chummer.Core\bin\**\*.cs;..\src\Chummer.Core\obj\**\*.cs"
         LinkBase="CoreLinked" />
```

**Por quê:** o `Chummer.csproj` usa globbing, então mover um arquivo para fora de
`Chummer/` o remove do build legado. E o build legado **não pode quebrar** — é ele que gera
os artefatos dourados do teste diferencial (DEC-004). Sem o link, migrar código e manter o
oráculo de teste seriam objetivos incompatíveis.

**A regra que isto cria:** um arquivo só migra quando compila **nos dois alvos**. O alvo
`net9.0` sem sufixo `-windows` do `Chummer.Core` (DEC-009) garante que nada acoplado a
WinForms atravesse a fronteira. É o mesmo mecanismo de DEC-009 aplicado à migração: o
compilador impede o atalho.

**Alternativa descartada:** copiar os arquivos e manter as duas cópias em sincronia. Numa
migração de ~350.000 linhas, divergência silenciosa entre as cópias é questão de tempo, e
seria descoberta pelo teste diferencial acusando uma regressão que não existe.

**Ciclo de vida:** este `ItemGroup` encolhe conforme o legado é desmontado, e desaparece
quando `Chummer/` for removido.

**Detalhe que quase passou:** o glob precisa excluir `bin/` e `obj/`. Sem isso ele arrasta o
`AssemblyInfo` gerado do `Chummer.Core` e o build legado quebra com atributos de assembly
duplicados.

---

## DEC-014 — Ordem de migração: os limpos primeiro · VIGENTE
**2026-08-11**

O censo (DEC-004 / `docs/codebase/14-censo-de-erros.md`) revelou que **171 dos 245 arquivos
de `Backend/` já compilam limpos sob net9.0** — 70% da base, sem uma linha de alteração.

A migração começa por eles, não pelos problemáticos. Ganhos:

- o `Chummer.Core` deixa de ser vazio e passa a ter domínio real, cedo;
- cada lote é verificável isoladamente pelo CI;
- os 74 arquivos com erro passam a ser atacados com o restante já do lado de cá, o que
  reduz o acoplamento residual de cada um.

**Primeiro lote executado:** `Backend/Enums/` — 11 de 11 arquivos limpos, sem dependências,
sem `using` algum. Escolhido deliberadamente pelo tamanho: serve para **provar o mecanismo
de DEC-013 pelo CI** antes de mover os outros 160 arquivos.

**Limite de verificação:** o build `net48` não roda em Linux. O `Chummer.Core` foi
verificado localmente; o efeito do link no projeto legado só pode ser confirmado pelo CI
Windows. Por isso o primeiro lote é pequeno.

