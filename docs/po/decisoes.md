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

## DEC-014 — Ordem de migração: os limpos primeiro · SUPERSEDIDA por DEC-016
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

**Derrubada em 2026-08-11, por medição.** A premissa central — de que os 171 arquivos
limpos poderiam migrar em lotes — é falsa. Compilados isoladamente, produzem **368 erros**:
referenciam tipos que moram nos 74 arquivos sujos.

O erro de raciocínio foi confundir duas coisas diferentes: "compila limpo **junto com todo
o `Backend/`**" (o que o censo mediu) com "compila limpo **sozinho**" (o que a migração
exige). O censo nunca mediu fechamento por dependência.

O primeiro lote (`Backend/Enums`) sobreviveu porque enums sem nenhum `using` são
genuinamente folhas. Foi sorte de escolha, não validação da estratégia.

Substituída por DEC-016.

---

## DEC-015 — O build legado funciona neste fork · VIGENTE (risco encerrado)
**2026-08-11**

Registro de um risco que estava aberto e foi resolvido por evidência, não por suposição.

**O risco:** toda a estratégia de teste (DEC-004) depende de o build legado net48 rodar em
CI Windows para gerar os artefatos dourados. Ao investigar o CodeQL, descobriu-se que o
`nightly-build.yml` usava `dotnet build` — que falha neste projeto — e que **nunca havia
executado neste fork**. Ou seja, não havia nenhuma evidência de que a `Chummer.sln`
compilasse aqui.

**A evidência que encerrou o risco:** o job `Analyze (csharp)` do CodeQL compila a
`Chummer.sln` inteira em `windows-latest`. Sequência de execuções:

| Commit | Resultado | Causa |
|---|---|---|
| `756145f1` | falha | caminho fixo do MSBuild do Visual Studio (herdado do upstream) |
| `dd2dddf3` | falha | troca para `dotnet build` + SDK 10 selecionado por `latestMajor` |
| `36b3889c` | falha | `dotnet build` com SDK 8.0.423 — **provou que a causa não era o SDK** |
| `0606f716` | **sucesso** | `microsoft/setup-msbuild` |
| `9723622f`, `0ea3c56d` | **sucesso** | — |

Três execuções verdes consecutivas a partir do commit que introduziu o `setup-msbuild`.
**A `Chummer.sln` compila neste fork.** Os artefatos dourados são viáveis e DEC-004 está de
pé.

**A causa raiz, que vale além do CodeQL:** o `Chummer.csproj` tem alvo net48 e 108 arquivos
`.resx` com recursos binários. A tarefa `GenerateResource` que vem no dotnet CLI não os
serializa sem `GenerateResourceUsePreserializedResources` e falha com `MSB3823`. O MSBuild
do Visual Studio os processa nativamente.

**Consequência aplicada:** o `nightly-build.yml` recebeu o mesmo tratamento
(`setup-msbuild` + `msbuild` no lugar de `dotnet build`), porque tinha exatamente o mesmo
defeito latente.

**Consequência para o futuro:** o job que gerar os artefatos dourados **precisa usar MSBuild
do Visual Studio**, não `dotnet build`. Fica registrado aqui para não ser redescoberto.

---

## DEC-016 — Consertar em lugar, mover uma vez · VIGENTE
**2026-08-11**

A extração do núcleo passa a ter duas fases separadas no tempo:

**Fase 1 — consertar em lugar.** Os 74 arquivos com erro são corrigidos **dentro de
`Chummer/Backend/`**, onde estão hoje. O critério de progresso é o próprio censo:
`./scripts/censo-erros.sh` recompila todo o `Backend/` sob net9.0 e devolve o número de
erros. A fase termina quando o número chega a zero.

**Fase 2 — mover uma vez.** Com todo o `Backend/` compilando sob net9.0, o movimento para
`src/Chummer.Core/` vira uma operação mecânica única, sem risco de conjunto aberto.

**Por que isto e não a migração em lotes:** o domínio é um grafo fortemente conectado —
`Character` sozinho instancia 30 tipos. Não existe sequência de lotes fechados por
dependência que não seja, na prática, "quase tudo de uma vez". Tentar migrar em ondas
produziria centenas de erros artificiais a cada onda, indistinguíveis dos erros reais.

**O que se ganha:**
- cada conserto é verificável isoladamente, pelo delta do censo;
- o build legado continua verde o tempo todo, porque nada se move;
- o número de erros vira uma **barra de progresso honesta** da Etapa 2;
- o movimento final é mecânico, e um diff de "arquivo movido" é trivial de revisar.

