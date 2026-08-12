# Estado atual — retomar daqui

**Este arquivo existe para uma coisa: permitir voltar a todo vapor depois de qualquer
interrupção.** Contexto estourado, contêiner reciclado, sessão perdida, uma semana sem
mexer — tanto faz. Leia este arquivo e o `prompt-agente.md` e continue.

Atualize-o ao fim de toda sessão. Se ele estiver velho, ele é pior que inútil.

**Última atualização:** 2026-08-12

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
| Linhas ainda em `Chummer/Backend/` | 321.901 |
| Linhas já em `src/Chummer.Core/` | 747 |
| Erros de **declaração** (censo) | 20 — todos mugshots |
| Distância do domínio até o Android, com stubs | 25 erros |
| `ThreadSafeForm` no Backend (corpo, não medido pelo censo) | 327 |
| Linhas de UI Avalonia | 0 |

**0,2% do domínio migrado.** O que existe até aqui é terreno preparado: metades de UI
separadas, abstração de interação (metade de mensagem), e o ferramental de verificação.

---

## Em voo neste momento

Duas frentes rodando em worktrees separados. **Se a sessão morreu, verifique se as branches
abaixo existem no remoto** — o trabalho pode ter sido empurrado antes de eu integrar.

| Frente | Branch | Entrega |
|---|---|---|
| Etapa 2.5 — APK esqueleto | `claude/etapa-2.5-apk-esqueleto` | Avalonia + Android + Desktop, spikes de plataforma |
| Etapa 2 — retratos como bytes | `claude/etapa-2-mugshots` | zerar os 20 erros de declaração |

```bash
git fetch origin && git branch -r | grep claude/
```

Se as branches existirem e a sessão tiver morrido, **integre-as antes de recomeçar qualquer
coisa** — refazer trabalho já feito é o desperdício mais caro deste projeto.

---

## Próximo passo, em ordem de valor

1. **Integrar as duas frentes em voo** (acima).
2. **Rodar o censo depois que os mugshots zerarem.** Este é o item de maior valor do
   projeto agora: enquanto houver um erro de declaração, o compilador não vincula corpos de
   método (DEC-032), e metade do acoplamento restante é invisível. Zerar acende a luz. O
   resultado deve virar `docs/codebase/15-acoplamento-de-corpo.md`.
3. **Testar o spike de stubs de verdade** (DEC-037): carregar um `.chum5` no Android e ver
   se algum diálogo é atingido. Hoje só medimos distância de **compilação**, não execução.
4. **Metade de seleção** da abstração de interação — os 327 `ThreadSafeForm`. É o grosso do
   que falta na Etapa 2.
5. **Mover o `Backend/` inteiro** para `Chummer.Core`, de uma vez, quando o censo zerar.

---

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
