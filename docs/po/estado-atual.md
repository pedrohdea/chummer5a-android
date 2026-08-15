# Estado atual — retomar daqui

**Este arquivo existe para uma coisa: permitir voltar a todo vapor depois de qualquer
interrupção.** Contexto estourado, contêiner reciclado, sessão perdida, uma semana sem
mexer — tanto faz. Leia este arquivo e o `prompt-agente.md` e continue.

Atualize-o ao fim de toda sessão. Se ele estiver velho, ele é pior que inútil.

**Última atualização:** 2026-08-15 (mudança de rumo do PO: esqueleto antes de domínio)

---

## Como retomar, em três passos

```bash
./scripts/setup-dev.sh --android      # 1. toolchain (contêiner novo perde tudo)
./scripts/dev.sh status               # 2. onde o porte está, em números
./scripts/dev.sh check                # 3. a árvore está sã?
```

Depois leia, nesta ordem: `CLAUDE.md` → `docs/DESENVOLVIMENTO.md` → este arquivo →
`docs/po/plano.md`. Para abrir um agente novo, use `docs/po/prompt-agente.md`.

---

## Onde o porte está

| | |
|---|---|
| Linhas ainda em `Chummer/Backend/` | 321.655 |
| Linhas já em `src/Chummer.Core/` | 747 |
| Erros de **declaração** | **zero** |
| Distância do domínio até o Android, com stubs | **zero** |
| **Personagens de teste que CARREGAM fora do Windows** | **34 de 34** |
| **Acoplamento de CORPO de método** | **1.489** (era 1.584) |
| `ThreadSafeForm` no Backend | 327 |
| APK esqueleto | **14,94 MiB** (sem dados de jogo, arm64) |
| O APK **abre** num aparelho? | **NÃO SABEMOS** — job de CI criado em 15/08, nunca executado |
| Linhas de UI Avalonia | 756 |

**0,2% do domínio migrado.** O que existe até aqui é terreno preparado: metades de UI
separadas, abstração de interação (metade de mensagem), e o ferramental de verificação.

---

## A MUDANÇA DE RUMO DE 15/08 — leia isto antes de qualquer coisa

**O PO parou a portabilidade de domínio.** A ordem é: acertar o esqueleto primeiro. Um APK
simples, tela de apresentação, menus desativados, e o app **provado de pé** — antes de portar
mais uma linha de regra.

E ele está certo sobre a sequência. Havia um APK que **compila** desde 12/08 e **ninguém
nunca o tinha aberto**. Portar domínio por cima disso é construir sobre fundação não
verificada.

O que isso muda na prática:

- A tela do app **não faz nada**, e isso é o projeto, não uma limitação.
- Os dados de jogo **saíram do APK** (`EmbedGameData=false`). Religue com
  `-p:EmbedGameData=true` quando houver o que ler. APK: 17,86 → **14,94 MiB**.
- Frentes **em espera**, não canceladas: artefato dourado (DEC-049), consolidação de
  `AddImprovementCollection` (DEC-047, faltam 31 pares), metade de seleção da abstração.
- "Sem testes de carga" — ordem do PO, e ela vale para a sondagem de `.chum5` também.

---

## O que agora responde "o app ABRE?" (DEC-052)

A lacuna maior do projeto era: quatro ferramentas de verificação, todas respondendo
**compila?**. Nenhuma tocava o runtime do Android.

**O runner Linux do GitHub tem `/dev/kvm`; este contêiner não.** Essa diferença estava
disponível o tempo todo. O job `emulador` do `port-build.yml` instala o APK num emulador de
verdade, lança e exige três provas — processo vivo após 10 s, `logcat` sem `FATAL EXCEPTION`,
activity na pilha. A captura de tela sai sempre, inclusive na falha, porque numa falha ela
**é** o diagnóstico.

Receita conferida em `home-assistant/android`, que o PO apontou.

```bash
./scripts/verificar-apk.sh <apk> <png>   # só roda onde há emulador: no CI, não aqui
```

