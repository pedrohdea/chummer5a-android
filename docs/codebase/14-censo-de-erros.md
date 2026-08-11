# 14 — Censo de Erros (Etapa 1.2/1.3)

Resultado de arrastar todo o `Chummer/Backend/` legado para dentro de um projeto `net9.0` e
deixar quebrar. O objetivo **não** era fazer compilar — era medir o acoplamento à plataforma
com precisão, para dimensionar a Etapa 2.

Reproduzível: `./scripts/censo-erros.sh`

---

## Resultado

**733 erros distintos** em 74 arquivos.

| Código | Ocorrências | O que é |
|---|---|---|
| `CS0246` | 557 | tipo ou namespace não encontrado |
| `CS0234` | 96 | namespace não existe no namespace pai |
| `CS1069` | 78 | tipo não encontrado no framework alvo |
| `CS0103` | 2 | nome não existe no contexto |

## O número que importa

Agrupando por **destino** de cada arquivo, e não por código de erro:

| Categoria | Erros | Arquivos | Destino |
|---|---|---|---|
| **Infraestrutura de UI** | 248 | 12 | **não vai para o `Core`** — sobe para a camada de UI ou é apagada |
| **Telemetria** | 34 | 6 | **removida** por PREM-005 |
| **Domínio** | **451** | **56** | **precisa ser resolvido de verdade** |

Ou seja: **38% dos erros somem sem trabalho de porte**, apenas por não moverem para o
núcleo. O trabalho real da Etapa 2 são **451 erros em 56 arquivos**.

### Infraestrutura de UI que não sobe para o núcleo

`WinFormsExtensions.cs` (99) · `CenterableMessageBox.cs` (40) · `ColorManager.cs` (22) ·
`DispatcherExtensions.cs` (17) · `ThreadSafeForm.cs` (16) · `ListViewItemWithValue.cs` (14) ·
`TooltipFactory.cs` (14) · `CursorWait.cs` (9) · `HoverDisplayCoordinator.cs` (7) e outros.

Estes arquivos moram em `Backend/` por convenção histórica, mas são código de apresentação.
`WinFormsExtensions.cs` sozinho, o maior emissor de erros do censo inteiro, é a ponte
`DoThreadSafe` entre o modelo assíncrono e a afinidade de thread do WinForms — conceito que
não existe no destino.

### Domínio — os 15 maiores

| Arquivo | Erros |
|---|---|
| `Backend/Characters/Character.cs` | 63 |
| `Backend/Interfaces/IHasInternalId.cs` | 35 |
| `Backend/Static/Extensions/ImageExtensions.cs` | 33 |
| `Backend/Interfaces/IHasMatrixAttributes.cs` | 19 |
| `Backend/Static/Managers/LanguageManager.cs` | 18 |
| `Backend/Equipment/VehicleMod.cs` | 18 |
| `Backend/Equipment/Gear.cs` | 18 |
| `Backend/Equipment/Weapon.cs` | 17 |
| `Backend/Equipment/Vehicle.cs` | 14 |
| `Backend/Static/Utils.cs` | 13 |
| `Backend/Equipment/Cyberware.cs` | 12 |
| `Backend/Characters/CharacterCache.cs` | 12 |
| `Backend/Equipment/WeaponMount.cs` | 11 |
| `Backend/Datastructures/ListItem.cs` | 9 |
| `Backend/Equipment/Armor.cs` | 7 |

## Símbolos ausentes mais citados

| Símbolo | Ocorrências | Origem |
|---|---|---|
| `Control` | 115 | WinForms |
| `TreeNode` | 98 | WinForms |
| `ContextMenuStrip` | 98 | WinForms |
| `Forms` (namespace) | 74 | WinForms |
| `Image` | 45 | System.Drawing |
| `TreeView` | 31 | WinForms |
| `DialogResult` | 28 | WinForms |
| `ElasticComboBox` | 26 | controle próprio do Chummer |
| `IWin32Window` | 16 | WinForms |
| `Form` | 16 | WinForms |
| `DispatcherObject` | 16 | WPF |
| `ApplicationInsights` | 16 | telemetria |
| `Bitmap` | 12 | System.Drawing |
| `MessageBoxButtons` / `MessageBoxIcon` | 14 | WinForms |
| `ListViewItem` / `ListViewGroup` | 12 | WinForms |
| `ToolStripItem` | 7 | WinForms |
| `Icon` | 7 | System.Drawing |
| `ComboBox` / `ListBox` | 12 | WinForms |

Três agrupamentos dominam: **WinForms** (~500), **System.Drawing** (~64) e **telemetria/WPF**
(~38).

## Leituras que o censo permite

**O acoplamento é raso e concentrado, não difuso.** 733 erros sobre ~350.000 linhas é uma
densidade baixíssima. A maior parte do `Backend/` — as regras de Shadowrun propriamente
ditas — é C# puro sobre XML e atravessa para net9.0 sem tocar em nada.

**`ContextMenuStrip` com 98 ocorrências foi a surpresa.** Menus de contexto atravessam o
domínio quase tanto quanto `TreeNode`. Junto com `ElasticComboBox` (26), confirma que
objetos de domínio carregam referências a controles concretos de UI, e não apenas a tipos
utilitários como `Color`.

**`IHasInternalId.cs` com 35 erros é o alvo mais barato de todos.** É uma *interface* — os
erros são assinaturas que expõem tipos de UI no contrato. Consertar um arquivo pequeno
provavelmente derruba erros em cascata em todos os implementadores.

**`Character.cs` com 63 erros é menos assustador que suas 56.290 linhas sugerem.** Um erro a
cada ~900 linhas.

## Ordem de ataque revisada

O censo confirma a ordem de DEC-006 e a torna mais específica:

1. **Não mover** os 12 arquivos de infraestrutura de UI (−248 erros, custo zero)
2. **Descartar** a telemetria (−34 erros, já decidido em PREM-005)
3. **`IHasInternalId` e demais interfaces** — limpar os contratos primeiro, pelo efeito cascata
4. **`ImageExtensions` + `IHasMugshots`** — a fronteira de imagem, já mediada por interface
5. **`ListItem` e `SourceString`** — tipos de apoio que carregam apresentação
6. **Equipamento** (`Gear`, `Weapon`, `Vehicle`, `VehicleMod`, `Cyberware`, `Armor`, `WeaponMount`) — ~97 erros, padrão repetido
7. **`Character.cs`** por último, quando o resto já compilar

## Ressalvas honestas

- O censo mede **acoplamento de plataforma**, não esforço total. Erros de compilação não
  capturam a reescrita do modelo assíncrono (146 `DoEvents`, 279 execuções síncronas), que
  compila perfeitamente e mesmo assim precisa mudar.
- A sondagem inclui os pacotes NuGet **já portáveis**. Os não portáveis foram deixados de
  fora de propósito, porque os erros que eles geram são sinal, não ruído.
- Duas correções foram necessárias na própria ferramenta antes de o número ser confiável: o
  MSBuild emite cada erro duas vezes (com e sem prefixo de nó `1>`), e `Annotations.cs` vive
  fora de `Backend/`. Sem essas correções, o censo reportava **6.196** erros — 88% ruído.
  Fica o registro de que o primeiro número que uma ferramenta de medição produz merece
  desconfiança.
