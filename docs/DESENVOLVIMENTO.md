# Como compilar, testar e desenvolver

Tudo passa por **`./scripts/dev.sh`**. Se você só quer uma linha:

```bash
./scripts/dev.sh check      # antes de todo commit
```

---

## O comando que importa

```bash
./scripts/dev.sh check
```

Roda, em ordem do mais barato ao mais caro, para falhar cedo:

1. **auditoria estrutural** das parciais extraídas — instantânea
2. **`verificar-ui.sh`** — o código extraído para `Controls/` compila sob net9.0
3. **`verificar-legado.sh`** — o aplicativo legado **inteiro** compila, ~30 s

O passo 3 é o que não pode ser pulado. É a única ferramenta local que enxerga erro **dentro
de corpo de método**, e é a ausência dela que deixou quatro rodadas de CI vermelhas passarem
por ferramentas locais verdes. Ver DEC-036.

---

## Por que existem quatro ferramentas e não uma

Elas medem coisas diferentes, e usar a errada dá falsa segurança.

| Comando | O que compila | O que ele vê | Quando usar |
|---|---|---|---|
| `dev.sh build` | `src/` (o porte) | o que já foi migrado | ao mexer em `Chummer.Core` |
| `dev.sh ui` | `Backend` + `Core` + `Controls` sob **net9.0 sem WinForms** | defeitos de extração, na fase de declaração | depois de rodar o extrator |
| `dev.sh legado` | **tudo**, sob **net9.0-windows com WinForms** | **corpos de método** — tipo errado, argumento nomeado inexistente, membro sumido | antes de todo commit no legado |
| `dev.sh censo` | `Backend` + `Core` sob net9.0 sem WinForms | **mede** acoplamento de declaração restante | para saber onde o porte está |

### A pegadinha que custou caro

O compilador **não vincula corpos de método quando a fase de declaração já falhou**
(DEC-032). Como o censo compila sem WinForms, sempre há erro de declaração — logo, o censo
**nunca** enxerga o interior de um método. Um campo de tipo inexistente apaga um `MessageBox`
no método ao lado.

Consequência prática, e vale gravar: **o número do censo não é "quanto falta"**. Ele é
"quanto falta na frente de declaração". A outra frente — 327 usos de `ThreadSafeForm`, por
exemplo — só aparece quando a primeira zerar.

`dev.sh legado` não tem esse problema porque ali o WinForms existe de verdade e os erros de
declaração chegam a zero.

---

## Ambiente

```bash
./scripts/dev.sh setup              # SDK .NET 9 + verificação
./scripts/dev.sh setup --android    # + workload Android
```

O contêiner é **efêmero**: só o que está no repositório sobrevive. Toda dependência de
toolchain vai em `scripts/setup-dev.sh`, nunca num comando avulso digitado na hora.

### Dois `global.json`, e a distinção é crítica

A raiz fixa o **SDK 8**, que é o do build legado, e o porte **não mexe nela**. `src/global.json`
fixa o **SDK 9**. O `dotnet` resolve o `global.json` pelo **diretório de trabalho** — então
compile sempre de dentro de `src/`. Da raiz, o SDK errado é escolhido e o build legado quebra
com `MSB3823`. É por isso que `dev.sh build` faz `cd src` antes de qualquer coisa (DEC-012).

---

## APK

**Não sai deste contêiner, e isso é um fato medido, não uma suposição.**

O workload .NET de Android instala normalmente:

```bash
dotnet workload install android      # funciona
```

Mas ele traz apenas o compilador e os runtimes. Empacotar um APK exige também o **SDK do
Google** — `platform-tools`, `platforms/android-NN`, `build-tools` — e:

- `dotnet build -t:InstallAndroidDependencies` falha com `XAIAD7009` neste contêiner;
- o download direto de `dl.google.com/android/repository/commandline-tools-linux-*.zip`
  responde **404** para todas as versões testadas.

**Onde o APK é gerado, portanto: no CI.** O runner `ubuntu-latest` do GitHub Actions já vem
com o Android SDK e `$ANDROID_HOME` definido. Se você tiver o SDK na sua máquina, defina
`ANDROID_HOME` e `./scripts/dev.sh apk` funciona local.

Isso **não bloqueia o desenvolvimento**: o `Chummer.Desktop` existe exatamente para depurar a
mesma UI Avalonia sem emulador e sem APK (DEC-002). O APK é passo de empacotamento, não de
desenvolvimento.

---

## Testes

**Hoje:** a suíte (`Chummer.Tests`) ainda é **net48** e aponta para o `Chummer.csproj`. Ela
roda no CI Windows, não em Linux.

**O plano:** teste **diferencial contra o build legado** (DEC-004). O net48 gera artefatos
dourados para os 34 personagens de `Chummer.Tests/TestFiles/`; o `Chummer.Core` gera os
mesmos e um job compara. Divergência é regressão até prova em contrário — o porte preserva
até os defeitos atuais (PREM-002).

A peça de maior retorno a construir: `PrintToXmlTextWriter` produz uma projeção com **todos
os valores de regra já calculados**. Hoje o `Test05` gera e descarta. Transformar isso em
artefato dourado dá detecção de regressão de regra por propriedade, quase de graça.

Enquanto os artefatos não existem, o que temos de verificação real é `dev.sh check` mais o
CI Windows.

---

## Fluxo de trabalho típico

Mexendo no código legado, que é onde o porte acontece hoje:

```bash
./scripts/dev.sh erros Weapon     # onde estão os erros nesse arquivo (~1 s)
# ... edita ...
./scripts/dev.sh legado           # o legado ainda compila? (~30 s)
./scripts/dev.sh censo            # o número andou?
./scripts/dev.sh check            # antes de commitar
git commit
```

Usando o extrator de UI:

```bash
python3 scripts/extrair-ui.py Chummer/Backend/Equipment/Weapon.cs
./scripts/dev.sh check
```

O extrator **funde** com o que já foi extraído antes e **verifica conservação de membros** —
ele move código, nunca apaga (DEC-033). Se a conta não fechar, ele aborta e restaura.

---

## Convenções

- **Código movido** do `Backend/` mantém a notação húngara original. Refatoração de estilo
  misturada com refatoração estrutural torna o diff irrevisável.
- **Código novo** (`Chummer.Core`, `Chummer.UI`): C# moderno, sem notação húngara.
- `Chummer.Core` está preso a **C# 7.3** enquanto durar a compilação dupla (DEC-027): o
  mesmo arquivo é compilado pelo projeto legado net48, que não fixa `LangVersion`.
- Conversa, documentação e PRs em **português**; código, identificadores e comentários em
  **inglês**.

---

## Onde ler mais

- `CLAUDE.md` — o acordo de trabalho e as decisões já tomadas
- `docs/po/plano.md` — as etapas e o estado de cada uma
- `docs/po/decisoes.md` — toda decisão técnica, com o motivo e as alternativas descartadas
- `docs/codebase/` — 14 documentos sobre o código como ele é, com números medidos