**O que se perde:** o `Chummer.Core` fica quase vazio por mais tempo. É custo cosmético — o
progresso real é medido pelo censo, não pelo tamanho da pasta.

**O que fica de pé de DEC-014:** a ordem de ataque dos 74 arquivos, que continua valendo —
interfaces primeiro pelo efeito cascata, depois a fronteira de imagem, depois equipamento,
e `Character.cs` por último.

---

## DEC-017 — Toda etapa termina com reavaliação do plano · VIGENTE
**2026-08-11**

Pedido do PO, e o projeto já provou que é necessário. A última subetapa de cada etapa em
`docs/po/plano.md` é **reavaliar o plano na totalidade**: conferir se a medição contradiz o
planejado, se alguma etapa seguinte mudou de tamanho, se alguma premissa deve cair por
evidência, se a ordem ainda é a certa e se o maior risco continua sendo o mesmo.

**Por que não é formalidade:** em menos de um dia de trabalho, três reavaliações mudaram o
rumo — o achado dos 30 tipos na Etapa 0 fixou o recorte do MVP, o censo da Etapa 1 reduziu
o trabalho estimado da Etapa 2 em 38%, e a medição de fechamento por dependência derrubou
DEC-014 antes de ela custar caro.

O resultado de cada reavaliação fica no histórico de `plano.md`.

---

## DEC-018 — Interface de domínio e extensões de UI vivem em arquivos separados · VIGENTE
**2026-08-11**

Vários arquivos de `Backend/Interfaces/` contêm **duas coisas** no mesmo arquivo: uma
interface pequena, que é domínio, e uma classe estática grande de métodos de extensão que
manipulam `TreeView`, `ContextMenuStrip` e `Control`.

O caso extremo é `IHasInternalId.cs`: **1.430 linhas**, das quais a interface ocupa 5.

```csharp
public interface IHasInternalId          // 5 linhas — domínio
{
    string InternalId { get; }
}

public static class InternalId           // 1.400 linhas — reconstrução de nós de TreeView
{
    public static async Task RefreshChildrenGears(this IHasInternalId objParent,
        TreeView treGear, ContextMenuStrip cmsGear, ...)
```

A correção não é portar: é **separar**. A interface fica em `Backend/`, as extensões vão
para `Chummer/Controls/Extensions/`, onde o projeto legado continua as compilando e o
`Chummer.Core` nunca as verá.

**Técnica usada quando a própria interface está dividida** (`IHasSource`): declarar
`partial interface`, com a metade de domínio em `Backend/` e a metade que depende de
WinForms em `Controls/Extensions/`. No projeto legado as duas voltam a ser uma interface só,
então **nenhum implementador ou chamador precisa mudar** — o que preserva o build que gera
os artefatos dourados.

**Resultado medido:** 733 → 671 erros no censo, exatamente os 62 erros das interfaces.
Confirma a previsão de DEC-014 de que interfaces são o alvo de melhor relação custo-benefício.

**Não resolvido nesta leva:** `IHasMugshots.cs` (5 erros), cujo problema é
`System.Drawing.Image` **dentro da própria interface**. Depende da abstração de imagem, que
é o item 3 de DEC-006.

---

## DEC-019 — A ferramenta de medição precisa falhar alto · VIGENTE
**2026-08-11**

O `censo-erros.sh` teve dois defeitos descobertos ao ser usado para medir progresso real, e
ambos produziam **medição errada em silêncio**:

1. **Falso zero.** O script rodava `dotnet` a partir da raiz do repositório, cujo
   `global.json` fixa o SDK 8 (DEC-012). O build nem começava, nenhum erro CS aparecia no
   log, e o censo reportava **"0 erros"** — indistinguível de "todo o Backend compila".
   Corrigido rodando de dentro do diretório de sondagem, que fica fora da árvore do
   repositório e portanto sem `global.json`.

2. **Conjunto incompleto.** A sondagem incluía apenas `Backend/`, então o código já migrado
   para o `Chummer.Core` sumia da compilação e gerava erros fantasma nos tipos que haviam
   saído — medindo regressão onde houve progresso. Corrigido incluindo
   `src/Chummer.Core/**/*.cs` na sondagem.

**A regra que fica:** o censo é a barra de progresso da Etapa 2, e uma barra de progresso
que confunde "não mediu" com "está tudo certo" é pior que nenhuma. O script agora **aborta
com erro** se não encontrar erros CS **e** o build também não tiver tido sucesso.

