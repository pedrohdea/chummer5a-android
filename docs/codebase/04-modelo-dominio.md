# 04 — Modelo de Domínio

## `Character` — o agregado central

`Backend/Characters/Character.cs`, **56.290 linhas em um único arquivo**. É o objeto que
tudo orbita. Contém o estado do personagem, as ~30 coleções de seus componentes, e
centenas de propriedades calculadas que aplicam as regras de Shadowrun.

### Regiões do arquivo

O arquivo é navegado por `#region`. As principais, em ordem:

| Linha aprox. | Região | Conteúdo |
|---|---|---|
| 286 | Initialization, Save, Load, Print, Reset | ciclo de vida |
| 2694 | Create, Save, Load and Print Methods | serialização |
| 13554 | Helper Methods | — |
| 17532 | **UI Methods** / Move TreeNodes / Tab clearing | **manipulação direta de `TreeNode` do WinForms dentro do domínio** |
| 19316 | Basic Properties | nome, metatipo, flags |
| 24438 | Attributes | — |
| 29032 | Initiative (Physical / Astral / Matrix AR / Cold Sim / Hot Sim) | quatro modos de iniciativa |
| 30225 | XPath Processing | avaliação de expressões vindas dos dados |
| 31693 | Reputation | Street Cred, Notoriety, Public Awareness |
| 33533 | Armor Properties | — |
| 34025 | Dodge / Spell Defense (Indirect Dodge, Indirect Soak, Direct Soak Mana, Direct Soak Physical, Detection) | defesas mágicas |
| 38011 | Condition Monitors | trilhas de dano |
| 38754 | Build Properties | orçamento de construção |
| 40208 | Metatype/Metavariant Information | — |
| 41922 | Special Functions and Enabled Check Properties | `MAGEnabled`, `RESEnabled`, `AdeptEnabled`… |
| 44276 | Old Quality Conversion Code | migração de saves antigos |
| 45386 | Temporary Properties : Dashboard | estado transitório de UI |
| 51076 | Hero Lab Importing | importação de formato concorrente |
| 54444 | Karma Values | — |

Observação importante: existe uma região **"UI Methods"** com manipulação de `TreeNode`
dentro do arquivo de domínio. Não é acidente isolado — é uma decisão de design da base.

### As coleções do personagem

Um `Character` agrega 29 coleções observáveis (`ThreadSafeObservableCollection<T>`):

```
Improvements     MentorSpirits   Contacts        Spirits         Spells
SustainedObjects ComplexForms    AIPrograms      MartialArts     LimitModifiers
Armor            Cyberware       Weapons         Qualities       Lifestyles
Gear             Vehicles        Metamagics      Arts            Enhancements
ExpenseLog       CritterPowers   InitiationGrades Drugs          ImprovementGroups
GearLocations    ArmorLocations  VehicleLocations WeaponLocations
```

Mais duas seções compostas: `AttributeSection` (os 14 atributos, incluindo os especiais)
e `SkillsSection` (perícias, grupos, conhecimento).

**Consequência para o porte:** abrir um `.chum5` instancia ao menos 30 tipos distintos do
domínio. Não existe subconjunto pequeno do `Backend/` que possa ser portado isoladamente
— carregar um personagem exige a árvore inteira.

## Ciclo de vida

```
   novo personagem                      personagem existente
         │                                       │
         ▼                                       ▼
  Create mode  ──── finalizar ────►  Career mode  ◄──── Load(.chum5)
  (Created=false)                    (Created=true)
         │                                       │
         └──────────► Save(.chum5) ◄─────────────┘
                            │
                            ▼
                    Print (XSLT → HTML)
                    Export (dados)
```

`Character.Created` é a chave: uma vez `true`, o sistema de construção por pontos é
desligado permanentemente e o personagem passa a evoluir por karma. A transição é de mão
única e está documentada no próprio código como tal.

## Serialização — o formato `.chum5`

### Estrutura