**ATENÇÃO:** esse job **nunca rodou** até o fechamento desta sessão. A primeira execução no
CI é que dirá se ele presta, e é bem possível que precise de ajuste.

---

## Em voo neste momento

**Nada.** As três frentes foram entregues, **integradas nesta branch e verificadas aqui**
(`./scripts/dev.sh check` verde após cada integração):

| Frente | PR | Estado |
|---|---|---|
| `claude/etapa-2-mugshots` | — | integrada · declarações a zero |
| `claude/etapa-2.5-apk-esqueleto` | #2 mergeado | integrada · APK de 17,86 MiB |
| `claude/consolidar-addimprovement` | #3 mergeado | integrada · censo 1.584 → 1.489 |

As branches continuam existindo no remoto; isso é histórico, não trabalho pendente.
`AddImprovementAsyncCollection.cs` não existe mais na árvore — é a prova de que a
consolidação entrou.

Se você está lendo isto depois de uma interrupção, confira mesmo assim:

```bash
git fetch origin && git branch -r | grep claude/
git log --oneline -1
```

---

## O que mudou em 14/08: o domínio EXECUTA fora do Windows

Até 13/08 sabíamos que o domínio estava a 5 erros de **compilar** para Android. Compilar não
é executar. `./scripts/testar-carga.sh` fecha a lacuna: monta um console `net9.0` **sem
WinForms** com o domínio real e abre os 34 personagens de `Chummer.Tests/TestFiles/`.

**A hipótese de DEC-037 se sustenta, e o número é 34 de 34.** Com `Load(showWarnings: false)`
os 34 personagens carregam inteiros — nome, metatipo, atributos, perícias, equipamento — e
**nenhum diálogo de seleção é atingido**. Carregar é parser XML e construção de objetos, como
se supunha. Com
`showWarnings: true` os 34 abrem `SelectBuildMethod`, e não por regra de jogo: as fichas
apontam para `settings/default.xml`, que o repositório não tem. Detalhes em DEC-050; PREM-023
fixa `showWarnings: false` para o leitor.

O que barrava a execução era **plataforma, não escolha do usuário** — thread STA, registro do
Windows, ACL de diretório, leitura de PDF dentro da carga. Os cinco achados estão em DEC-051.

---

## O que mudou em 13/08, e é grande

**A luz acendeu.** Zerados os erros de declaração, o compilador passou a vincular corpos de
método (DEC-032) e apareceram **1.584 erros de corpo** — medidos pela primeira vez. O salto
de 20 para 1.584 **não é regressão**: é a régua trocando de significado. Registro completo em
`docs/codebase/15-acoplamento-de-corpo.md`.

Onde eles estão:

| Origem | Erros |
|---|---|
| `AddImprovementCollection` (as duas metades, agora uma classe) | **429** — era 524 (262 cada) |
| `ColorManager` | 336 |
| `ThreadSafeForm` + `DialogResult` + `Form` | 337 |
| `Program` (fachada de UI) | 102 |
| domínio puro, só falta arrastar | ~200 |

Os 262 idênticos são confirmação mecânica de que os dois `AddImprovement*` são a mesma
lógica escrita duas vezes — **a maior alavanca isolada do resto da Etapa 2.**

**Dois spikes inverteram premissas:**
- O risco de tamanho não eram os dados de jogo (2,80 MiB comprimidos), era a ABI `android-x64`
  (13,2 MiB), que só serve para emulador. Só arm64: **17,86 MiB**.
- `XslCompiledTransform` **não degrada sem código dinâmico — ele explode.** NativeAOT fica
  fora do `Chummer.Android` enquanto a impressão for XSLT (DEC-041).

---

## Próximo passo, em ordem de valor

**O rumo mudou em 15/08.** Os itens 2, 3 e 5 abaixo continuam válidos e continuam sendo o
grosso do trabalho — mas estão **em espera** por ordem do PO até o esqueleto estar de pé.

