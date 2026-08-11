# 13 — Acoplamento à Plataforma Windows

Inventário do que prende a base de código ao Windows. Este documento é o insumo direto do
planejamento do porte: cada seção é um problema a resolver, com a medida do problema.

Todos os números foram medidos por varredura estática na árvore. Onde a leitura estática
não basta para afirmar comportamento, o texto diz.

---

## Resumo

| Acoplamento | Medida | Concentração |
|---|---|---|
| Interop nativa (`DllImport`) | 80 | **1 arquivo** |
| Registro do Windows | 7 arquivos | disperso |
| `System.Windows.Forms` no `Backend/` | 69 de 245 arquivos | disperso |
| `System.Drawing` no `Backend/` | 46 arquivos | parcialmente concentrado |
| `MessageBox` no `Backend/` | 329 chamadas | disperso |
| Diálogos `Select*` instanciados no `Backend/` | ~120 chamadas | disperso |
| Tipos de UI em assinaturas do `Backend/` | 25 arquivos | disperso |
| `Application.DoEvents()` | 146 | UI e domínio |
| Execução síncrona de código async | 279 | disperso |

---

## 1. Interop nativa — **melhor notícia do inventário**

As **80 chamadas `DllImport` estão todas em um único arquivo**:
`Backend/Static/NativeMethods.cs`.

| Biblioteca | Chamadas | Para quê |
|---|---|---|
| `user32.dll` / `user32` | 29 | DPI, mensagens de janela, `SendMessage` |
| `kernel32.dll` / `kernel32` | 5 | processos, strings do sistema |
| `dbghelp.dll` | 2 | minidumps de falha |
| `winspool.drv` | 2 | `SetDefaultPrinter` |
| `Shell32.dll` | 1 | ícones do sistema (`SHSTOCKICONID`) |
| `SHCore.dll` | 1 | DPI awareness |

E apenas **8 arquivos** referenciam `NativeMethods.*`:
`Program.cs`, `ScrollableMessageBox.cs`, `CharacterSheetViewer.cs`, `Chummy.cs`,
`ChummerMainForm.cs`, `Backend/Debugging/CrashHandler.cs`, `Backend/Static/Utils.cs`,
`Backend/Helpers/CenterableMessageBox.cs`.

Usos mais frequentes: `GetSystemString` (13), `GetStockIcon`/`SHSTOCKICONID` (12+12),
gerenciamento de DPI (`SetProcessDPIAware`, `SetProcessDpiAwareness`,
`SetThreadDpiAwarenessContext`), `WM_COPYDATA`/`SendMessage` (comunicação entre instâncias
do aplicativo), `SetDefaultPrinter`.

**Avaliação:** nenhuma dessas funcionalidades precisa existir no Android. DPI é resolvido
pelo framework; ícones do sistema vêm do tema; comunicação entre instâncias não se aplica;
impressão usa a API da plataforma; minidumps são substituídos pelo relatório de falhas do
Android. Este acoplamento é **grande em contagem e trivial em esforço** — apaga-se o
arquivo e os 8 chamadores.

---

## 2. Registro do Windows

Sete arquivos:

| Arquivo | Uso |
|---|---|
| `Backend/Static/GlobalSettings.cs` | **toda a persistência de preferências do aplicativo** |
| `Backend/Static/Utils.cs` | `SetupWebBrowserRegistryKeys` — emulação de versão do IE |
| `Backend/Static/Managers/ColorManager.cs` | detecção de modo escuro, por polling a cada 5 s |
| `Backend/Character Settings/CustomDataDirectoryUpdater.cs` | — |
| `Backend/Debugging/CrashHandler.cs` | — |
| `Forms/Utility Forms/CharacterSheetViewer.cs` | — |
| `Plugins/PluginControl.cs` | registro do protocolo `chummer://` |

O caso central é `GlobalSettings`: `s_ObjBaseChummerKey` é uma `RegistryKey` e todas as
preferências são lidas e escritas por `OpenSubKey`/`SetValue`.

**Avaliação:** é uma substituição mecânica — trocar a implementação de leitura/escrita por
um armazenamento de chave-valor da plataforma, mantendo a API de `GlobalSettings` intacta.
Há **6.448 acessos a `GlobalSettings.*`** espalhados pelo código, o que torna a fachada
inegociável: troca-se o *backing store*, não a superfície pública.