Vale como aviso geral: nesta sessão, o primeiro número que a ferramenta de medição produziu
esteve errado **três vezes** — 6.196 por dupla contagem e ruído de `Annotations.cs`, 0 por
SDK errado, e 732 por conjunto incompleto. Desconfie do primeiro número.

---

## DEC-020 — `Backend/` guarda domínio; infraestrutura de UI sai · VIGENTE
**2026-08-11**

Treze arquivos moravam em `Chummer/Backend/` sem serem domínio. Movidos para
`Chummer/Controls/Infrastructure/`:

| Arquivo | Erros que causava | O que é |
|---|---|---|
| `WinFormsExtensions.cs` | 99 | a ponte `DoThreadSafe` — conceito que não existe no destino |
| `CenterableMessageBox.cs` | 40 | caixa de mensagem |
| `ImageExtensions.cs` | 33 | thumbnails, encoders, `PixelFormat` — apresentação, não domínio |
| `ColorManager.cs` | 22 | tema claro/escuro, com polling do Registro a cada 5 s |
| `DispatcherExtensions.cs` | 17 | WPF |
| `ThreadSafeForm.cs` | 16 | wrapper de `Form` |
| `TooltipFactory.cs` | 14 | tooltips |
| `ListViewItemWithValue.cs` | 14 | `ListViewItem` do WinForms |
| `CursorWait.cs` | 9 | cursor de espera |
| `HoverDisplayCoordinator.cs` | 7 | interação de mouse |
| `NegatableBinding.cs` | 6 | binding do WinForms |
| `FlagImageGetter.cs` | 2 | bandeiras de idioma |
| `DataGridViewTextBoxColumnTranslated.cs` | 2 | coluna de grid |

Mais `Backend/Helpers/Application Insights/` → `Chummer/Telemetry/ApplicationInsights/`,
por PREM-005 (telemetria desligada, e portanto fora do núcleo).

**Resultado medido:** 671 → **367 erros**. Metade dos erros restantes eliminada sem portar
uma linha — apenas devolvendo cada arquivo ao lugar a que pertence. Nada foi apagado: o
projeto legado compila os mesmos arquivos, só que a partir de outra pasta.

**A regra que fica:** `Backend/` é domínio. Se um arquivo referencia `Control`, `TreeNode`,
`Form`, `Image` ou `Color` **como sua razão de existir** — e não como detalhe de uma
assinatura isolada — ele não é domínio, e a resposta certa é movê-lo, não portá-lo.

---

## DEC-021 — Retratos: o domínio guarda bytes, não `Image` · VIGENTE (a implementar)
**2026-08-11**

Hoje `Character.Mugshots` é `ThreadSafeList<Image>`: o carregamento decodifica base64 →
`Image` e o salvamento re-codifica `Image` → base64.

**Decisão:** o domínio passa a guardar a **string base64 / bytes** exatamente como estão no
arquivo, e nunca decodifica. Decodificar para exibir é responsabilidade da apresentação.

**Três ganhos, e o terceiro foi surpresa:**

1. Remove `System.Drawing` do domínio no ponto mais profundo em que ele entra
   (`IHasMugshots`, implementado por `Character`, `Contact` e `Spirit`).
2. **Carregamento mais rápido** — abrir um personagem deixa de decodificar imagens. Importa
   no Android, onde PREM-003 está 🟡 e um personagem de teste chega a 5,7 MB.
3. **Elimina uma não-determinância conhecida do teste dourado.** O
   `Test04_LoadThenSaveIsDeterministic` filtra explicitamente os nós `mugshot`, com o
   comentário *"image loading and unloading is not going to be deterministic due to
   compression algorithms"*. Guardando bytes e nunca recodificando, o round-trip vira
   determinístico e **o filtro pode ser removido**, fortalecendo o oráculo.

**Mudança de comportamento assumida:** a compressão passa a acontecer na **importação** do
retrato, não a cada salvamento. Hoje, mudar a configuração de qualidade de imagem
recomprime retratos antigos no próximo save. Isso não é regra de Shadowrun, então PREM-002
não se aplica — e o novo comportamento é mais fiel, porque não degrada a imagem a cada
ciclo.

Implementação pendente: toca `IHasMugshots`, `Character`, `Contact`, `Spirit`,
`CharacterCache` e `GlobalSettings`.

---

## DEC-022 — O censo tem dois modos, porque são dois usos · VIGENTE
**2026-08-11**

Proposta do PO: *"faça fail-fast no caso de teste para ganhar velocidade — normalmente o
quadro parcial do erro é suficiente para gerar correções."*

**Avaliação: diagnóstico certo, remédio mirando o lugar errado.** Medido antes de opinar:

