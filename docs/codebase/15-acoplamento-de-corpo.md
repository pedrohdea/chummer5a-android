# 15 — Acoplamento de corpo de método: a primeira medição

**2026-08-12.** Este documento registra o que apareceu quando os erros de **declaração** do
`Backend/` chegaram a **zero** e o compilador finalmente vinculou os corpos de método.

Reproduzível: `./scripts/censo-erros.sh` · erros crus em `<dir>/erros.txt`

---

## Por que esta medição não existia antes

O Roslyn de linha de comando compila em fases e **não vincula corpos de método enquanto a
fase de declaração produziu erro** (DEC-032). Um campo de tipo inexistente apagava por
completo um `MessageBox` no método ao lado. Enquanto restasse **um** erro de declaração, o
censo era cego para o interior de todo método do projeto.

O documento 14 mede a frente de declaração e diz, no topo, que os números dele **não
incluem** o acoplamento de corpo. Esta página é a outra metade — a que só pôde ser contada
depois que os últimos 20 erros de declaração (os retratos, DEC-034) saíram do caminho.

**Portanto: o salto de 20 para 1.584 não é uma regressão.** É a luz acendendo.

| Momento | Erros do censo | O que estava sendo medido |
|---|---|---|
| Etapa 1.3 (doc 14) | 733 | declaração, antes das âncoras de namespace |
| Depois de DEC-031 | 57 | declaração |
| Antes de DEC-034 | 20 | declaração — só os retratos |
| **Depois de DEC-034** | **1.584** | **corpos de método, pela primeira vez** |

Erros de declaração restantes: **zero**. Nenhum `CS0535` (membro de interface não
implementado), e os 7 `CS1069` que sobraram estão todos em corpo de método, dentro de
`CharacterCache.cs`.

---

## O resultado

**1.584 erros distintos.**

| Código | Ocorrências | O que é |
|---|---|---|
| `CS0103` | 826 | nome não existe no contexto — **corpo de método** |
| `CS0246` | 541 | tipo ou namespace não encontrado |
| `CS1061` | 187 | membro não existe no tipo |
| `CS1662` / `CS0029` | 20 | conversão de lambda / de tipo |
| `CS1069` | 7 | tipo ausente no framework alvo (`Image`, em `CharacterCache`) |
| outros | 3 | `CS1929`, `CS0121`, `CS0117` |

### Por arquivo (os 15 maiores)

| Arquivo | Erros |
|---|---|
| `Backend/Improvements/AddImprovementCollection.cs` | 262 |
| `Backend/Improvements/AddImprovementAsyncCollection.cs` | 262 |
| `Backend/Characters/Character.cs` | 123 |
| `Backend/Equipment/Gear.cs` | 91 |
| `Backend/Equipment/Cyberware.cs` | 79 |
| `Backend/Equipment/Armor.cs` | 55 |
| `Backend/Equipment/Weapon.cs` | 54 |
| `Backend/Equipment/Vehicle.cs` | 52 |
| `Backend/Static/Utils.cs` | 42 |
| `Backend/Static/Managers/ImprovementManager.cs` | 41 |
| `Backend/Characters/CharacterCache.cs` | 39 |
| `Backend/Equipment/WeaponMount.cs` | 31 |
| `Backend/Characters/Spirit.cs` | 27 |
| `Backend/Characters/Contact.cs` | 27 |
| `Backend/Static/CommonFunctions.cs` | 24 |

**`AddImprovementCollection.cs` e `AddImprovementAsyncCollection.cs` somam 524 dos 1.584 —
um terço do total — com 262 erros cada, número idêntico.** É a confirmação mecânica do que
o `CLAUDE.md` já afirmava: são a mesma lógica escrita duas vezes. Consolidar os dois
arquivos resolve dois terços desses 524 de uma vez, e é de longe a maior alavanca isolada
do restante da Etapa 2.

---

## As quatro frentes reais, por símbolo ausente

Agrupando por **o que** está faltando, e não por código de erro:

| Frente | Ocorrências | Natureza |
|---|---|---|
| `ColorManager` | 336 | tema/cor — o domínio decide cor de item de árvore |
| `ThreadSafeForm<T>` + `DialogResult` + `Form` | 337 | o domínio **abre diálogo** e lê o resultado |
| Diálogos de seleção (`Select*`) | ~300 | **24 tipos distintos** instanciados de dentro do domínio |
| `Program` | 102 | fachada global de UI (`ShowScrollableMessageBox`, `MainForm`, …) |
| Atributos de Matriz (`GetTotalMatrixAttribute*` e afins) | ~180 | **não é acoplamento de plataforma** — ver abaixo |
| `Application`, `CursorWait`, `ListViewItem`, `DataGridViewRow`, … | ~40 | WinForms diverso |

Os 24 diálogos de seleção distintos que o domínio instancia:

`SelectAIProgram` · `SelectArmorMod` · `SelectArt` · `SelectAttribute` · `SelectBuildMethod`
· `SelectComplexForm` · `SelectDiceHits` · `SelectItem` · `SelectLimit` · `SelectMartialArt`
· `SelectMentorSpirit` · `SelectMetamagic` · `SelectMetatypeKarma` · `SelectMetatypePriority`
· `SelectNumber` · `SelectOptionalPower` · `SelectPower` · `SelectSide` · `SelectSkill` ·
`SelectSkillGroup` · `SelectSpell` · `SelectSpellCategory` · `SelectText` ·
`SelectWeaponCategory`

Isto é o inventário exato do padrão de **solicitação de escolha ao usuário** que o MVP
precisa destravar. O `CLAUDE.md` estima 45 diálogos no total; 24 deles são chamados de
dentro do domínio e aparecem aqui.

### A frente que não é acoplamento de plataforma

`GetTotalMatrixAttribute`, `GetMatrixAttributeString`, `SetHomeNode`, `SetActiveCommlink` e
companhia — cerca de 180 erros `CS1061` sobre `IHasMatrixAttributes` — são **métodos de
extensão de domínio puro** que ainda não foram para o projeto de sondagem. Não têm nada de
WinForms. É trabalho de arrastar arquivo, não de refatorar.

O mesmo vale para `ClearInitiations`, `ClearMagic`, `ClearResonance`,
`ClearCyberwareTab` e `MatrixAttributes` em `Character`: membros que ficaram do lado de
fora quando a classe foi partida em duas, e voltam com uma movida de código.

**Consequência prática:** dos 1.584, algo em torno de **200 não exigem decisão de
arquitetura nenhuma**. O número de erros que representam acoplamento real à plataforma está
mais perto de 1.380.

---

## O que fazer com isso, em ordem de retorno

1. **Consolidar `AddImprovementCollection` com `AddImprovementAsyncCollection`** — 524
   erros, um terço do total, e o ganho vai muito além do censo: são ~15 mil linhas de
   lógica de regras duplicada.
2. **`ColorManager`** — 336 ocorrências de um único símbolo, e é a mais mecânica das
   frentes: o domínio precisa expor *intenção* (item com nota, item de fonte proibida) e
   deixar a cor para a apresentação.
3. **O padrão de solicitação de escolha** — `ThreadSafeForm` + `DialogResult` + os 24
   `Select*`. É a frente cara, a que exige desenho, e é a que o MVP já previu exercitar com
   "recarregar arma".
4. **`Program`** — fachada global; 102 ocorrências, quase todas `MessageBox` disfarçado.
5. **Arrastar o que já é domínio** — atributos de Matriz e os `Clear*` de `Character`:
   ~200 erros sem decisão nenhuma pela frente.

---

## Nota sobre `CharacterCache`

Os 39 erros de `CharacterCache.cs`, e os 7 `CS1069` de `Image` entre eles, são o mesmo
subsistema de retratos que DEC-034 resolveu para `IHasMugshots` — mas `CharacterCache` não
implementa essa interface e por isso ficou de fora daquela conversão. Ele guarda um retrato
já comprimido (`GetCompressedImage`) para a lista de personagens, e a compressão é trabalho
de imagem de verdade, não de armazenamento.

Convertê-lo para bytes é possível pelo mesmo desenho, mas exige decidir onde a compressão
passa a acontecer sem que a lista de personagens carregue retratos em tamanho cheio na
memória. Fica registrado como pendência da frente de retratos, não como esquecimento.