---

## 3. WinForms dentro do domínio — **o problema central**

**69 dos 245 arquivos de `Backend/`** têm `using System.Windows.Forms`. Inclui os tipos
mais centrais do domínio:

```
Backend/Characters/Character.cs        Backend/Equipment/Weapon.cs
Backend/Equipment/Gear.cs              Backend/Equipment/Cyberware.cs
Backend/Equipment/Armor.cs             Backend/Equipment/Vehicle.cs
Backend/Equipment/Drugs.cs             Backend/Equipment/Lifestyle.cs
Backend/Static/Managers/ImprovementManager.cs
Backend/Static/Managers/LanguageManager.cs
Backend/Static/CommonFunctions.cs      Backend/Static/Utils.cs
Backend/Static/GlobalSettings.cs       Backend/Uniques/Power.cs
Backend/Character Settings/CharacterSettings.cs
```

O acoplamento tem quatro formas distintas, com soluções diferentes:

### 3a. Caixas de mensagem — 329 chamadas

O domínio informa e pergunta ao usuário diretamente, via `Program.ShowMessageBox[Async]` e
`Program.ShowScrollableMessageBox[Async]`. Só `Character.cs` faz isso 24 vezes.

### 3b. Diálogos de seleção — ~120 instanciações

O domínio **constrói formulários**:

| Diálogo | Ocorrências no `Backend/` |
|---|---|
| `SelectItem` | 35 |
| `SelectNumber` | 21 |
| `SelectAttribute` | 10 |
| `SelectSkillGroup`, `SelectSkill` | 6 cada |
| `SelectSpellCategory`, `SelectMetamagic`, `SelectMentorSpirit`, `SelectBuildMethod`, `SelectAIProgram` | 4 cada |
| `SelectMetatypePriority`, `SelectMetatypeKarma` | 3 cada |
| `SelectWeaponCategory`, `SelectSpell`, `SelectPower`, `SelectOptionalPower`, `SelectLimit`, `SelectComplexForm`, `SelectArt`, `SelectArmorMod` | 2 cada |

Como explicado em [06 — Motor de Regras](06-motor-regras.md), **isto não é um defeito de
design a ser eliminado**: as regras de Shadowrun genuinamente exigem escolha do usuário
durante a aplicação de um bônus. O que muda é o mecanismo — de "construir um `Form`" para
"emitir uma solicitação que a camada de apresentação resolve".

### 3c. Tipos de UI em assinaturas — 25 arquivos

```csharp
public bool Load(string strFileName = "", LoadingBar frmLoadingForm = null, …)
```

`LoadingBar` (16 ocorrências no `Backend/`), `ThreadSafeForm<T>`, `CursorWait`. São
parâmetros de progresso e de cursor de espera atravessando a fronteira do domínio.

### 3d. Manipulação de `TreeNode` no domínio

`Character.cs` tem uma região inteira chamada "UI Methods / Move TreeNodes" (linha ~17.532).
Números: **344 referências a `TreeNode` dentro de `Backend/`**, das quais 78 estão em
`Character.cs`. (Em `Forms/` + `Controls/`, onde é legítimo, são 770.)

**Avaliação:** este é o trabalho estrutural principal do porte. 3a e 3b resolvem-se com uma
única abstração de interação; 3c com parâmetros de progresso abstratos
(`IProgress<T>`); 3d exige remover código de UI do domínio.

---

## 4. `System.Drawing`

46 arquivos de `Backend/` usam `System.Drawing`. A distribuição não é uniforme:

### Cores — concentradas

Das 1.748 ocorrências de `Color`, **560 estão em `ColorManager.cs`** — o gerenciador de
tema, que é conceitualmente UI e será reescrito de qualquer forma.

O restante está disperso em objetos de domínio que carregam cor de exibição (`Spell.cs` 37,
`ComplexForm.cs` 37, `Contact.cs` 29, `Quality.cs` 26). São propriedades de apresentação
morando no domínio.

### Imagens — bem abstraídas

`Image` aparece em: `ImageExtensions.cs` (58), `Spirit.cs` (15), `Contact.cs` (15),
`Character.cs` (14), `CharacterCache.cs` (10), `IHasMugshots.cs` (5).