| Operação | Tempo |
|---|---|
| Censo completo, como estava | **27 s** |
| Recompilar o mesmo projeto, sem restore | **4 s** |

**23 dos 27 segundos não eram compilação.** Eram o `rm -rf`, a recriação do `.csproj` e o
restore do NuGet que o script fazia a cada execução. Fail-fast atacaria uma fração dos 4
segundos restantes.

Some-se a isso um fato da plataforma: **o compilador C# não tem `/maxerrors`**. O limite de
100 erros que aparece na documentação da Microsoft é do Visual Basic. Fail-fast no nível do
`csc` não é sequer possível — só dá para truncar a saída depois, o que é apresentação e não
desempenho.

**A parte da afirmação que está certa, e importa:** ler 367 erros para achar os 3 que
interessam é desperdício **cognitivo**, mesmo que a compilação leve 1 segundo. O gargalo
real do laço interno é volume de saída, não tempo de máquina.

### O que foi feito

**Sondagem persistente.** O `.csproj` só é reescrito quando muda de conteúdo, o que permite
ao `dotnet` reusar `obj/` e pular o restore.

**Dois modos, com propósitos distintos:**

```bash
./scripts/censo-erros.sh                  # mede: relatório completo e total
./scripts/censo-erros.sh --rapido         # guia: os primeiros erros
./scripts/censo-erros.sh --rapido Weapon  # guia: filtrado por arquivo
```

O modo completo é a **barra de progresso da Etapa 2** e precisa do número inteiro — aplicar
fail-fast nele destruiria exatamente aquilo para o que ele existe. O modo rápido serve ao
laço de correção, onde o total é irrelevante.

Conflacionar os dois era erro meu de desenho, e foi o que a proposta do PO expôs.

### Resultado medido

| Cenário | Antes | Depois |
|---|---|---|
| Partida fria | 27 s | 5 s |
| Execução seguinte | 27 s | **1 s** |
| Filtrado por arquivo | não existia | 2 s |

**27× mais rápido no laço interno**, sem perder a medição completa.

### A lição geral

A proposta partia de uma intuição correta — havia velocidade a ganhar — e de um palpite
sobre a causa. Medir antes de implementar mostrou que a causa era outra, e o ganho real
acabou **muito maior** do que o que a proposta original teria produzido.

---

## DEC-023 — Classes de domínio viram parciais; a metade de UI sai · VIGENTE
**2026-08-11**

As classes de `Backend/Equipment/`, `Backend/Uniques/` e as companhias de personagem
misturam regras de Shadowrun com métodos que constroem `TreeNode`, recebem
`TreeView`/`ContextMenuStrip` ou `Control`.

**Diferente das interfaces (DEC-018), aqui os membros de UI não estão agrupados no fim do
arquivo — estão interleaved com o domínio.** Em `Weapon.cs`, por exemplo, `Reload` (UI) vem
antes de `UnloadGear` (domínio), que vem antes de `CreateTreeNode` (UI), que vem antes de
`ImportHeroLabWeapon` (domínio). Cortar por linha não funciona.

**Solução: `partial class`.** A metade de regras fica em `Backend/`, a metade dependente de
WinForms vai para `Chummer/Controls/Dominio/<Tipo>.UI.cs`. No projeto legado as duas voltam
a ser uma classe só, então **nenhum chamador precisou mudar** — mesmo princípio da
`partial interface` de DEC-018, e igualmente protetor do build dos artefatos dourados.

**Ferramenta:** `scripts/extrair-ui.py`, que separa por membro com contagem de chaves
ciente de literais e comentários. Aplicada a 31 tipos.

**Detalhe que evita um erro fácil:** `Color`, `Point`, `Size` e `Rectangle` **não** contam
como tipos de UI. Vivem em `System.Drawing.Primitives`, que faz parte do framework
compartilhado e existe sob net9.0. Só `Image`, `Bitmap` e `Icon` exigem
`System.Drawing.Common`. Tratar `Color` como acoplamento teria arrastado centenas de
membros de domínio para fora sem necessidade.

**Resultado medido:** 367 → **175 erros**.

---

## DEC-024 — Verificador de sintaxe para o código extraído · VIGENTE
**2026-08-11**

Um furo apareceu ao usar a extração automática: **o censo compila apenas `Backend/` e
`Chummer.Core/`.** Os arquivos gerados em `Chummer/Controls/` não eram compilados por nada
em Linux — só pelo build net48 no CI Windows, minutos depois.

Numa extração feita por script sobre 350 mil linhas, isso é inaceitável: um gerador com
defeito produziria dezenas de arquivos quebrados antes de alguém perceber.

