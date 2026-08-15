# 03 — Arquitetura

## Panorama

O Chummer é uma aplicação **.NET Framework 4.8, WinForms** (com WPF habilitado no projeto,
usado pontualmente). Não é uma arquitetura em camadas no sentido formal: é um executável
único em que a separação entre domínio e interface é **convencional, por pasta, e não
enforçada por fronteira de projeto**.

```
Chummer.sln
├── Chummer/                    ← o aplicativo (net48, WinExe)
│   ├── Program.cs              ← ponto de entrada + serviços globais estáticos
│   ├── Backend/                ← domínio e regras         ~350.000 LOC / 245 arquivos
│   ├── Forms/                  ← 78 telas                 ~214.000 LOC / 175 arquivos
│   ├── Controls/               ← 40+ controles            ~27.000 LOC / 67 arquivos
│   ├── Plugins/                ← carregador MEF
│   ├── 7zip/                   ← LZMA gerenciado (SDK 7-Zip)
│   ├── Properties/             ← recursos gerados
│   ├── data/ customdata/ lang/ sheets/   ← ~21 MB de conteúdo
│   └── docs/                   ← wiki de autoria de dados
├── Chummer.Tests/              ← testes (net48)
├── Chummer.Benchmarks/
├── ChummerHub/                 ← serviço web ASP.NET (net6.0)
├── Plugins/ChummerHub.Client/  ← plugin cliente do Hub
├── Plugins/SamplePlugin/
├── CrashHandler/
├── ChummerDataViewer/
├── Translator/
└── TextblockConverter/
```

## Dimensões

| Métrica | Valor |
|---|---|
| Arquivos `.cs` no projeto principal | 513 |
| Linhas de C# no projeto principal | ~625.000 |
| Arquivos `.Designer.cs` (código gerado de UI) | 116 |
| Arquivos `.resx` | 108 |
| Maior arquivo: `Backend/Characters/Character.cs` | **56.290 linhas** |
| Conteúdo XML/XSLT | ~21 MB (`data` 7,2 MB · `lang` 9,7 MB · `customdata` 2,2 MB · `sheets` 1,6 MB) |
| Chamadas `ConfigureAwait` | ~47.200 |
| Métodos `async Task` públicos | ~2.960 |
| `DllImport` | 80 |

## As três camadas de fato

### 1. `Program` — serviços globais estáticos

`Program.cs` (1.718 linhas) é muito mais que um `Main`. É o **service locator estático**
de todo o aplicativo:

- `Program.OpenCharacters` — a coleção observável de personagens abertos
- `Program.MainForm` — referência global à janela principal
- `Program.LoadCharacter[Async]`, `OpenCharacter`, `OpenCharacterForPrinting`, `…ForExport`
- `Program.ShowMessageBox[Async]` e `ShowScrollableMessageBox[Async]` — as caixas de diálogo
- `Program.CreateAndShowProgressBar[Async]` — barras de progresso
- `Program.PluginLoader` — o carregador MEF
- `Program.ChummerTelemetryClient` — Application Insights
- `Program.SetProcessDPI` / `SetThreadDPI` — DPI awareness via P/Invoke

**Isto é o acoplamento central do sistema.** O `Backend/` chama `Program.*` livremente —
`Character.cs` sozinho referencia `Program.ShowScrollableMessageBox[Async]` 24 vezes.

### 2. `Backend/` — domínio e regras

Organizado por assunto:

| Pasta | Conteúdo |
|---|---|
| `Characters/` | `Character.cs` — o agregado central |
| `Attributes/` | `CharacterAttrib`, `AttributeSection` |
| `Skills/` | `Skill`, `SkillGroup`, `KnowledgeSkill`, `ExoticSkill`, `SkillsSection` (~20.500 LOC) |
| `Equipment/` | `Gear`, `Weapon`, `Armor`, `Cyberware`, `Vehicle`, `Drug`, `Lifestyle`, mods e acessórios |
| `Uniques/` | `Quality`, `Spell`, `Power`, `Contact`, `Spirit`, `MartialArt`, `Metamagic`, `MentorSpirit`, … |
| `Improvements/` | O motor de regras (~21.900 LOC) |
| `Static/` | `Utils`, `GlobalSettings`, `CommonFunctions`, `SelectionShared`, `NativeMethods` e os *Managers* |
| `Static/Managers/` | `XmlManager`, `ImprovementManager`, `LanguageManager`, `ColorManager` |
| `Datastructures/` | 37 coleções e tipos utilitários próprios |
| `Helpers/` | Locks assíncronos, pools de objetos, temporizadores |
| `Character Settings/` | Rulesets e custom data |
| `Enums/`, `Interfaces/`, `Stories/`, `Debugging/` | — |

### 3. `Forms/` e `Controls/` — interface

WinForms puro, com designers gerados. Ver [08 — Interface](08-interface.md).

## Padrões arquiteturais em uso

### Duplicação sync/async deliberada

O código sustenta **duas implementações de quase tudo**: uma síncrona e uma assíncrona.
Isso aparece de duas formas:

1. **Métodos `Core` com flag `blnSync`** — 168 ocorrências. Um único corpo de método recebe
   `bool blnSync` e escolhe o caminho em cada ponto de espera. Exemplo:
   `Program.LoadCharacterCoreAsync(bool blnSync, …)`.
2. **Classes irmãs completas** — `AddImprovementCollection.cs` (7.219 linhas) e
   `AddImprovementAsyncCollection.cs` (7.663 linhas) são a mesma lógica escrita duas vezes.

É a maior fonte de volume redundante da base e um alvo óbvio de consolidação no porte.

### Managers estáticos

`XmlManager`, `ImprovementManager`, `LanguageManager`, `ColorManager` e `GlobalSettings`
são classes **estáticas com estado global e cache interno**. Não há injeção de dependência
em lugar nenhum do projeto principal.

### Concorrência pesada e caseira

O projeto implementa a própria biblioteca de sincronização assíncrona:
`AsyncFriendlyReaderWriterLock` (1.716 linhas), `LinkedAsyncRWLockHelper` (1.557),
`DebuggableSemaphoreSlim` (465), `AsyncLock` (266) — e 37 coleções thread-safe próprias.
Cada objeto de domínio tem um `LockObject`. Ver [07 — Concorrência](07-concorrencia.md).

### Notificação de mudança por grafo de dependências

`PropertyDependencyGraph` e `DependencyGraph` modelam explicitamente quais propriedades
dependem de quais, para que alterar uma dispare `PropertyChanged` em cascata nas derivadas.
`MultiplePropertiesChangedEventArgs` existe para notificar lotes.

### Dados como conteúdo, não como código

Nenhum item de jogo está no código. Tudo é XML carregado, mesclado e cacheado em runtime.
Ver [05 — Sistema de Dados](05-sistema-dados.md).

## O que a arquitetura atual assume

Estas premissas estão embutidas em todo o código e cada uma delas quebra no Android:

1. Existe um sistema de arquivos livre, com caminhos absolutos ao lado do executável.
2. Existe o Registro do Windows.
3. Existe uma UI thread do WinForms, e é aceitável bloqueá-la (`Application.DoEvents`
   aparece 146 vezes).
4. Existe `System.Drawing` para imagens e cores.
5. Existe interoperabilidade nativa com Win32.
6. Diálogos modais podem ser abertos de qualquer lugar, inclusive de dentro de regras de
   negócio.

O inventário detalhado está em [13 — Acoplamento à Plataforma](13-acoplamento-plataforma.md).
