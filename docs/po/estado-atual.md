# Estado atual — retomar daqui

**Este arquivo existe para uma coisa: permitir voltar a todo vapor depois de qualquer
interrupção.** Contexto estourado, contêiner reciclado, sessão perdida, uma semana sem
mexer — tanto faz. Leia este arquivo e o `prompt-agente.md` e continue.

Atualize-o ao fim de toda sessão. Se ele estiver velho, ele é pior que inútil.

**Última atualização:** 2026-08-13

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
| Linhas ainda em `Chummer/Backend/` | 322.125 |
| Linhas já em `src/Chummer.Core/` | 747 |
| Erros de **declaração** | **zero** |
| Distância do domínio até o Android, com stubs | **5** |
| **Acoplamento de CORPO de método** (medido em 13/08) | **1.584** |
| `ThreadSafeForm` no Backend | 327 |
| APK esqueleto | **17,86 MiB, gera em ~1 min 38 s** |
| Linhas de UI Avalonia | 756 |

**0,2% do domínio migrado.** O que existe até aqui é terreno preparado: metades de UI
separadas, abstração de interação (metade de mensagem), e o ferramental de verificação.

---

## Em voo neste momento

**Nada.** As duas frentes que rodavam em 12/08 foram entregues e **já integradas** nesta
branch:

- `claude/etapa-2-mugshots` — retratos como bytes. Declarações a zero.
- `claude/etapa-2.5-apk-esqueleto` — APK esqueleto e os spikes de plataforma (PR #2).

Se você está lendo isto depois de uma interrupção, confira mesmo assim:

```bash
git fetch origin && git branch -r | grep claude/
```

---

## O que mudou em 13/08, e é grande

**A luz acendeu.** Zerados os erros de declaração, o compilador passou a vincular corpos de
método (DEC-032) e apareceram **1.584 erros de corpo** — medidos pela primeira vez. O salto
de 20 para 1.584 **não é regressão**: é a régua trocando de significado. Registro completo em
`docs/codebase/15-acoplamento-de-corpo.md`.

Onde eles estão:

| Origem | Erros |
|---|---|
| `AddImprovementCollection` + `AddImprovementAsyncCollection` | **524** (262 cada, idêntico) |
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

1. **QA-009 — o PO instala o APK no A56.** É a única coisa que ninguém aqui dentro pode
   fazer: não há emulador nem `/dev/kvm`. Também responde se o Android suporta código
   dinâmico, que é a pergunta aberta de DEC-041.
2. **Consolidar `AddImprovementCollection` e `AddImprovementAsyncCollection`.** 524 dos 1.584
   erros, e ~15 mil linhas duplicadas. Nenhuma outra tarefa chega perto dessa alavanca.
3. **Metade de seleção** da abstração de interação — 337 erros, 24 diálogos `Select*`
   distintos. O inventário exato já está em `15-acoplamento-de-corpo.md`.
4. **Testar o spike de stubs de verdade** (DEC-037): carregar um `.chum5` no Android e ver se
   algum diálogo é atingido. Hoje só medimos distância de **compilação**, não execução.
5. **Mover o `Backend/` inteiro** para `Chummer.Core`, quando o acoplamento de corpo zerar.

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
