# 07 — Concorrência e Notificação de Mudança

Duas mecânicas transversais que atravessam toda a base: como o código lida com paralelismo
e como uma alteração se propaga até a tela.

---

## Parte 1 — Concorrência

### O tamanho do compromisso com async

| Métrica | Valor |
|---|---|
| Chamadas `ConfigureAwait` | ~47.200 |
| Métodos `public async Task` | ~2.960 |
| Métodos com flag `bool blnSync` | 168 |
| `Application.DoEvents()` | 146 |
| `SafelyRunSynchronously` / `JoinableTaskFactory.Run` | 279 |

Praticamente **toda** operação do domínio existe em duas versões: síncrona e assíncrona.

### O padrão `blnSync`

Em vez de escrever o mesmo algoritmo duas vezes, muitos métodos usam um corpo único
parametrizado:

```csharp
private static async Task<Character> LoadCharacterCoreAsync(bool blnSync, string strFileName, …)
```

Em cada ponto de espera, o método ramifica: se `blnSync`, chama a versão bloqueante; senão,
faz `await`. Os wrappers públicos `Load(…)` e `LoadAsync(…)` apenas passam `true`/`false`.

Onde esse padrão não foi aplicado, a duplicação é literal — `AddImprovementCollection.cs`
(7.219 linhas) e `AddImprovementAsyncCollection.cs` (7.663 linhas) são a mesma lógica
escrita duas vezes.

### Biblioteca de sincronização própria

O projeto não usa apenas as primitivas do .NET. Ele implementa as suas:

| Arquivo | Linhas | Papel |
|---|---|---|
| `Helpers/AsyncFriendlyReaderWriterLock.cs` | 1.716 | lock leitor/escritor que funciona com `await` |
| `Helpers/LinkedAsyncRWLockHelper.cs` | 1.557 | hierarquia de locks encadeados (pai/filho) |
| `Helpers/DebuggableSemaphoreSlim.cs` | 465 | semáforo com rastreamento de quem detém |
| `Helpers/AsyncLock.cs` | 266 | lock exclusivo assíncrono |
| `Helpers/CancellationTokenTaskSource.cs` | — | ponte entre token e `Task` |

Objetos de domínio expõem `LockObject` e implementam `IHasLockObject`. O padrão de uso é:

```csharp
using (LockObject.EnterReadLock())
    return _blnCreated;
```

O encadeamento pai/filho existe porque a hierarquia do domínio é profunda: um `Gear` dentro
de um `Weapon` dentro de um `Vehicle` de um `Character`. Travar o filho precisa coordenar
com o pai sem deadlock.

### Coleções thread-safe próprias

`Backend/Datastructures/` traz 37 tipos, a maioria coleções:

```
ThreadSafeObservableCollection   ThreadSafeList        ThreadSafeBindingList
ThreadSafeQueue                  ThreadSafeStack       ThreadSafeRandom
ThreadSafeCachedRandom           ThreadSafeObservableCollectionWithMaxSize
LockingDictionary                LockingHashSet        LockingOrderedSet
LockingTypedOrderedDictionary    LockingEnumerator     LockingDictionaryEnumerator
ConcurrentHashSet                ConcurrentStringHashSet
CachedBindingList                EnhancedObservableCollection
TaggedObservableCollection       ObservableCollectionWithMaxSize
TypedOrderedDictionary           OrderedSet            KeyArray
MostRecentlyUsedCollection
```

`TaggedObservableCollection` é notável: permite associar a coleção ao objeto dono, para que
eventos de mudança saibam de onde vieram.

### Pooling de memória

O código é agressivo em evitar alocação: `Microsoft.IO.RecyclableMemoryStream`,
`Microsoft.Extensions.ObjectPool`, e helpers próprios —
`FetchSafelyFromArrayPool`, `FetchSafelyFromObjectPool`, `FetchSafelyFromSafeObjectPool`,
`TemporaryArray`, `TemporaryStringArray`, `SafeObjectPool`, `CollectionPooledObjectPolicy`.

Isso indica que desempenho e pressão de GC foram problemas reais e tratados.

### O ponto frágil: `DoEvents` e execução síncrona da UI

146 chamadas a `Application.DoEvents()`. O padrão é: uma operação longa roda na UI thread e
periodicamente bombeia a fila de mensagens para a janela não "congelar". Combinado com as
279 execuções síncronas de código assíncrono (`SafelyRunSynchronously`), o resultado é um
modelo em que **a UI thread executa trabalho pesado e se auto-desbloqueia**.

`Utils.EverDoEvents` controla quando isso é permitido:

```csharp
public static bool EverDoEvents => Program.IsMainThread && !IsDesignerMode && !IsRunningInVisualStudio;
```

No Android não existe equivalente a `DoEvents`, e bloquear a thread principal por mais de
alguns segundos resulta em ANR (*Application Not Responding*) — o sistema mata o processo.
Este é, junto com a extração do núcleo, o trabalho estrutural mais pesado do porte.

---

## Parte 2 — Notificação de mudança

### O problema

Em Shadowrun, alterar um valor cascateia. Mudar a Agilidade muda o pool de todas as
perícias baseadas em Agilidade, que muda os limites, que muda a defesa, que muda a ficha
inteira. A UI precisa ser notificada de **todas** as propriedades afetadas, não só da que
foi escrita.

### A solução: grafo de dependências declarado

Cada classe grande declara estaticamente quais propriedades dependem de quais:

```csharp
private static readonly PropertyDependencyGraph<Skill> s_SkillDependencyGraph =
    new PropertyDependencyGraph<Skill>(
        new DependencyGraphNode<string, Skill>(nameof(Pool),
            new DependencyGraphNode<string, Skill>(nameof(NonTrivialPool),
                new DependencyGraphNode<string, Skill>(nameof(AttributeModifiers),
                    new DependencyGraphNode<string, Skill>(nameof(AttributeObject),
                        new DependencyGraphNode<string, Skill>(nameof(Attribute)),
                        new DependencyGraphNode<string, Skill>(nameof(RelevantImprovements))
                    )
                ),
                new DependencyGraphNode<string, Skill>(nameof(Enabled)),
                new DependencyGraphNode<string, Skill>(nameof(IsNativeLanguage))
            ),
            …
```

Ao alterar uma propriedade, o objeto consulta o grafo:

```csharp
s_SkillDependencyGraph.GetWithAllDependents(this, strPropertyName, true)
```

e dispara `PropertyChanged` para o conjunto inteiro de propriedades afetadas — de uma vez,
via `MultiplePropertiesChangedEventArgs`.

Existem variantes com condição (`DependencyGraphNodeWithCondition`) e nós que carregam a
função de cálculo, inclusive a versão assíncrona:

```csharp
new DependencyGraphNode<string, Skill>(nameof(PoolOtherAttribute),
    x => x.NonTrivialPool,
    (x, t) => x.GetNonTrivialPoolAsync(t), …)
```

### Onde isso importa para o porte

Esta é a **melhor notícia da base de código** para o porte. O domínio já implementa
`INotifyPropertyChanged` de forma rigorosa e completa, com propagação correta de
dependências. Qualquer framework de UI com databinding declarativo (Avalonia, MAUI, WPF)
consome isso diretamente, sem adaptação.

Em outras palavras: a parte difícil de conectar um domínio a uma UI moderna **já está
pronta**. O que precisa ser refeito é a UI, não o mecanismo de notificação.
