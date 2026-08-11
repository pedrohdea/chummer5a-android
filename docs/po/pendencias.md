# Pendências do PO

Perguntas abertas que só o PO responde. Nenhuma delas está bloqueando o trabalho — cada uma
tem uma premissa correspondente em [`premissas.md`](premissas.md) sob a qual o
desenvolvimento segue.

Status: `ABERTA` · `RESPONDIDA` · `OBSOLETA`

---

## PEND-001 — Aparelhos-alvo · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-003 · **Reversão:** 🔴 alto

Qual o aparelho mais fraco que precisa rodar bem? Interessa a versão mínima do Android e,
principalmente, a **RAM disponível**.

**Por que importa agora e não depois:** o Chummer carrega ~21 MB de XML e monta uma árvore
de objetos grande em memória. Um personagem de teste do próprio repositório tem 5,7 MB.
Se o alvo incluir aparelhos de 3 GB de RAM, a estratégia de carga de dados muda de
"carrega tudo e cacheia" para "carrega sob demanda com índice" — e isso é decisão
estrutural, cara de mudar depois.

**Resposta (2026-08-11):** apenas o celular atual do PO. Sem exigência de aparelho fraco e
sem necessidade de rodar nos aparelhos dos outros jogadores.

**Efeito:** PREM-003 confirmada e **rebaixada de 🔴 para 🟢**. A estratégia "carrega tudo e
cacheia", que é a que o Chummer já usa, fica mantida. Deixa de existir frente bloqueada.

**Pendente ainda:** o modelo do aparelho, para o spike de desempenho da Etapa 4 (QA-002).
Não bloqueia — na falta do modelo, meço no desktop e reporto o consumo absoluto.

---

## PEND-002 — Idiomas empacotados no APK · ABERTA
**Criada:** 2026-08-11 · **Premissa:** PREM-004 · **Reversão:** 🟡 médio

`Chummer/lang/` tem 9,7 MB para 6 idiomas. Empacotar todos infla o APK. Quais idiomas
precisam vir instalados de fábrica?

Lembrando que são duas camadas: as frases da interface (pequenas) e a tradução do conteúdo
de jogo (o volume). Ver `docs/codebase/10-localizacao.md`.

**Resposta:**

---

## PEND-003 — Telemetria · ABERTA
**Criada:** 2026-08-11 · **Premissa:** PREM-005 · **Reversão:** 🟢 baixo

O upstream envia telemetria via Application Insights (`Program.ChummerTelemetryClient`,
`ExceptionHeatMap`) e coleta relatórios de falha. Manter, remover ou tornar opt-in
explícito?

Envolve política de loja de aplicativos e LGPD — é decisão de produto, não técnica.

**Resposta:**

---

## PEND-004 — Canal de distribuição · ABERTA
**Criada:** 2026-08-11 · **Premissa:** PREM-006 · **Reversão:** 🟡 médio

Google Play, APK direto, F-Droid, ou mais de um?

**Por que importa:** a Play Store impõe requisitos de política de conteúdo, privacidade e
API mínima, e exige assinatura gerenciada. F-Droid exige build reproduzível a partir da
fonte e é incompatível com dependências proprietárias. APK direto não impõe nada, mas
também não dá atualização automática.

**Resposta:**

---

## PEND-005 — Conteúdo de Shadowrun em loja pública · ABERTA
**Criada:** 2026-08-11 · **Premissa:** PREM-007 · **Reversão:** 🟡 médio

Os arquivos de `data/` contêm estatísticas de jogo de Shadowrun, propriedade da Catalyst
Game Labs / Topps. O upstream distribui isso como ZIP no GitHub há anos sem incidente, mas
publicar em loja de aplicativos com nome e ícone próprios tem visibilidade diferente.

Não é pergunta jurídica que eu possa responder — é decisão consciente de risco do PO.
Alternativa técnica, se preferir cautela: o app instala sem os dados e o usuário aponta
para os arquivos que ele já tem do Chummer desktop.

**Resposta:**

---

## PEND-006 — Identidade do aplicativo · ABERTA
**Criada:** 2026-08-11 · **Premissa:** PREM-008 · **Reversão:** 🟡 médio

Nome visível, ícone e *package id* (ex.: `com.exemplo.chummer`). O package id é
**imutável depois da primeira publicação** em loja — é o que identifica o app para sempre.

Não bloqueia o MVP, mas precisa estar decidido antes de qualquer publicação.

**Resposta:**

---

## PEND-007 — Personagem real para teste · ABERTA
**Criada:** 2026-08-11 · **Premissa:** — · **Reversão:** 🟢 baixo

Um `.chum5` de um personagem que você realmente usa na sua mesa.

O repositório já traz 34 personagens de teste (28 MB), mas são os que os mantenedores do
upstream escolheram. Personagens reais de uma mesa em uso costumam exercitar combinações
que a suíte oficial não cobre — especialmente custom data e house rules.

Se puder, coloque em `Chummer.Tests/TestFiles/` ou me mande que eu incorporo.

**Resposta:**

---

## PEND-009 — Métodos de construção de personagem no escopo final · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-011 · **Reversão:** 🟡 médio

O objetivo final inclui criação de personagem completa (DEC-007). O Chummer suporta
**quatro** métodos de construção: Prioridade, Soma-para-Dez, Karma e Life Modules.

Todos os quatro são obrigatórios, ou algum pode ficar para depois? Cada um tem tela própria
e regras próprias — Life Modules em particular tem um arquivo de dados de 396 KB e
documentação separada (`Chummer/Documentation/Lifemodule.md`).

Se você jogar só com um deles, a entrega da criação de personagem sai muito mais cedo.

**Resposta (2026-08-11):** **apenas Prioridade.**

**Efeito:** PREM-011 **derrubada**, e é a melhor notícia do projeto até agora. Saem do
escopo: Soma-para-Dez, Karma e Life Modules — este último sozinho traz 396 KB de dados,
regras próprias e documentação separada. Saem também as telas `SelectBuildMethod`,
`SelectMetatypeKarma` e `SelectLifeModule`; fica `SelectMetatypePriority`.

---

## PEND-008 — Escopo de edição do MVP · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-009 · **Reversão:** 🟡 médio

O MVP é um leitor/gerenciador de sessão. Está definido que ele edita: dano físico e de
atordoamento, Edge, karma, nuyen, munição/recarga.

Falta confirmar se isso cobre o que **você faz numa sessão real**. Candidatos que ficaram
de fora e podem ser essenciais na prática:

- marcar/desmarcar magias sustentadas
- rastrear Reagentes e Foco
- anotações rápidas durante a partida
- rolagem de dados integrada (existe `DiceRoller` no desktop)

**Resposta (2026-08-11):** **tudo.** Dano/Edge/munição, karma/nuyen, magias sustentadas e
rolagem de dados.

**Efeito:** PREM-009 **ampliada**. O MVP cresce em duas frentes:
- **Magias sustentadas** — barato. `Character` já tem a coleção `SustainedObjects` e o
  controle `SustainedObjectControl` no desktop mostra o que a tela precisa ter.
- **Rolagem de dados** — mais trabalho, mas autocontido e sem impacto arquitetural. Existe
  `DiceRoller` e `InitiativeRoller` no desktop como referência de requisito, além de
  `ThreadSafeCachedRandom` e `XoshiroPRNG.Net` já no núcleo.

Nenhum dos dois toca a arquitetura do piloto; ambos são incrementos sobre ela.
