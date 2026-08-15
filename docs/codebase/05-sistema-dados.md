# 05 — Sistema de Dados XML

O conteúdo de jogo não está no código. Está em XML, carregado, mesclado e cacheado em
runtime pelo `XmlManager` (`Backend/Static/Managers/XmlManager.cs`, 3.392 linhas).

## Os arquivos

### `Chummer/data/` — 7,2 MB, 42 arquivos XML + XSD

Os maiores, por tamanho:

| Arquivo | Tamanho | Conteúdo |
|---|---|---|
| `critters.xml` | 940 KB | criaturas e NPCs |
| `metatypes.xml` | 780 KB | metatipos e metavariantes |
| `gear.xml` | 640 KB | equipamento genérico |
| `qualities.xml` | 572 KB | vantagens e desvantagens |
| `weapons.xml` | 528 KB | armas |
| `vehicles.xml` | 408 KB | veículos e drones |
| `lifemodules.xml` | 396 KB | módulos de vida |
| `settings.xml` | 320 KB | rulesets prontos |
| `cyberware.xml` | 260 KB | implantes + grades |
| `priorities.xml` | 152 KB | tabela de prioridades |
| `skills.xml` | 148 KB | perícias e grupos |
| `references.xml` | 148 KB | referências bibliográficas |
| `spells.xml` | 144 KB | magias |
| `actions.xml`, `armor.xml` | 128 KB cada | ações, armaduras |

Mais: `bioware`, `books`, `complexforms`, `contacts`, `critterpowers`, `mentors`,
`traditions`, `packs`, `lifestyles`, `martialarts`, `echoes`, `powers`, `programs`,
`ranges`, `sheets`, `streams`, `vessels`, `weaponmounts` e outros.

Junto vêm os **XSD** (`armor.xsd`, `character.xsd`, `bonuses.xsd`, `conditions.xsd`,
`SchemaExtensions.xsd`…) que definem a gramática de cada arquivo. Eles são a especificação
formal do formato de dados e são a melhor referência para entender a estrutura de qualquer
item.

### `Chummer/customdata/` — 2,2 MB, 57 pacotes

Cada pasta é um pacote de modificação de regras que o usuário liga ou desliga. Exemplos
reais: `Bone Lacing Adds to Body`, `Critter Prices`, `College Education Qualities Stack`,
`Adapsin Applied as Multiplier, Not Grade`, `Exclude German sourcebooks`,
`Delnar Skills Remake`, `Fixed Nitama Sporter Stats`, `Chrome Flesh Stealth Errata`.

Isso mostra o propósito do sistema: **erratas, house rules e correções da comunidade sem
tocar no código nem nos dados base**.

## Como a mesclagem funciona

Os arquivos de custom data são reconhecidos por **prefixo no nome**, e processados em
ordem fixa:

| Prefixo | Operação | Ordem |
|---|---|---|
| `override_*.xml` | substitui nós existentes | 1º |
| `custom_*.xml` | acrescenta nós novos | 2º |
| `amend_*.xml` | altera cirurgicamente nós existentes | 3º |

O `amend` é o mais sofisticado. `AmendNodeChildren()` (a partir da linha 2199) percorre a
árvore e decide a operação:

- **`recurse`** — se o nó de amendment tem filhos, desce e aplica neles
- **`append`** — se não tem filhos e não há alvo, acrescenta
- **`replace`** — se há alvo, substitui (acrescentando se não encontrar)
- **`amendoperation="remove"`** — apaga o nó original por completo

Isso permite, por exemplo, mudar só o custo de uma arma específica sem copiar o arquivo
inteiro.

## Cache e chave de cache

```csharp
private static readonly ConcurrentDictionary<KeyArray<string>, XmlReference> s_DicXmlDocuments
```

A chave é **idioma + o array completo de caminhos de todos os arquivos da combinação de
dados usada**. Ou seja: cada combinação distinta de (idioma × conjunto de custom data
habilitado) produz um documento mesclado próprio, cacheado separadamente.

Consequência prática: personagens com rulesets diferentes abertos ao mesmo tempo mantêm
árvores de dados independentes em memória. Isso é correto e é caro.

## As duas formas de acesso

```csharp
XmlManager.Load(…)       → XmlDocument      // DOM completo, mutável
XmlManager.LoadXPath(…)  → XPathNavigator   // somente leitura, mais leve
```

O código prefere `LoadXPath` na maioria das consultas. Ambos têm variante `…Async`.
Assinatura típica:

```csharp
LoadXPath(string strFileName,
          IReadOnlyCollection<string> lstEnabledCustomDataPaths = null,
          string strLanguage = "",
          bool blnLoadFile = false,
          CancellationToken token = default)
```

`Character` tem açúcar sintático que preenche `lstEnabledCustomDataPaths` a partir do
ruleset ativo do personagem, para que o chamador não precise passar.

## Validação

`XmlManager.Verify(string strLanguage, ICollection<string> lstBooks, …)` valida os dados
contra os XSD. Existe uma tela dedicada, `Forms/Utility Forms/TestDataEntries.cs`, para
rodar essa verificação — é ferramenta de desenvolvimento/autoria, não de uso final.

## Reconstrução do índice

`RebuildDataDirectoryInfo(…)` reconstrói o mapa de diretórios de dados quando o usuário
adiciona ou remove custom data. Ele **limpa todo o cache** (`s_DicXmlDocuments.Clear()`),
forçando recarga.

## Descoberta de folhas de ficha

O `XmlManager` também localiza as folhas XSLT de impressão:
`AnyXslFiles(…)` e `GetXslFilesFromLocalDirectory(…)`. Ver
[09 — Fichas e Exportação](09-fichas-exportacao.md).

## Referência de autoria

A documentação de **como escrever** esses arquivos já existe e é boa — está em
`Chummer/docs/wiki/`: `Custom-Data-Files.md`, `Game-Content-Documentation.md`, e um arquivo
por tipo de item (`Armor.md`, `Gear.md`, `Weapons.md`, `Cyberware.md`, `Spells.md`,
`Metatypes.md`, `Life-Modules.md`, …). Esta documentação não a duplica.

## Implicações para o porte Android

Três pontos que a Etapa 1 precisa medir:

1. **~21 MB de XML** precisam ser empacotados como assets e ficar acessíveis por caminho.
   O código assume caminhos de sistema de arquivos ao lado do executável.
2. **Custo de parse.** Toda a mesclagem acontece em runtime, a cada combinação nova. Num
   desktop isso já leva segundos. Precisa ser medido em hardware móvel — se for proibitivo,
   um cache pré-parseado e serializado passa a ser requisito, não otimização.
3. **Custom data do usuário** precisa de um local gravável no Android, o que envolve o
   modelo de armazenamento com escopo (Scoped Storage / SAF).