**`scripts/verificar-sintaxe-ui.sh`** compila `Chummer/Controls/` sob net9.0 e separa os
erros em duas famílias:

- **sintaxe** → falha; o arquivo gerado está quebrado;
- **semântica** → esperada; são os tipos de WinForms que não existem sob net9.0.

Não conseguimos *compilar* esses arquivos em Linux, mas conseguimos **provar que estão bem
formados** — que é exatamente o que a geração automática pode quebrar.

**Valeu a pena de imediato:** pegou um defeito real na primeira execução. A ferramenta abria
o `namespace` mas esquecia de emitir a declaração da `partial class`, deixando uma chave de
fechamento sobrando (`CS1022`). Sem o verificador, isso teria ido para o CI multiplicado por
31 arquivos.

**Segundo defeito, no próprio verificador:** classificar sintaxe pela faixa `CS1xxx` é
errado — `CS1069` ("tipo encaminhado para outro assembly") cai nela e é semântico, sendo
justamente o erro esperado de `System.Drawing` sob net9.0. Trocado por lista explícita de
códigos de sintaxe.

Mais uma entrada para o padrão de DEC-019: **a primeira versão de uma ferramenta de medição
esteve errada de novo.** Vale como regra do projeto, não como coincidência.

---

## DEC-025 — Telemetria sai do domínio por fábrica plugável · VIGENTE
**2026-08-11**

`Timekeeper` vive no domínio e é chamado por `Character`, `SkillsSection` e
`AttributeSection` para cronometrar o carregamento. Ele construía `CustomActivity`, que
herda `System.Diagnostics.Activity` **e** traz `Microsoft.ApplicationInsights` junto — o que
puxava telemetria para dentro do núcleo, contra PREM-005.

**O que tornou a solução simples:** o domínio nunca chama membro algum da atividade. Só usa
`using (...)` e repassa adiante. E `System.Diagnostics.Activity` **está na BCL e existe sob
net9.0**.

Portanto:

- as assinaturas do domínio passam a usar `Activity`, não `CustomActivity`;
- `Timekeeper` ganha `ActivityFactory`, uma `Func<...>` estática;
- `Program.cs` — aplicação legada — instala a fábrica que constrói `CustomActivity`;
- sem fábrica instalada, `StartSyncron` devolve `null` e os `using` do domínio viram
  no-ops, que é o comportamento correto com telemetria desligada.

O enum `CustomActivity.OperationType` foi extraído para `TelemetryOperationType`, em
`Chummer.Core`, para que o domínio possa nomear o tipo de operação sem depender do pacote.

**Comportamento do legado preservado integralmente:** a fábrica instalada no `Program.cs`
reproduz exatamente o que existia antes.

**Uma tentativa descartada no caminho:** trocar `CustomActivity` por `Activity` também nos
formulários. Não funciona — `CharacterCreate` e `CharacterCareer` chamam `SetSuccess`, que é
membro de `CustomActivity`, inclusive com `?.`, e método de extensão não pode ser invocado
com acesso condicional. Os formulários mantiveram o tipo concreto, com um cast nas 7
atribuições que recebem o retorno do `Timekeeper`.

**Resultado medido:** 142 → **132 erros**.

---

## DEC-026 — `IUserInteraction`: o domínio pergunta, a plataforma responde · VIGENTE
**2026-08-11**

Depois das extrações mecânicas, o censo mostrou que **o que resta é um único problema de
design**. Os identificadores dominantes nos arquivos ainda acoplados:

| Identificador | Ocorrências |
|---|---|
| `ThreadSafeForm` | 335 |
| `DialogResult` | 193 |
| `MessageBoxIcon` | 104 |
| `MessageBoxButtons` | 101 |

Concentrados em `AddImprovementCollection` e sua irmã assíncrona (160 cada) e em
`Character.cs` (133).

**Isto não é defeito a eliminar.** As regras de Shadowrun genuinamente perguntam ao usuário
no meio de um cálculo — *"escolha um atributo para receber +1"*, *"confirma remover esta
qualidade?"*. A pergunta é regra de jogo. O que muda é **quem responde**.

### O desenho

`Chummer.Core/Interaction/`:

- `PromptButtons`, `PromptIcon`, `PromptResult`, `PromptDefaultButton` — equivalentes
  neutros dos enums do WinForms;
- `IUserInteraction` — `ShowMessageAsync` e `ShowScrollableMessageAsync`;
- `UserInteraction` — **fachada estática** com implementação instalável;
- `SilentUserInteraction` — objeto nulo que responde `DefaultResult` sem perguntar nada.