Existe uma **interface `Backend/Interfaces/IHasMugshots.cs`** implementada por `Character`,
`Contact` e `Spirit`. Isso é uma boa notícia: os retratos já passam por um contrato, e as
operações de imagem estão concentradas em `ImageExtensions.cs`.

**Avaliação:** substituível por `SkiaSharp` ou `ImageSharp` com esforço moderado, porque os
pontos de contato são poucos e já mediados por interface e por extensões.

---

## 5. Modelo de execução

| Padrão | Ocorrências |
|---|---|
| `Application.DoEvents()` | 146 |
| `SafelyRunSynchronously` / `JoinableTaskFactory.Run` | 279 |
| `DoThreadSafe*` | 7.102 |

O padrão dominante é: trabalho pesado roda na UI thread, bombeando a fila de mensagens para
não congelar a janela. `Utils.EverDoEvents` governa quando isso é permitido.

No Android **não existe equivalente a `DoEvents`**, e bloquear a thread principal resulta em
ANR — o sistema encerra o processo.

**Avaliação:** junto com a extração do núcleo, é o trabalho estrutural mais pesado. A boa
notícia é que o código **já tem** o caminho assíncrono implementado em quase todos os
lugares (~2.960 métodos `async Task`, ~47.200 `ConfigureAwait`). O trabalho é
majoritariamente **remover o caminho síncrono**, não escrever o assíncrono.

---

## 6. Sistema de arquivos e caminhos

`Utils.GetStartupPath` resolve para `Application.StartupPath` (WinForms) ou
`AppDomain.CurrentDomain.SetupInformation.ApplicationBase` em testes. Tudo é construído a
partir dele: `data/`, `lang/`, `sheets/`, `customdata/`, `settings/`, `wkhtmltopdf.exe`.

O modelo assumido é: **executável e dados na mesma pasta, com escrita livre**. O Android
separa assets somente-leitura de armazenamento gravável, e restringe o acesso ao sistema de
arquivos.

---

## 7. Dependências não portáveis

Listadas em [12 — Build e CI](12-build-ci.md): `HtmlRenderer.WinForms`,
`LiveCharts.WinForms`, `HtmlRenderer.Core`, `Codaxy.WkHtmlToPdf`,
`System.ComponentModel.Composition` (MEF), `Microsoft.ApplicationInsights.PerfCounterCollector`,
`System.Data.DataSetExtensions`.

---

## 8. O que **não** é problema

Vale registrar explicitamente, porque contrabalança o resto:

| Item | Situação |
|---|---|
| `Chummer/7zip/` (LZMA para `.chum5lz`) | **zero `DllImport`** — 100% gerenciado, portável como está |
| `INotifyPropertyChanged` + grafo de dependências | completo e rigoroso; consumível diretamente por databinding moderno |
| Dados XML (`data/`, `customdata/`, `lang/`) | independentes de plataforma |
| Folhas XSLT (`sheets/`) | produzem HTML; renderizáveis em `WebView` (sujeito ao spike de `XslCompiledTransform`) |
| `Chummer.Tests` | roda contra o domínio; serve de critério de sucesso da extração |
| `ChummerHub` | já em net6.0, multiplataforma |
| Lógica de regras propriamente dita | é C# puro sobre XML — a maior parte das 350k linhas do `Backend/` não toca a plataforma |

---

## Ordem de ataque sugerida

Da relação esforço/risco medida acima:

1. **`NativeMethods.cs`** — alto volume, esforço trivial. Elimina 80 ocorrências apagando um arquivo e 8 chamadores.
2. **`GlobalSettings` / Registro** — substituição mecânica atrás de fachada estável.
3. **`System.Drawing`** — pontos de contato poucos e já mediados por `IHasMugshots` e `ImageExtensions`.
4. **Tipos de UI em assinaturas** (`LoadingBar`, `CursorWait`) — troca por abstrações de progresso.
5. **`MessageBox` + diálogos `Select*`** — a abstração de interação. O trabalho conceitual maior, mas mecânico depois de definida.
6. **`TreeNode` no domínio** — remoção de código de UI do `Character.cs`.
7. **Caminho síncrono e `DoEvents`** — o mais arriscado, porque mexe em semântica de concorrência de um sistema com locks caseiros.

Os itens 1–4 são de baixo risco e podem começar sem decisões de arquitetura pendentes. Os
itens 5–7 dependem do desenho da camada de plataforma.
