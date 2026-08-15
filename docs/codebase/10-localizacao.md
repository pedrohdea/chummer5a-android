# 10 — Localização

O Chummer traduz duas coisas diferentes, por dois mecanismos diferentes: **a interface** e
**o conteúdo de jogo**. Entender essa separação é essencial — ela explica por que
`Chummer/lang/` tem 9,7 MB, mais que a pasta de dados.

## Idiomas suportados

| Código | Idioma |
|---|---|
| `en-us` | Inglês (EUA) — idioma base |
| `de-de` | Alemão |
| `fr-fr` | Francês |
| `ja-jp` | Japonês |
| `pt-br` | **Português (Brasil)** |
| `zh-cn` | Chinês simplificado |

Idiomas adicionais podem ser criados pela comunidade com a ferramenta `Translator/`.

## Camada 1 — Interface: `<idioma>.xml`

Arquivos como `lang/en-us.xml`, `lang/pt-br.xml`. Contêm as frases da interface —
**2.771 strings** no arquivo base em inglês.

Acesso pelo `LanguageManager` (`Backend/Static/Managers/LanguageManager.cs`, 2.966 linhas):

```csharp
LanguageManager.GetString(strTag, strLanguage, …)
LanguageManager.GetStringAsync(strTag, strLanguage, …, token)
```

Os identificadores seguem convenção por prefixo: `String_`, `Label_`, `Tip_`, `Message_`,
`Title_`, `Checkbox_`, `Button_`, `Menu_`, `Tab_`, `Node_`.

### Tradução automática de formulários

Existe um método de extensão que percorre a árvore de controles de um formulário e traduz
tudo de uma vez, usando a propriedade `Tag` de cada controle como chave:

```csharp
objForm.TranslateWinForm(strLanguage, …);
await objForm.TranslateWinFormAsync(strLanguage, …, token);
```

Isso significa que **os textos da UI não estão nos designers como literais** — estão como
tags resolvidas em runtime. É um design elegante que, no porte, não sobrevive
(`TranslateWinForm` opera sobre `Control` do WinForms), mas o **arquivo de strings sim**:
os 2.771 pares chave→texto migram diretamente para qualquer sistema de recursos.

## Camada 2 — Conteúdo de jogo: `<idioma>_data.xml`

Aqui está o volume. Arquivos como `lang/pt-br_data.xml` traduzem os **próprios dados de
jogo**: nomes de armaduras, categorias, magias, qualidades, implantes.

Estrutura real:

```xml
<chummer>
  <version>-500</version>
  <chummer file="armor.xml">
    <categories>
      <category translate="Armaduras">Armor</category>
      <category translate="Roupas">Clothing</category>
    </categories>
    <armors>
      <armor translated="True">
        <id>31c68476-6328-476a-ae8a-94f65d505a04</id>
        …
```

Pontos-chave do desenho:

- É organizado **por arquivo de dados** (`<chummer file="armor.xml">`), espelhando `data/`.
- A ligação é feita por **GUID** (`<id>`), não por nome. Renomear um item no idioma base não
  quebra as traduções.
- O atributo `translate="…"` carrega o texto traduzido; o texto do nó permanece em inglês.
- `translated="True"` marca o que já foi revisado.

Isso permite que o `XmlManager` entregue documentos já traduzidos: o idioma faz parte da
chave de cache dos documentos mesclados (ver [05](05-sistema-dados.md)).

## Tradução reversa

O sistema precisa mapear no sentido inverso — de um nome traduzido de volta ao nome
canônico em inglês — porque os dados internos e os saves usam sempre o nome base. Há
funções de *reverse translation* no `LanguageManager` para isso.

Esta área tem histórico de bugs sutis. Um commit recente no repositório corrige exatamente
isso: *"Fixed reverse-translation for skill specialisations incorrectly matching against
first instance of the string in language file rather than best-match against the parent
skill list"* — com testes dedicados em
`Chummer.Tests/SkillSpecializationTranslationTests.cs`.

## Ferramenta de tradução

`Translator/` é uma aplicação WinForms separada (net48, ~10.900 LOC) com solução própria
(`Translator.sln`). Serve para a comunidade criar e manter traduções sem editar XML à mão.
Não é parte do aplicativo distribuído ao usuário final.

## Cultura vs. idioma

São coisas separadas no Chummer. O idioma escolhe o arquivo de tradução; a **cultura**
(`CultureInfo`) controla formatação de números e datas. `PrintToXmlTextWriter` recebe os
dois independentemente:

```csharp
PrintToXmlTextWriter(XmlWriter objWriter, CultureInfo objCulture = null,
                     string strLanguageToPrint = …, CancellationToken token = default)
```

Um usuário pode querer a interface em português mas a ficha impressa em inglês, ou números
formatados na convenção local com conteúdo em inglês.

## Implicações para o porte

| Item | Situação |
|---|---|
| `lang/*.xml` (strings de UI) | **Reaproveitável** — migra para o sistema de recursos escolhido |
| `lang/*_data.xml` (conteúdo) | **Reaproveitável integralmente** — é consumido pelo `XmlManager`, não pela UI |
| `TranslateWinForm` | Descartável — específico do WinForms |
| Tradução reversa | Reaproveitável — é lógica pura no `LanguageManager` |
| Detecção de idioma do sistema | Precisa usar a API do Android em vez de `CultureInfo` do Windows |

Os 9,7 MB de `lang/` somam-se aos 7,2 MB de `data/` no problema de empacotamento de assets.
Uma decisão de produto razoável no Android é **empacotar apenas os idiomas necessários** e
baixar os demais sob demanda.