**Por que fachada estática e não injeção de dependência:** o domínio já chama
`Program.ShowMessageBox(...)` estaticamente em centenas de pontos. Manter a forma estática
torna a migração uma substituição quase mecânica, em vez de exigir injetar um serviço em
classes de dezenas de milhares de linhas. Mesmo padrão de `Timekeeper.ActivityFactory`
(DEC-025), que funcionou.

**Objeto nulo em vez de `null`:** `SilentUserInteraction` é o valor inicial. Testes e
cenários headless funcionam sem travar esperando um usuário que não existe. E
`DefaultResult` é `OK`, não `Cancel` nem `None`, porque quem chama costuma testar
`== Cancel` para abortar — devolver `Cancel` faria operações legítimas abortarem em silêncio.

`Chummer/Controls/Infrastructure/WinFormsUserInteraction.cs` traduz as solicitações para as
caixas existentes, e `Program.cs` a instala no arranque. Comportamento do legado preservado.

**Ainda por fazer:** os ~300 pontos de chamada, e a metade de *seleção* (os diálogos
`Select*`), que é mais variada e ganha desenho próprio.

---

## DEC-027 — O núcleo está preso a C# 7.3 enquanto DEC-013 valer · VIGENTE
**2026-08-11**

Descoberto ao escrever a primeira classe de verdade no `Chummer.Core`: `Nullable` está
habilitado em `src/Directory.Build.props` e uma propriedade estática mutável exigia
anotação `?`.

O problema é que **todo arquivo do `Chummer.Core` é também compilado pelo projeto legado**
(DEC-013, uma fonte, duas compilações). O `Chummer.csproj` não fixa `LangVersion`, e para
alvo `net48` o padrão é **C# 7.3** — que não tem tipos de referência anuláveis, `using`
declaration, expressões `switch` nem `??=`.

Confirmado por varredura: o código legado não usa **nenhum** recurso de C# 8 ou superior.

**Consequência prática:** enquanto a compilação dupla existir, código novo no núcleo fica em
C# 7.3. Resolvi o caso concreto com **padrão de objeto nulo**, que dispensa anotações e é
melhor desenho — mas a restrição vale para tudo.

**Alternativa descartada:** fixar `LangVersion` alto no projeto legado. Mexer no build que
gera os artefatos dourados para conveniência do núcleo inverte a prioridade certa.

A restrição **cai sozinha** ao fim da Etapa 2, quando o `Backend/` for movido e o projeto
legado deixar de linkar o núcleo. Registrado como comentário em `src/Directory.Build.props`
para não ser redescoberto por acidente.

---

## DEC-028 — A dualidade sync/async da interação é deliberada e temporária · VIGENTE
**2026-08-11**

`IUserInteraction` expõe `ShowMessage` e `ShowMessageAsync`, duplicando cada operação.

**Por que não só a assíncrona:** o domínio tem **73 pontos de chamada síncronos** e 74
assíncronos. Converter os síncronos agora exigiria tornar assíncronos os métodos que os
contêm, e os que chamam esses, em cascata — que é exatamente o trabalho da **Etapa 5**
(eliminar `DoEvents` e as 279 execuções síncronas de código async).

Misturar as duas coisas seria ruim por um motivo específico: se a extração do núcleo e o
saneamento assíncrono viajarem no mesmo commit, uma regressão de regra detectada pelo teste
diferencial fica **impossível de atribuir** a uma causa. A separação existe para manter cada
divergência diagnosticável.

A metade síncrona morre na Etapa 5, junto com todo o resto do caminho síncrono.

**Migração feita:** 542 substituições em 30 arquivos —
`Program.ShowMessageBox*` → `UserInteraction.Show*`, e os enums do WinForms para os
equivalentes neutros. Censo: 95 → **89 erros**.

---

## DEC-029 — Um arquivo pode declarar vários tipos; a ferramenta precisa saber disso · VIGENTE
**2026-08-11**

O CI acusou `CS0117: 'CompareTreeNodes' does not contain a definition for 'CompareText'` em
15 pontos. Causa: `scripts/extrair-ui.py` **assumia um tipo por arquivo**.

`ListItem.cs` declara **seis** tipos — `ListItem`, `CompareTreeNodes`, `CompareListViewItems`,
`CompareListItems`, `ListViewColumnSorter`, `DataGridViewColumnSorter`. A ferramenta pegava
a primeira declaração e reemitia **todos** os membros extraídos dentro dela. `CompareText`,
que pertence a `CompareTreeNodes`, foi parar dentro de `partial struct ListItem`.

