# 09 — Fichas, Impressão e Exportação

Três saídas distintas, todas construídas sobre o mesmo mecanismo: o personagem é
serializado em XML e transformado por XSLT.

## O pipeline

```
Character
    │
    ▼  PrintToXmlTextWriter(…)        ← projeção de impressão, não o save
XML do personagem (com cultura e idioma aplicados)
    │
    ▼  XslCompiledTransform
HTML
    │
    ├──► WebBrowser (visualização na tela)
    ├──► arquivo HTML
    └──► PDF (wkhtmltopdf.exe)
```

Ponto importante: `PrintToXmlTextWriter` **não** produz o mesmo XML do `.chum5`. É uma
projeção separada, já formatada para exibição — valores calculados resolvidos, números
formatados na cultura escolhida, textos traduzidos no idioma escolhido. Inclui
`PrintMugshots` para embutir os retratos.

## As folhas de estilo

### `Chummer/sheets/` — 1,6 MB

Duas extensões, com significados diferentes:

| Extensão | Papel |
|---|---|
| `*.xsl` | folha completa, aparece na lista para o usuário |
| `*.xslt` | template parcial, **oculto** — só serve como referência incluída por outras |

O comentário no código é explícito sobre isso:

> *"Do not include files that end in .xslt since they are used as 'hidden' reference sheets
> (hidden because they are partial templates that cannot be used on their own)."*

Layouts disponíveis incluem `Fancy Blocks`, `Fancy Fours`, `Formatted Text-Only`,
`Game Master Summary`, `Dossier`, `Calendar`, `Commlinks`, `Contacts`, `Expenses`, `Notes`
— cada um com um `.xslt` parceiro.

Há também subpastas por idioma, já que as folhas podem ser traduzidas.

### `Chummer/export/`

Contém `Squad Manager.xsl` — folhas de **exportação de dados**, não de impressão. Alimentam
a tela `ExportCharacter`.

## Visualização — `CharacterSheetViewer`

`Forms/Utility Forms/CharacterSheetViewer.cs`. Fluxo:

1. Monta o caminho: `Path.Combine(Utils.GetStartupPath, "sheets", _strSelectedSheet + ".xsl")`
2. Obtém a transformação via `XslManager.GetTransformForFileAsync(…)` (com cache)
3. Aplica em `_objCharacterXml`
4. Escreve o HTML no `WebBrowser`

O controle `WebBrowser` do WinForms é o Internet Explorer embarcado. Para que ele renderize
com um motor moderno, `Utils.SetupWebBrowserRegistryKeys(int intEmulatedBrowserVersion)`
grava chaves de emulação de versão do IE no Registro — mais um ponto de dependência do
Windows.

Há uma acomodação explícita para Wine no código:

> *"The DocumentStream method fails when using Wine, so we'll instead dump everything out a
> temporary HTML file, have the WebBrowser load that, then delete the temporary file."*

## Exportação — `ExportCharacter`

Dois formatos:

- **XSLT** → qualquer formato textual definido por folha em `export/`
- **JSON** → via `Newtonsoft.Json` (`ExportJson` / `GenerateJson`)

## PDF

Dois usos completamente diferentes de PDF no projeto, que não devem ser confundidos:

### 1. Gerar PDF da ficha — `Codaxy.WkHtmlToPdf`

```csharp
new PdfConvertEnvironment(Path.Combine(Utils.GetStartupPath, "wkhtmltopdf.exe"))
```

Depende de um **executável nativo do Windows** distribuído junto ao aplicativo. Não tem
caminho no Android; precisa ser substituído pela API de impressão da plataforma.

### 2. Abrir o livro-fonte no PDF do usuário — `iText`

`CommonFunctions.OpenPdf(…)` e `OpenPdfFromControl(…)`. Quando o usuário clica na referência
de um item ("Core, p. 437"), o Chummer abre o PDF do livro **que o usuário possui** na
página certa, usando o leitor configurado em `GlobalSettings.PdfAppPath`.

O `iText` também é usado em `EditGlobalSettings` para extrair texto das páginas
(`GetTextFromPage`) e calibrar o deslocamento de numeração de cada livro — porque a
numeração impressa raramente bate com a numeração do arquivo.

Esta funcionalidade é muito usada na prática e depende de: PDFs locais do usuário, um leitor
externo de PDF, e passagem de parâmetros de linha de comando para ele. No Android, os dois
últimos não existem nessa forma.

## Impressão em lote

`PrintMultipleCharacters` gera fichas de vários personagens de uma vez — usado por mestres.

## Implicações para o porte

**Boa notícia:** as 20+ folhas XSLT são reaproveitáveis integralmente. Elas produzem HTML,
e uma `WebView` Android renderiza HTML. É o caminho mais curto para uma funcionalidade de
alto valor no MVP.

**Risco a validar (spike da Etapa 1):** `XslCompiledTransform` historicamente depende de
`Reflection.Emit`. O runtime Mono do Android tem JIT, então há boa expectativa de que
funcione — mas isso precisa ser **testado em dispositivo**, não presumido. Se não funcionar,
as alternativas são um processador XSLT 1.0 gerenciado ou pré-transformar no build, e ambas
mudam significativamente o custo desta parte.

**Substituições necessárias:**

| Hoje | No Android |
|---|---|
| `WebBrowser` (IE) + chaves de Registro | `WebView` |
| `wkhtmltopdf.exe` | API de impressão/PDF do Android |
| Abrir PDF do livro em app externo com parâmetros | Intent de visualização — sem controle fino de página |
| `Utils.GetStartupPath` + `sheets/` | assets empacotados |
