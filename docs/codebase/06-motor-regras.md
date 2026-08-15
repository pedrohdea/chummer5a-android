# 06 — Motor de Regras (Improvements)

Este é o subsistema mais importante e menos óbvio do Chummer. Se você entender só um
subsistema antes de mexer no código, que seja este.

## O conceito

Um **Improvement** é a unidade atômica de *"alguma coisa modifica alguma coisa"*.

Praticamente tudo em Shadowrun modifica outra coisa: uma qualidade dá +1 em um atributo,
um implante aumenta a Agilidade mas custa Essence, uma magia sustentada dá −2 em todos os
testes, uma arte marcial altera o dano desarmado, um espírito mentor concede bônus
condicionais. Em vez de codificar cada caso, o Chummer tem **um mecanismo genérico**:

```
XML de dados          →   nó <bonus>
                              │
                              ▼
                    AddImprovementCollection
                              │
                              ▼
              lista de objetos Improvement no Character
                              │
                              ▼
            ImprovementManager.ValueOf(tipo, …)  →  decimal
                              │
                              ▼
              propriedade calculada do Character
```

## As peças

| Arquivo | Linhas | Papel |
|---|---|---|
| `Backend/Improvements/Improvement.cs` | 5.590 | o objeto Improvement e seus enums |
| `Backend/Improvements/AddImprovementCollection.cs` | 7.219 | **319 métodos**, um por tipo de nó `<bonus>` |
| `Backend/Improvements/AddImprovementAsyncCollection.cs` | 7.663 | a mesma coisa, versão assíncrona |
| `Backend/Improvements/ImprovementMethods.cs` | 1.364 | tabela de despacho **gerada** |
| `Backend/Improvements/ImprovementMethods.tt` | 261 | o template T4 que gera a tabela |
| `Backend/Static/Managers/ImprovementManager.cs` | 7.346 | consulta, cache, ciclo de vida |

Total: ~29.200 linhas.

## Escala do vocabulário

- **310 valores** no enum `Improvement.ImprovementType` — de `Attribute` e `Armor` a
  `WeaponCategoryAccuracy`, `CyberwareTotalEssMultiplierNonRetroactive`,
  `LivingPersonaDeviceRating`.
- **41 valores** no enum `ImprovementSource` — de onde o improvement veio (Qualidade,
  Cyberware, Magia, Poder, Arte Marcial, Espírito Mentor…). É o que permite remover
  corretamente todos os efeitos de um item quando ele é deletado.
- **646 casos** na tabela de despacho gerada.

## Despacho: geração de código, não reflexão

Os métodos de bônus são nomeados **em minúsculas, iguais aos nomes dos nós XML**:

```csharp
public void qualitylevel(XmlNode bonusNode)
public void cyberlimbattributebonus(XmlNode bonusNode)
public void blockskilldefaulting(XmlNode bonusNode)
public void enableattribute(XmlNode bonusNode)
```

O despacho acontece em `ImprovementManager`:

```csharp
ImprovementMethods.GetMethod(bonusNode.Name.ToUpperInvariant(), container)
```

E `ImprovementMethods.GetMethod` é um `switch` gigante **gerado por T4** a partir dos
métodos existentes. O comentário no código gerado explica a escolha:

> *"Switch-cases get compiled as hashes, so this is as close as you can get to a
> compile-time Dictionary"*

Ou seja: o projeto conscientemente trocou reflexão por geração de código, por desempenho.

**Consequência para o porte:** este é um dos poucos pontos onde o build depende de
ferramentas do Visual Studio. O `.tt` importa `EnvDTE` e o `.csproj` tem
`<TransformOnBuild>True</TransformOnBuild>`. Fora do Visual Studio isso não roda — o
arquivo `.cs` gerado está versionado, então compila, mas regenerá-lo exige outro caminho.

## Consulta de valores

A API principal:

```csharp
ImprovementManager.ValueOf(Character, ImprovementType, …)          → decimal
ImprovementManager.AugmentedValueOf(Character, ImprovementType, …) → decimal
```

Mais as variantes `…Async` e `…TupleAsync` (que devolvem também a lista de improvements que
contribuíram, para exibir o detalhamento ao usuário em tooltips).

"Augmented" distingue valores base de valores já aumentados por outros modificadores — a
distinção existe porque Shadowrun tem regras diferentes para o que conta como bônus
natural e o que conta como aumento artificial.

## Cache

O `ImprovementManager` mantém cache por (personagem × tipo de improvement), com invalidação
explícita:

```csharp
ClearCachedValue(Character, ImprovementType, …)
ClearCachedValues(Character, …)
ClearAllCharacterValues(Character, …)
```

O cache é indexado por uma `ImprovementDictionaryKey` própria, com operadores de igualdade
customizados.

## Escolha do usuário no meio da regra

Muitos bônus exigem decisão humana: *"escolha uma perícia para receber +2"*. O mecanismo é
um trio de estados por personagem:

```csharp
GetForcedValue / SetForcedValue / ClearForcedValue        // valor imposto pelos dados
GetSelectedValue / SetSelectedValue / ClearSelectedValue  // o que o usuário escolheu
GetLimitSelection / SetLimitSelection / ClearLimitSelection // restrição da escolha
```

Quando o dado XML não impõe um valor, o método de bônus **abre um diálogo** (`SelectSkill`,
`SelectAttribute`, `SelectItem`…), grava o resultado em `SelectedValue`, e continua. O
valor escolhido é persistido no `.chum5` para que, ao recarregar, a mesma escolha seja
reaplicada sem perguntar de novo.

**Este é o ponto exato onde o domínio depende da UI, e é o padrão que o porte precisa
substituir por uma abstração de solicitação de escolha.** Não é possível remover a
pergunta — ela é regra de jogo. É possível mudar quem a responde.

## Condições

Improvements podem ser condicionais. O sistema de condições usa uma sintaxe tipo XPath,
com predicados em lista de permissão:

```xml
<condition>/character/created</condition>
<condition>/spell/range = "Touch"</condition>
<condition>not(/spell/alchemical)</condition>
<condition>/spell/alchemical = false and /spell/range = "Touch"</condition>
```

Documentação completa e já existente: **`Chummer/docs/XPathConditionSystem.md`** e
**`Chummer/docs/ComparisonOperators.md`**. O histórico recente do repositório mostra
trabalho ativo nessa área (expansão do sistema, correção de parênteses e dos operadores
`<` e `>`).

## Propagação

Alterar um improvement precisa fazer as propriedades derivadas recalcularem e notificarem a
UI. Isso é responsabilidade do grafo de dependências de propriedades — ver
[07 — Concorrência e Notificação](07-concorrencia.md).

## Referência de autoria

`Chummer/docs/wiki/Improvement-Manager.md` documenta os nós `<bonus>` do ponto de vista de
quem escreve dados. O XSD `Chummer/data/bonuses.xsd` é a gramática formal.