O domínio ficou correto — os membros saíram do lugar certo. Quem quebrou foi a metade de
UI, que os colocou no tipo errado.

**Por que não foi pego antes:** o `Chummer.Core` não compila `Controls/`, e o verificador de
sintaxe só checa que o arquivo está **bem formado** — e estava. Colocar um método no tipo
errado é erro **semântico**, não sintático. Só o build net48 completo detecta.

**Auditoria feita antes de consertar:** dos 35 tipos extraídos, quatro vieram de arquivos com
mais de um tipo — `ListItem`, `LanguageManager`, `Drugs` e `WeaponMount`. Nos três últimos,
os membros extraídos pertenciam de fato ao primeiro tipo, então só o `ListItem` estava
corrompido. Consertar às cegas os quatro teria sido pior que auditar.

**Correção da ferramenta:** ela agora mapeia todos os tipos do arquivo com suas linhas de
início, atribui cada membro extraído ao tipo que o contém, e emite **uma `partial` por
tipo**. Também passou a reconhecer `partial` como modificador — sem isso ela recusava
arquivos já processados, o que ao menos falhava de forma segura.

**A lição, e ela é a terceira do mesmo tipo:** meu verificador de sintaxe cobre o que a
geração automática quebra **na forma**, mas não o que ela quebra **no sentido**. Enquanto o
build net48 só existir no CI, erros semânticos em `Controls/` terão latência de minutos. É
custo aceito e conhecido — não custo surpresa.

---

## DEC-030 — Verificação semântica local do código extraído · VIGENTE
**2026-08-11**

Substitui `verificar-sintaxe-ui.sh` por **`verificar-ui.sh`**, que compila
`Backend/` + `Chummer.Core/` + as três pastas de `Controls/` juntas sob net9.0 e classifica
cada erro.

**Por que a versão anterior não bastava:** ela provava que o arquivo gerado estava **bem
formado**, e o bug de DEC-029 produziu arquivos perfeitamente bem formados com métodos no
tipo errado. Sintaxe válida, sentido errado. Só o build net48 no CI pegava, com minutos de
latência e depois de o commit já estar publicado.

**A classificação, e o detalhe que a torna correta:**

| Código | Veredito |
|---|---|
| `CS0246`, `CS0234`, `CS1069` | **esperado** — tipo de WinForms ou `System.Drawing` ausente sob net9.0. É a razão do porte existir. |
| `CS0103` | **depende do símbolo** |
| todo o resto | **defeito da extração** |

`CS0103` precisou de tratamento por nome, não por código. Um enum de WinForms usado como
valor — `RightToLeft eIntoRightToLeft = RightToLeft.Inherit` num parâmetro padrão — produz
`CS0103` e é esperado. Mas `MessageBoxButtons` sem o `using` também produz `CS0103` e é bug
real, exatamente o que quebrou o build antes. A diferença está no **nome citado na
mensagem**, e a primeira versão do classificador acusou os dois como problema.

**Estado atual:** 723 erros em `Controls/`, todos de acoplamento conhecido. Nenhum defeito de
extração.

**O que isto compra:** a latência de detecção de um defeito de extração cai de minutos, no
CI Windows, para segundos, em Linux. Não substitui o CI — continua sendo o único lugar que
prova que o net48 compila — mas tira dele o papel de primeira linha de defesa.


---

## DEC-031 — Âncoras de namespace na sondagem · VIGENTE
**2026-08-12**

`scripts/probe/NamespaceAnchors.cs` declara um tipo interno e vazio em cada namespace que a
sondagem não consegue resolver — `System.Windows.Forms`, `Microsoft.ApplicationInsights` e
mais três. Entra no censo e no verificador de UI; não entra em nenhum projeto do produto.

**O problema:** quando `using System.Windows.Forms;` não resolve, o Roslyn emite **um** erro,
o `CS0234` da própria diretiva, e suprime todos os erros de nome não resolvido no resto do
arquivo. Medido em repro mínimo: sete linhas com três referências a WinForms produzem 1 erro
sem âncora e 5 com âncora.

**O estrago no censo:** 28 arquivos colapsavam assim. `AddImprovementCollection.cs`, com 52
comparações a `DialogResult` e 54 usos de `ThreadSafeForm`, contava como **um** erro.

**Como funciona:** um namespace declarado em código-fonte só existe para o `using` se contiver
ao menos um tipo. Um tipo vazio por namespace basta — a diretiva resolve e cada referência
real volta a ter o seu próprio erro.

