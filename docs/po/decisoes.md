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

