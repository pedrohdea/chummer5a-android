# 11 — Projetos Satélite

O repositório contém oito projetos além do aplicativo principal. Nenhum é essencial ao
funcionamento básico do Chummer, mas todos aparecem na solução e no build.

| Projeto | Framework | Arquivos | LOC | Papel |
|---|---|---|---|---|
| `ChummerHub` | **net6.0** | 153 | 31.462 | serviço web de compartilhamento |
| `Plugins/ChummerHub.Client` | net48 | 138 | 24.675 | plugin cliente do Hub |
| `Translator` | net48 | 7 | 10.911 | ferramenta de tradução |
| `ChummerDataViewer` | net48 | 14 | 2.186 | visualizador de relatórios de falha |
| `CrashHandler` | net48 | 7 | 1.469 | captura de falhas |
| `TextblockConverter` | net48 | 4 | 481 | utilitário de conversão de texto |
| `Chummer.Benchmarks` | net48 | 1 | 189 | benchmarks |
| `Plugins/SamplePlugin` | net48 | 4 | 377 | exemplo de plugin |

Note que **`ChummerHub` é o único projeto do repositório já em .NET moderno** (net6.0) e,
portanto, o único que já é multiplataforma. Tem `Dockerfile` próprio.

---

## Sistema de plugins

### Mecanismo

MEF (`System.ComponentModel.Composition`). `Chummer/Plugins/PluginControl.cs` (741 linhas):

- `CompositionContainer` + `AggregateCatalog` + `DirectoryCatalog`
- Um `FileSystemWatcher` monitora a pasta de plugins
- Plugins são DLLs carregadas dinamicamente em runtime
- `RegisterChummerProtocol()` registra um handler de protocolo `chummer://` no sistema

### A interface `IPlugin`

```csharp
public interface IPlugin : IDisposable
{
    void CustomInitialize(ChummerMainForm mainControl);
    Task<ICollection<TabPage>> GetTabPages(CharacterCareer input, …);
    Task<ICollection<TabPage>> GetTabPages(CharacterCreate input, …);
    Task<ICollection<ToolStripMenuItem>> GetMenuItems(ToolStripMenuItem menu, …);
    ITelemetry SetTelemetryInitialize(ITelemetry telemetry);
    bool ProcessCommandLine(string parameter);
    Task<ICollection<TreeNode>> GetCharacterRosterTreeNode(CharacterRoster frmCharRoster, …);
    UserControl GetOptionsControl();
    string GetSaveToFileElement(Character input);
    void LoadFileElement(Character input, string fileElement);
    void SetIsUnitTest(bool isUnitTest);
    Assembly GetPluginAssembly();
    bool SetCharacterRosterNode(TreeNode objNode);
    Task<bool> DoCharacterList_DragDrop(…, TreeView treCharacterList, …);
}
```

**A interface inteira é definida em termos de WinForms**: `TabPage`, `ToolStripMenuItem`,
`TreeNode`, `UserControl`, `DragEventArgs`, `TreeView`, `ChummerMainForm`. Um plugin não
estende o domínio — ele injeta controles WinForms na UI.

Apenas dois pontos são independentes de UI: `GetSaveToFileElement` / `LoadFileElement`, que
permitem ao plugin persistir dados próprios dentro do `.chum5` do personagem.

### Consequência para o porte

O sistema de plugins **não é portável na forma atual**. Além do acoplamento total à UI,
carregamento dinâmico de assemblies é restrito no Android. Se a funcionalidade for
desejada, precisa ser redesenhada do zero com outra interface.

Para o MVP, plugins ficam fora — o que também elimina a dependência de MEF
(`System.ComponentModel.Composition`).

---

## ChummerHub — o serviço web

ASP.NET Core em **net6.0**, com Entity Framework (pasta `Migrations/`), controllers, API e
`Dockerfile`. Permite ao usuário publicar personagens online e compartilhá-los com o grupo.

O acesso pelo aplicativo é feito pelo plugin `ChummerHub.Client` (24.675 LOC), que inclui um
cliente OIDC próprio em `OidcClient/` (dois subprojetos: `OidcClient` e
`IdentityTokenValidator`).

`Character.cs` tem o atributo `[HubTag]` marcando quais propriedades são publicadas ao Hub
— por exemplo, `Character.Created`. `Backend/Helpers/HubTagAttribute.cs` define o atributo.

Para o porte: o Hub é um **serviço**, não código de cliente. Ele continua funcionando como
está. O que precisaria ser reescrito é o cliente, e isso é opcional e pós-MVP.

---

## CrashHandler e ChummerDataViewer

`CrashHandler` (net48) é um processo separado que captura falhas do Chummer e monta o
relatório. Usa `dbghelp.dll` via P/Invoke para gerar minidumps —
`Backend/Debugging/CrashHandler.cs` no projeto principal faz a ponte.

`ChummerDataViewer` (net48) consome os relatórios coletados (há integração com AWS/S3 nas
dependências) para que os mantenedores analisem as falhas em volume.

Ambos são específicos do Windows e não têm papel no Android — a plataforma tem seu próprio
mecanismo de relatório de falhas.

Existe também telemetria em produção: **Application Insights**
(`Microsoft.ApplicationInsights.NLogTarget`, `…PerfCounterCollector`), com
`Program.ChummerTelemetryClient` e um `ExceptionHeatMap` em `Program.cs`. Qualquer porte
precisa decidir explicitamente o que fazer com essa coleta — inclusive por questões de
política de loja de aplicativos.

---

## Translator

Aplicação WinForms independente, com `Translator.sln` próprio. Edita os arquivos de
`lang/` graficamente. Ferramenta de colaborador, não de usuário final. Ver
[10 — Localização](10-localizacao.md).

---

## TextblockConverter e Chummer.Benchmarks

Utilitários pequenos de desenvolvimento. `Chummer.Benchmarks` tem um único arquivo de 189
linhas.

---

## Chummer.Tests

Não é satélite — é a rede de segurança do porte, e merece destaque:

| Arquivo | Linhas | Cobre |
|---|---|---|
| `ChummerTest.cs` | 832 | carga de personagens, dados, ciclo geral |
| `XmlNodeExtensionsTests.cs` | 443 | extensões de XML |
| `OrderInvariantHashCodeTests.cs` | 138 | hashing independente de ordem |
| `SkillSpecializationTranslationTests.cs` | 58 | tradução reversa de especializações |
| `SpiritBoundLimitTests.cs` | 52 | limite de espíritos vinculados |
| `ValueVersionTests.cs` | 51 | comparação de versões |

Há uma pasta `TestFiles/` com personagens de exemplo, e o projeto principal expõe
`InternalsVisibleTo("Chummer.Tests")`.

**Estes testes são o critério objetivo de sucesso da extração do núcleo**: se eles passarem
contra o `Chummer.Core` em .NET moderno, a extração preservou o comportamento. É pouca
cobertura para 625k linhas, mas é o que existe, e cobre justamente o caminho crítico de
carregar personagens.