1. **Ver o job `emulador` rodar no CI, e consertá-lo.** Ele foi escrito em 15/08 e **nunca
   executou**. Enquanto não passar, o projeto continua sem saber se o APK abre. Se falhar, a
   captura publicada como artefato `tela-emulador` é o primeiro lugar a olhar: branca aponta
   a armadilha do `TopLevel` do Avalonia, preta aponta outra coisa.
2. **QA-009 — o PO instala o APK no A56.** O emulador x86_64 do CI **não** é o A56: não pega
   defeito de ARM64, de densidade, de fabricante nem de memória real. O CI vira o primeiro
   filtro; o aparelho continua sendo o último.
3. **Crescer o esqueleto uma tela por vez**, cada uma passando pelo job do emulador antes da
   seguinte. É o que "esqueleto sustentável" quer dizer na prática.

### Em espera, não canceladas

- **Artefato dourado mínimo** (DEC-049) — a dependência dele já caiu: o domínio carrega ficha
  fora do Windows (DEC-050/051).
- **Consolidação de `AddImprovementCollection`** (DEC-047) — faltam 31 dos 42 pares com
  diálogo. Ferramenta pronta em `scripts/fundir-par.py`; repetição, não decisão.
- **Metade de seleção** da abstração de interação — 337 erros, 24 diálogos `Select*`.
- **Mover o `Backend/` inteiro** para `Chummer.Core`, quando o acoplamento de corpo zerar.

### Uma armadilha nova, que custou tempo nesta sessão

**`./scripts/dev.sh carga` sem argumento imprime `0 de 34`. Com `--sem-avisos`, `34 de 34`.**
Os dois números foram medidos nesta árvore em 15/08, e ambos estão certos — medem coisas
diferentes.
Isso não contradiz DEC-050: nesse modo o domínio pergunta qual configuração usar, porque as
fichas apontam para `settings/default.xml`, que o repositório não tem. O número que vale para
um leitor headless sai com `--sem-avisos`. **O padrão da ferramenta é o modo pessimista** —
quem rodar e ler "0 de 34" vai concluir a coisa errada.

## O que já é resiliente, e o que não é

**Sobrevive a tudo** (está no repositório):
- Documentação, plano, decisões, premissas, pendências, roteiro de QA
- `scripts/setup-dev.sh` — reproduz a toolchain inteira, incluindo o Android SDK
- `scripts/dev.sh` e as quatro ferramentas de verificação
- Todo commit empurrado

**Morre com o contêiner:**
- Worktrees de agentes que não empurraram
- Loops de `CronCreate` (são de sessão, não vão para disco)
- O Android SDK instalado em `~/android-sdk` — reinstale com `setup-dev.sh --android`
- Qualquer coisa em `/tmp`

**Regra prática:** commite e empurre a cada passo concluído, mesmo parcial, mesmo que não
compile — marcando no corpo do commit que é parcial. Trabalho parcial no remoto vale mais
que trabalho perfeito perdido.

---

## Armadilhas que já custaram caro

Estão no `prompt-agente.md` em detalhe. O resumo:

1. **O número do censo não é "quanto falta"** — mede só declaração (DEC-032).
2. **Compile sempre de dentro de `src/`** — o `global.json` da raiz fixa o SDK 8 (DEC-012).
3. **`Chummer.Core` está preso a C# 7.3** — compilação dupla com o net48 (DEC-027).
4. **O contêiner é efêmero** — toolchain vai em script versionado, nunca em comando avulso.
5. **Desconfie da própria ferramenta.** O primeiro número que uma medição produziu já esteve
   errado cinco vezes neste projeto. Teste a ferramenta injetando um defeito conhecido e
   exigindo que ela falhe.

---

## Decisões de escopo já fechadas com o PO

Hard fork · este repo é o destino final · Avalonia · MVP é leitor/gerenciador de sessão ·
criação de personagem só pelo método Prioridade · **sem fins comerciais** — o que tirou
telemetria, loja, identidade de marca e regras da casa do escopo.

Pendências abertas, **nenhuma bloqueando**: `PEND-007` (um `.chum5` real do PO), `PEND-010`
(origem do personagem durante o MVP), `PEND-014` (ritmo de entrega), `PEND-015` (critério de
aceite).
