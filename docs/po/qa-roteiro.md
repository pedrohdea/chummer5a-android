# Roteiro de QA

O que precisa de **teste humano** — o que os testes automatizados não alcançam. O
programador acumula itens aqui conforme constrói; o PO executa quando houver o que executar.

Status: `AGUARDANDO` (ainda não há build) · `PRONTO` (pode testar) · `PASSOU` · `FALHOU`

---

## Situação atual

**Não há nada para testar ainda.** O trabalho até aqui foi documentação (Etapa 0). Os itens
abaixo estão pré-registrados para que o roteiro exista antes de ser preciso — e para que o
PO saiba desde já o que vai ser cobrado dele.

---

## O que o teste automatizado cobre (não precisa de você)

Registrado para delimitar: carga de todo o XML de dados · carga dos 34 personagens de teste
· round-trip de save em `.chum5` e `.chum5lz` com diff XML · geração da ficha impressa em
todos os idiomas · e, a partir da Etapa 2, **diff contra os artefatos dourados do build
legado** para save e impressão.

Detalhe da estratégia: `docs/codebase/11-projetos-satelite.md` e as anotações de teste em
`decisoes.md` (DEC-004).

---

## QA-001 — Fidelidade de regras num personagem real · AGUARDANDO
**Etapa:** 2 (extração do núcleo) · **Depende de:** PEND-007

Abrir o **seu** personagem no núcleo portado e conferir, valor a valor, contra o que o
Chummer desktop mostra: pools de dados, limites, Essência, iniciativa, karma e nuyen.

**Por que humano:** o diff automatizado cobre os 34 personagens do repositório. O seu
exercita combinações que eles não cobrem — especialmente custom data e house rules da sua
mesa.

---

## QA-002 — Desempenho de abertura no aparelho · AGUARDANDO
**Etapa:** 4 (assets) · **Alimenta:** PEND-001

Quanto tempo leva do toque no ícone até o personagem na tela, no seu aparelho real.

**Critério que proponho:** até 5 s é aceitável; acima de 10 s é reprovado e obriga a mudar a
estratégia de carga de dados.

---

## QA-003 — Ciclo de vida do Android · AGUARDANDO
**Etapa:** 6 (MVP)

Com um personagem aberto e alterações não salvas: mandar o app para segundo plano, usar
outros apps até o Android matar o processo, e voltar. **Nada pode ser perdido silenciosamente.**

Também: girar a tela, receber ligação, bloquear e desbloquear.

**Por que humano:** é o modo de falha mais comum de app Android e não aparece em emulador
com memória sobrando.

---

## QA-004 — Fluxo real de arquivos · AGUARDANDO
**Etapa:** 6 (MVP)

Abrir um `.chum5` que está no Google Drive. Salvar. Compartilhar por WhatsApp. Abrir um
recebido de outra pessoa. Abrir um `.chum5lz`.

**Por que humano:** o armazenamento com escopo do Android tem comportamento que só aparece
com provedores reais de arquivo.

---

## QA-005 — Round-trip com o desktop · AGUARDANDO
**Etapa:** 6 (MVP) · **Valida:** PREM-010

Salvar no celular, abrir no Chummer desktop, conferir que está íntegro. E o inverso.

**Critério:** nenhuma perda de dado. Este é o teste que protege a premissa mais cara do
projeto.

---

## QA-006 — Ficha impressa · AGUARDANDO
**Etapa:** 7 (fichas)

A ficha renderizada no celular bate com a do desktop, nos layouts que você usa. Legível em
tela pequena.

---

## QA-007 — Sessão de jogo real · AGUARDANDO
**Etapa:** 6 (MVP) — **o teste mais importante do projeto**

Usar o app numa partida de verdade, na mesa, com as mãos ocupadas e sob pressão de tempo.

**O que quero saber:** o que você tentou fazer e não achou; o que exigiu toques demais; o
que você acabou fazendo no papel porque foi mais rápido; e se em algum momento você
preferiu o desktop.

**Por que é o mais importante:** uma partida encontra mais problema de usabilidade que um
mês de emulador. É também o único teste que valida a premissa central do projeto — que um
gerenciador de sessão sem criação de personagem é útil de verdade numa mesa.
