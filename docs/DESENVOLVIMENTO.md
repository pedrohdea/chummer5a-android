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
| `testar-carga.sh` | domínio + sombras, sob **net9.0 sem WinForms**, e **EXECUTA** | o que só a execução mostra: diálogo atingido, API do Windows chamada | ao mexer em carga de personagem |

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

**Sai deste contêiner.** Medido em 2026-08-12: 1 min 38 s do zero, ~50 s incremental.

```bash
./scripts/dev.sh setup --android    # uma vez: SDK .NET + workload + SDK do Google
./scripts/dev.sh apk                # gera o APK e informa o tamanho
```

O setup instala o SDK do Google em `~/android-sdk`, e o `dev.sh` o adota automaticamente se
estiver lá — não é preciso exportar `ANDROID_HOME` na mão.

> Uma versão anterior deste documento afirmava que o APK **não** saía do contêiner, porque o
> download do `commandline-tools` respondia 404. A URL fixa é que estava errada; descobri-la
> pelo índice do repositório resolveu. O CI continua gerando o APK, mas agora para **publicar
> o artefato ao PO**, não porque falte capacidade aqui.

**Tamanho, medido** (ver DEC-039 para a composição completa):

| Configuração | APK |
|---|---|
| arm64 apenas, com todos os dados de jogo | **17,86 MiB** |
| arm64 + x86_64 (só para emulador) | 31,10 MiB |

O padrão é **só arm64**. Incluir `android-x64` acrescenta 13,2 MiB — mais do que todos os
dados de jogo juntos, que custam 2,80 MiB comprimidos. Para emulador:

```bash
./scripts/dev.sh apk -p:RuntimeIdentifiers="android-arm64;android-x64"
```

### Ver a UI sem aparelho e sem emulador

É para isso que o `Chummer.Desktop` existe (DEC-002).

```bash
./scripts/dev.sh tela /tmp/x.png    # renderiza a UI para PNG, sob Xvfb
./scripts/dev.sh tela /tmp/x.png --with-spikes   # já com as medições na tela
./scripts/dev.sh spikes             # as medições de plataforma no console
```

`dev.sh spikes` é a medição de **controle**: roda no desktop exatamente o que o APK roda no
aparelho. Sem ela, um número ruim no celular não distingue "o Android é lento" de "o código
é lento".

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

### `testar-carga.sh` — a única ferramenta que EXECUTA o domínio

As quatro ferramentas acima compilam. Esta roda:

```bash
./scripts/testar-carga.sh                 # os 34 personagens, com avisos
./scripts/testar-carga.sh --sem-avisos    # como o leitor do MVP vai carregar
./scripts/testar-carga.sh Skink           # só as fichas cujo nome casa
./scripts/testar-carga.sh --erros         # só compila, e agrupa os erros
```

Ela monta um console `net9.0` **sem WinForms** com `Chummer/Backend/`,
`Chummer/Controls/Dominio/`, `Chummer/Controls/Extensions/`, `src/Chummer.Core/`,
`src/Chummer.Compat/` e as sombras de `scripts/probe/RuntimeShims/`, abre cada `.chum5` e
imprime nome, metatipo, atributos e contagens — **imprimir os dados é o ponto**: sem eles,
"OK" não distingue carga de silêncio.

Tudo em `RuntimeShims/` **lança** ao ser usado, e é assim de propósito: uma sombra que
funciona esconde o acoplamento que a sondagem existe para medir. `LoadingBar` é a única
exceção (barra de progresso; lançar mataria toda carga), e `ColorManager` devolve cor porque
é apresentação pura.

Não é rápido — dezenas de segundos por ficha grande, e a rodada completa passa de vinte
minutos. Rode filtrado enquanto estiver iterando.

Ver DEC-049 e DEC-050 para o que ela já mediu.

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

---

## Testar o APK sem aparelho — o que funciona e o que não

Medido em 2026-08-13, neste contêiner.

### O emulador SOBE, mas não é utilizável ainda

Não há `/dev/kvm` e a CPU não expõe `vmx`/`svm` — só emulação por software. Ainda assim:

```bash
./scripts/setup-dev.sh --android
sdkmanager --install "emulator" "system-images;android-35;google_apis;x86_64"
avdmanager create avd -n teste -k "system-images;android-35;google_apis;x86_64"
emulator -avd teste -no-window -no-audio -no-snapshot -gpu swiftshader_indirect \
         -no-accel -memory 3072 -no-boot-anim &
```

**Resultado real:** o emulador **completa o boot** (~10 min sem aceleração; `adb devices`
mostra `device`, `init.svc.bootanim` fica `stopped`). Mas o `adb install` de um APK de 32 MB
falha com `Failure calling service package: Broken pipe (32)` — o serviço de pacotes não
aguenta, provavelmente por lentidão.

Conclusão honesta: **subir, sobe. Instalar e rodar, ainda não.** Quem quiser insistir deve
atacar por aí — APK menor, `-writable-system`, ou mais memória — e não repetir a instalação
do zero, que já está provada.

### A ABI é uma pegadinha

O APK padrão é **`android-arm64` apenas** (DEC-039), o que é certo para o aparelho e
economiza 13,2 MiB. Mas **nenhum emulador x86_64 roda esse APK**. Para emulador, edite
`RuntimeIdentifiers` no `Chummer.Android.csproj` para `android-arm64;android-x64` e publique
— passar a propriedade pela linha de comando **não funciona**, porque ela vaza para o
`Chummer.UI` e dá `NETSDK1083`. Com as duas ABIs o APK vai a 32,6 MB.

### O caminho que realmente funciona hoje: logcat do aparelho

Enquanto o emulador não fecha, o diagnóstico vem do celular do PO.

```bash
adb logcat -c && adb logcat | grep -iE 'chummer|AndroidRuntime|mono'
```

Sem PC: *Logcat Reader* (F-Droid, sem root), filtrando por `chummer`. O trecho que importa
começa em `FATAL EXCEPTION`.

**Um sintoma vale mais que uma varredura.** Se o app abre branco, o suspeito imediato é o
Avalonia resolver estilos subindo até um `TopLevel` — um `UserControl` fora de um `TopLevel`
renderiza válido e completamente branco. Foi pego uma vez na ferramenta de captura de tela.