XML com raiz `<character>`, escrito por `XmlWriter`. O primeiro elemento gravado é
`appversion` — a versão do Chummer que salvou o arquivo.

### Duas extensões

| Extensão | Formato |
|---|---|
| `.chum5` | XML puro |
| `.chum5lz` | O mesmo XML comprimido com **LZMA** |

A compressão usa a implementação **gerenciada** do SDK do 7-Zip, embarcada em
`Chummer/7zip/` (15 arquivos, ~6.400 LOC) e acessada por `Backend/Helpers/LzmaHelper.cs`.
Verificado: **zero `DllImport` nessa pasta** — é portável como está.

### Migração de saves antigos

O `appversion` é lido no carregamento e usado para aplicar conversões. A região "Old
Quality Conversion Code" (linha ~44.276) é um exemplo: qualidades salvas em formatos
antigos são convertidas ao carregar. Qualquer alteração no formato precisa preservar esse
caminho de compatibilidade.

### Retratos

`Character.Mugshots` é um `ThreadSafeList<Image>` — `System.Drawing.Image`. Os retratos são
serializados no XML em base64. `MainMugshot` aponta para o principal. Este é um dos pontos
de contato com `System.Drawing` mais fundo no domínio.

## A assinatura que resume o problema

```csharp
public bool Load(string strFileName = "", LoadingBar frmLoadingForm = null,
                 bool showWarnings = true, CancellationToken token = default)
```

`LoadingBar` é **um formulário WinForms**, recebido como parâmetro por um método de
domínio. Existem 16 ocorrências de `LoadingBar` dentro de `Backend/`, e 25 arquivos do
`Backend/` referenciam tipos de UI (`LoadingBar`, `ThreadSafeForm`, `CursorWait`) em
assinaturas.

## Tipos de apoio do domínio

`Backend/Datastructures/` contém 37 tipos, dos quais alguns são conceitualmente de domínio
e não meras coleções:

| Tipo | Papel |
|---|---|
| `AvailabilityValue` | Disponibilidade de um item, com sufixos (`R`, `F`) e aritmética própria |
| `NuyenString` | Valor monetário com formatação e parsing |
| `SourceString` | Referência livro+página, com tradução |
| `ValueVersion` | Versão semântica usada em comparações de compatibilidade |
| `TranslatedField` | Par valor-original / valor-traduzido |
| `ListItem` | Par exibição/valor para listas de UI — **usado dentro do `Backend/`** |
| `DependencyGraph` / `PropertyDependencyGraph` | Grafo de propriedades derivadas |
| `OrderInvariantHashCode` | Hash independente de ordem (tem testes dedicados) |

## Interação com o usuário dentro do domínio

O domínio **pergunta ao usuário** no meio do processamento de regras. Ocorrências medidas
dentro de `Backend/`:

| Chamada | Ocorrências |
|---|---|
| `MessageBox` (qualquer variante) | 329 |
| `new SelectItem(…)` | 35 |
| `new SelectNumber(…)` | 21 |
| `new SelectAttribute(…)` | 10 |
| `new SelectSkillGroup(…)` / `new SelectSkill(…)` | 6 cada |
| `new SelectSpellCategory / SelectMetamagic / SelectMentorSpirit / SelectBuildMethod / SelectAIProgram(…)` | 4 cada |
| demais diálogos `Select*` | 2–3 cada |

Isto não é um defeito acidental: o sistema de improvements do Shadowrun genuinamente exige
escolha do usuário no meio da aplicação de um bônus (ex.: "escolha um atributo para
receber +1"). O mecanismo de `ForcedValue` / `SelectedValue` / `LimitSelection` do
`ImprovementManager` existe justamente para mediar isso — ver
[06 — Motor de Regras](06-motor-regras.md).

O que o porte precisa mudar não é *que* o domínio pergunta, mas *como*: hoje ele constrói
um formulário; precisará emitir uma solicitação abstrata que a plataforma resolve.