**O que deliberadamente NÃO se faz:** declarar os tipos de WinForms. O censo mede acoplamento;
declarar `DialogResult` esconderia exatamente o que ele conta. A âncora torna os erros
visíveis, não os faz sumir.

---

## DEC-032 — O censo mede declarações, não corpos de método · VIGENTE
**2026-08-12**

O número do censo conta **apenas erros de nível de declaração**. Acoplamento dentro de corpos
de método é invisível para ele enquanto existir um único erro de declaração no arquivo.

**A prova, mínima e reproduzível:**

```csharp
public class C {
    private MissingInDecl _field;                        // erro de DECLARAÇÃO
    public void Body() { var x = MissingInBody.Value; }  // erro de CORPO
}
```

Sozinho, o erro de corpo é reportado (`CS0103`). Com o erro de declaração presente, o
compilador reporta **só a declaração** — o erro de corpo desaparece por completo. O Roslyn
de linha de comando compila em fases e não vincula corpos de método se a fase de declaração
já produziu erros.

**Como isso apareceu:** a migração para a fachada de interação (DEC-026) quebrou 164
expressões — `ShowDialogSafe(...) == PromptResult.Cancel`, comparando `DialogResult` com
`PromptResult`. Todas em corpos de método. O censo em Linux não viu nenhuma; o build net48
no CI Windows viu todas, porque lá as declarações estão limpas. Duas execuções de CI
vermelhas antes de a causa ficar clara.

**O que isso muda na leitura do progresso:** a série 733 → 671 → … → 57 nunca significou "92%
do acoplamento resolvido". Significa "o acoplamento **de declaração** está quase resolvido; o
de corpo segue não medido". As 329 chamadas a `MessageBox`, as ~120 instanciações de diálogo
e os 146 `Application.DoEvents()` estão quase todos em corpos — e portanto fora da conta.

**Consequência de estratégia:** zerar os erros de declaração deixa de ser um marco entre
outros e passa a ser **pré-requisito de medição**. Só depois disso o censo enxerga o resto.
Dos 57 atuais, 26 são `Image`/`Icon`/`Bitmap` em assinaturas — o que promove DEC-021
(mugshots como bytes) de dívida conhecida a próximo passo do caminho crítico.

**A lição, e é a quarta do mesmo tipo (ver DEC-019):** o primeiro número que uma ferramenta de
medição produz esteve errado quatro vezes — duplicado pelo MSBuild, falso zero por SDK
errado, inflado por conjunto incompleto, e agora cego para corpos de método. A ferramenta
merece a mesma desconfiança que o código que ela mede.

---

## DEC-033 — O extrator funde em vez de sobrescrever, e conserva membros · VIGENTE
**2026-08-12**

`scripts/extrair-ui.py` reescrevia `Tipo.UI.cs` do zero a cada execução. Rodá-lo de novo no
mesmo arquivo — o que acontece toda vez que um tipo novo entra em `TIPOS_UI` — **apagava**
o que a execução anterior havia extraído, sem devolver nada ao domínio. `Vehicle.UI.cs`
perdeu dois dos três membros assim, e os dois simplesmente sumiram da árvore.

Três mudanças:

**Fusão.** A ferramenta lê o `.UI.cs` anterior, recupera o corpo de cada tipo parcial e o
reemite junto com os membros novos. Tipos que só existiam na extração anterior continuam no
arquivo — sem isso, extrair um tipo de um arquivo com vários apagaria os outros.

**Conservação.** Depois de montar as duas metades, a soma de membros declarados é comparada
com a de antes. Se algum sumiria, a extração é abortada e a metade de domínio já gravada é
restaurada. É barato e teria pego a destruição na hora, em vez de num `git diff --stat` lido
por acaso.

**O nome do membro sai da assinatura antes da marcação.** `public int SortOrder` casava com
`SortOrder` da lista de tipos de UI — mas ali `SortOrder` é o nome do membro, o índice de
ordenação `int` de `ICanSort`, e não o enum de WinForms. Vehicle e Improvement tiveram a
propriedade arrancada do domínio por isso, quebrando a implementação da interface. A
marcação agora remove o identificador do próprio membro antes de procurar tipos.

**Estado após a correção:** censo de declarações em **20 erros, todos do mesmo subsistema** —
a API de mugshots. Todo o resto do acoplamento de declaração do `Backend/` está resolvido.

**A lição:** uma ferramenta que MOVE código nunca deve poder APAGÁ-LO. A verificação de
conservação não é defesa contra um bug específico; é a invariante da ferramenta, e devia
estar lá desde a primeira versão.
