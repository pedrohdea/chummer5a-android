# 12 — Build, CI e Dependências

## Como se compila hoje

| Item | Valor |
|---|---|
| Target framework do app | `net48` (.NET Framework 4.8) |
| Tipo de saída | `WinExe`, assembly `Chummer5` |
| SDK requerido (`global.json`) | .NET SDK `8.0.401`, `rollForward: latestFeature` |
| Estilo do projeto | SDK-style, mas alvo .NET Framework |
| Configurações | `Debug`, `Release`, `Debuggable Release` |
| Flags notáveis | `UseWindowsForms`, `UseWPF`, `AllowUnsafeBlocks`, `TransformOnBuild` |

O `.csproj` do projeto principal tem **mais de 25.700 linhas** — quase tudo é listagem
explícita de arquivos de dados, recursos e páginas de tradução.

### Requisito de plataforma

O build **exige Windows**. Motivos:

1. `net48` + WinForms/WPF só compila com as reference assemblies do .NET Framework.
2. Há `HintPath` apontando para `$(MSBuildProgramFiles32)\Reference Assemblies\…\v4.8\`.
3. `<TransformOnBuild>True</TransformOnBuild>` executa o template T4
   `ImprovementMethods.tt`, que importa `EnvDTE` — API do Visual Studio.

O arquivo `.cs` gerado pelo T4 está versionado, então o build funciona sem regenerá-lo; mas
alterar o template exige Visual Studio.

### Neste container

Verificado: **não há .NET SDK nem Android SDK instalados** (apenas Java). Qualquer trabalho
de compilação exige instalar a toolchain primeiro, e como o container é efêmero, isso
precisa ser um script de setup versionado no repositório.

## Dependências NuGet do projeto principal

### Portáveis (funcionam em .NET moderno / Android)

| Pacote | Uso |
|---|---|
| `Newtonsoft.Json` 13.0.4 | exportação JSON |
| `NLog` 6.1.4 + `NLog.Schema` | logging |
| `Microsoft.Extensions.ObjectPool` | pooling |
| `Microsoft.IO.RecyclableMemoryStream` | pooling de streams |
| `XoshiroPRNG.Net` | gerador de aleatórios |
| `Ben.Demystifier` | stack traces legíveis |
| `itext` 9.7.0 | leitura de PDF dos livros |
| `Microsoft.CodeAnalysis.*` | analisadores (build-time) |

### Não portáveis — exigem substituição

| Pacote | Problema |
|---|---|
| `HtmlRenderer.WinForms` | WinForms |
| `LiveCharts.WinForms` | WinForms — usado no `ExpenseChart` |
| `HtmlRenderer.Core` | acoplado a `System.Drawing` |
| `Codaxy.WkHtmlToPdf` | invoca `wkhtmltopdf.exe`, binário nativo Windows |
| `System.ComponentModel.Composition` | MEF — carregamento dinâmico de assemblies |
| `Microsoft.VisualStudio.Threading` | `JoinableTaskFactory`; funciona fora do VS, mas o padrão de uso (execução síncrona de async) é o que precisa sair |
| `Microsoft.ApplicationInsights.*` | telemetria; `PerfCounterCollector` é específico do Windows |
| `System.Data.DataSetExtensions` | legado do .NET Framework |

## Pipelines de CI

### AppVeyor — o build oficial

`appveyor.yml`. Imagem **Visual Studio 2022**, configuração `Release`. Etapas:
`nuget restore` → `build` → `after_build` → `test_script` → `artifacts` → `deploy`.

Versionamento: `5.226.{build}`, propagado para assembly version, file version e
informational version.

Este é o pipeline que produz as releases Nightly do projeto upstream.

### GitHub Actions — 4 workflows

| Workflow | Runner | Papel |
|---|---|---|
| `nightly-build.yml` | `windows-latest` | build noturno (cron `0 0 * * *`), cria release, timeout 60 min |
| `codeql.yml` | — | análise de segurança semanal |
| `main.yml` | `ubuntu-latest` | notificação de release |
| `publish-wiki.yml` | `ubuntu-latest` | publica `Chummer/docs/wiki/` como wiki do GitHub |

O `nightly-build.yml` usa `actions/setup-dotnet@v4` com `dotnet-version: 8.0.x` — o SDK 8
compilando projetos SDK-style que têm alvo .NET Framework 4.8, exatamente como descrito no
comentário do próprio workflow.

## Distribuição atual

Arquivo ZIP extraído pelo usuário, com `Chummer5.exe` na raiz junto às pastas `data/`,
`lang/`, `sheets/`, `customdata/`, `settings/` e `wkhtmltopdf.exe`. Dois canais: Milestone
(estável) e Nightly (diário).

Há um atualizador embutido: `Forms/Utility Forms/ChummerUpdater.cs`.

`Program.cs` contém `UnblockPath` / `UnblockFile` — remove a marca de "arquivo baixado da
internet" (*Zone.Identifier*) que o Windows aplica a arquivos extraídos de ZIP.

## Assinatura

O `.csproj` referencia `ManifestCertificateThumbprint` e
`ManifestKeyFile = Chummer_TemporaryKey.pfx`, mas `GenerateManifests` e `SignManifests`
estão ambos em `false` — a infraestrutura de ClickOnce está presente porém desligada.

## O que muda no porte

| Hoje | Android |
|---|---|
| `net48` | `net9.0-android` |
| Build exige Windows | build em qualquer plataforma |
| AppVeyor + Visual Studio 2022 | GitHub Actions com workload Android |
| Artefato: ZIP com `.exe` | artefato: `.aab` / `.apk` |
| Assinatura ClickOnce (desligada) | keystore Android (obrigatório) |
| Atualizador próprio | loja de aplicativos ou distribuição direta |
| T4 com `EnvDTE` | precisa de gerador independente do Visual Studio, ou manter o `.cs` versionado |

O `docs/index.html` na raiz do repositório é um site GitHub Pages legado, sem relação com o
build.
