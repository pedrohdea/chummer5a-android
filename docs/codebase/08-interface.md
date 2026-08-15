# 08 — Interface WinForms

~241.000 linhas entre `Forms/` (175 arquivos) e `Controls/` (67 arquivos). É a maior
superfície do projeto e a única parte que será integralmente descartada no porte — por isso
este documento foca em **o que a UI faz**, não em como está escrita.

## Hierarquia de janelas

```
ChummerMainForm (4.862 linhas)
├── CharacterRoster            ← lista de personagens salvos, favoritos, recentes
├── CharacterCreate  (24.849)  ← edição em modo Create
├── CharacterCareer  (28.494)  ← edição em modo Career
│   └── ambos herdam de CharacterShared (13.069)
├── CharacterSheetViewer       ← ficha impressa (XSLT → WebBrowser)
├── ExportCharacter
├── MasterIndex                ← índice pesquisável de todo o conteúdo
└── utilitários (dados, iniciativa, painéis, atualizador…)
```

**Três arquivos concentram 66.412 linhas**: `CharacterCareer`, `CharacterCreate` e a base
comum `CharacterShared`. Essa é a maior massa de código de UI do projeto, e a razão de o
MVP do porte começar por um leitor: recriar essas três telas é a maior parte do trabalho.

## Inventário completo de telas

### Criação de personagem (4)
`SelectBuildMethod` · `SelectLifeModule` · `SelectMetatypeKarma` · `SelectMetatypePriority`

### Telas de personagem (3)
`CharacterCreate` · `CharacterCareer` · `CharacterShared` (base)

### Formulários de criação de conteúdo (8)
`CreateCustomDrug` · `CreateCyberwareSuite` · `CreateExpense` · `CreateImprovement` ·
`CreateNaturalWeapon` · `CreatePACKSKit` · `CreateSpell` · `CreateWeaponMount`

### Janelas de seleção (45)

```
SelectAIProgram          SelectArmor              SelectArmorMod
SelectArt                SelectAttribute          SelectCalendarStart
SelectComplexForm        SelectContactConnection  SelectCritterPower
SelectCyberware          SelectCyberwareSuite     SelectDiceHits
SelectDrug               SelectExoticSkill        SelectGear
SelectItem               SelectLifestyle          SelectLifestyleQuality
SelectLifestyleStartingNuyen  SelectLimit         SelectLimitModifier
SelectMartialArt         SelectMartialArtTechnique SelectMentorSpirit
SelectMetamagic          SelectNumber             SelectOptionalPower
SelectPACKSKit           SelectPower              SelectProgramOption
SelectQuality            SelectSetting            SelectSide
SelectSkill              SelectSkillCategory      SelectSkillGroup
SelectSkillSpec          SelectSpell              SelectSpellCategory
SelectText               SelectVehicle            SelectVehicleMod
SelectWeapon             SelectWeaponAccessory    SelectWeaponCategory
```

Todas seguem o mesmo padrão: catálogo vindo do XML, busca, filtro por livro/disponibilidade/
custo, painel de detalhes, confirmação. **É o padrão mais repetido do projeto** — e, no
porte, o candidato mais óbvio a virar uma única tela genérica parametrizada em vez de 45.

### Painéis de mesa (3)
`AddToken` · `GameMasterDashboard` · `PlayerDashboard`

### Utilitários (15)
`About` · `CharacterRoster` · `CharacterSheetViewer` · `ChummerUpdater` · `DataExporter` ·
`DiceRoller` · `ExportCharacter` · `HeroLabImporter` · `InitiativeRoller` ·
`InitiativeTracker` · `LoadingBar` · `MasterIndex` · `PrintMultipleCharacters` ·
`TestDataEntries` · `VersionHistory`

## Controles customizados

| Pasta | Controles |
|---|---|
| `Attributes/` | `AttributeControl` |
| `Characters/` | `ContactControl` · `PetControl` · `SpiritControl` |
| `Charts/` | `ExpenseChart` (LiveCharts.WinForms) |
| `Dashboards/` | `ConditionMonitorUserControl` · `InitiativeUserControl` |
| `Editors/` | `RtfEditor` |
| `Powers/` | `PowersTabUserControl` |
| `Skills/` | `SkillControl` · `KnowledgeSkillControl` · `SkillGroupControl` · `SkillsTabUserControl` |
| `Shared/` | `BindingListDisplay` · `ObservableCollectionDisplay` · `DicePoolControl` · `LimitTabUserControl` · `SustainedObjectControl` |
| `Table/` | um sistema de tabela próprio: `TableView`, `TableColumn`, `TableRow`, `TableCell` e células tipadas (`ButtonTableCell`, `CheckBoxTableCell`, `SpinnerTableCell`, `TextTableCell`) |
| `Shared/Components/` | `ElasticComboBox` · `SplitButton` · `NumericUpDownEx` · `ColorableCheckBox` · variantes `…WithToolTip` e `DpiFriendly…` |

O sistema de tabela próprio e os controles `DpiFriendly*` existem porque o WinForms não
resolve bem virtualização de listas grandes nem escalonamento DPI. **Ambos os problemas
desaparecem em frameworks modernos** — esse código não precisa ser portado, precisa ser
abandonado.

## Padrões de UI

### `DoThreadSafe` — 7.102 ocorrências

O padrão dominante. Toda interação com controle a partir de código assíncrono passa por
`DoThreadSafe` / `DoThreadSafeAsync` / `DoThreadSafeFuncAsync`
(`Backend/Static/Extensions/WinFormsExtensions.cs`):

```csharp
await webViewer.DoThreadSafeAsync(x => x.DocumentText = strDocumentText, token);
```

É a ponte manual entre o modelo assíncrono do domínio e a afinidade de thread do WinForms.

### `ThreadSafeForm<T>`

Wrapper em `Backend/Helpers/ThreadSafeForm.cs` que permite criar e exibir formulários a
partir de qualquer thread. Usado por `Program.CreateAndShowProgressBar[Async]`.

### `TreeView` — 770 referências a `TreeNode` em `Forms/` + `Controls/`

Boa parte da UI é árvore: equipamento aninhado, veículos com mods, armas com acessórios.
Como registrado em [04](04-modelo-dominio.md), há mais **344 referências dentro do
`Backend/`** — 78 delas em `Character.cs`, na região "UI Methods / Move TreeNodes".

### Tema claro/escuro

`Backend/Static/Managers/ColorManager.cs` implementa modo claro/escuro. O mecanismo de
detecção:

> um `Timer` que **consulta o Registro do Windows a cada 5 segundos** para saber se o
> sistema está em modo escuro.

Funciona, mas é inteiramente específico do Windows.

### DPI

`Program.SetProcessDPI` / `SetThreadDPI` via P/Invoke (`SHCore.dll`, `user32.dll`), mais a
família de controles `DpiFriendly*`. Também inteiramente específico do Windows.

## O que é aproveitável

Quase nada em termos de código. O que é aproveitável são as **decisões de produto** já
tomadas e testadas por anos de uso:

- quais campos aparecem em cada aba e em que agrupamento
- quais filtros as janelas de seleção precisam ter
- o fluxo de criação de personagem por cada um dos 4 métodos de construção
- quais informações o mestre precisa nos painéis

Ao redesenhar para toque, essa é a fonte de requisitos. O código WinForms é a documentação
executável desses requisitos — e é por isso que ele deve ser lido antes de ser descartado.
