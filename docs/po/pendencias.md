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

**Complemento (2026-08-11):** o PO informou o modelo — **Samsung Galaxy A56** (Exynos 1580,
6/8/12 GB conforme variante, Android 15). Pendência **encerrada**.

O aparelho é confortável, mas ver PREM-003: o risco real não é a RAM do dispositivo e sim o
limite de heap por processo que o Android impõe, que fica na casa das centenas de MB
independentemente do total instalado.

---

## PEND-002 — Idiomas empacotados no APK · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-004 · **Reversão:** 🟡 médio

`Chummer/lang/` tem 9,7 MB para 6 idiomas. Empacotar todos infla o APK. Quais idiomas
precisam vir instalados de fábrica?

Lembrando que são duas camadas: as frases da interface (pequenas) e a tradução do conteúdo
de jogo (o volume). Ver `docs/codebase/10-localizacao.md`.

**Resposta (2026-08-11):** "tanto faz, decida você". Decidido: **`pt-br` + `en-us`
embarcados**, os outros quatro fora do APK. PREM-004 confirmada.

O `en-us` não é escolha de gosto: é o idioma canônico dos dados e dos saves, e a tradução
reversa depende dele.

---

## PEND-003 — Telemetria · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-005 · **Reversão:** 🟢 baixo

O upstream envia telemetria via Application Insights (`Program.ChummerTelemetryClient`,
`ExceptionHeatMap`) e coleta relatórios de falha. Manter, remover ou tornar opt-in
explícito?

Envolve política de loja de aplicativos e LGPD — é decisão de produto, não técnica.

**Resposta (2026-08-12):** **Sem telemetria.** O projeto não tem fins comerciais, e não há a quem reportar nada. Removida do porte por inteiro — não é "desligada por padrão", é ausente. Isso elimina o pacote Application Insights da árvore de dependências.

---

## PEND-004 — Canal de distribuição · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-006 · **Reversão:** 🟡 médio

Google Play, APK direto, F-Droid, ou mais de um?

**Por que importa:** a Play Store impõe requisitos de política de conteúdo, privacidade e
API mínima, e exige assinatura gerenciada. F-Droid exige build reproduzível a partir da
fonte e é incompatível com dependências proprietárias. APK direto não impõe nada, mas
também não dá atualização automática.

**Resposta (2026-08-12):** **APK direto, sem loja.** Sem fins comerciais, o aplicativo é instalado à mão pelos envolvidos. Nenhuma política de loja se aplica.

---

## PEND-005 — Conteúdo de Shadowrun em loja pública · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-007 · **Reversão:** 🟡 médio

Os arquivos de `data/` contêm estatísticas de jogo de Shadowrun, propriedade da Catalyst
Game Labs / Topps. O upstream distribui isso como ZIP no GitHub há anos sem incidente, mas
publicar em loja de aplicativos com nome e ícone próprios tem visibilidade diferente.

Não é pergunta jurídica que eu possa responder — é decisão consciente de risco do PO.
Alternativa técnica, se preferir cautela: o app instala sem os dados e o usuário aponta
para os arquivos que ele já tem do Chummer desktop.

**Resposta (2026-08-12):** **Não se aplica.** Sem publicação em loja, a questão de conteúdo de Shadowrun em plataforma pública desaparece. O README registra a não afiliação com a Catalyst Game Labs.

---

## PEND-006 — Identidade do aplicativo · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-008 · **Reversão:** 🟡 médio

Nome visível, ícone e *package id* (ex.: `com.exemplo.chummer`). O package id é
**imutável depois da primeira publicação** em loja — é o que identifica o app para sempre.

Não bloqueia o MVP, mas precisa estar decidido antes de qualquer publicação.

**Resposta (2026-08-12):** **Sem identidade própria.** Não há marca a construir. Nome e ícone ficam como estiverem; não é assunto do projeto.

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

---

## PEND-010 — De onde vem o personagem antes da Etapa 8? · ABERTA
**Criada:** 2026-08-11 · **Premissa:** PREM-014 · **Reversão:** 🟢 baixo

O PO respondeu em PEND-011 que o personagem fica **só no celular**, e o desktop deixa de
ser usado. Isso colide com duas decisões já tomadas:

- o MVP é um **leitor** — não cria personagem (DEC-003);
- criação de personagem só chega na **Etapa 8**.

Enquanto a criação não existir no Android, o desktop é a única forma de produzir um
personagem novo. Logo, "só no celular" não pode valer desde já — na melhor das hipóteses é
o estado final do projeto, não o inicial.

**A pergunta:** durante o período MVP, você vai criar os personagens no desktop e passar
para o celular, ou o MVP precisa de alguma forma mínima de criar personagem?

**Resposta:**

---

## PEND-011 — Circulação do personagem entre celular e desktop · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-010, PREM-014 · **Reversão:** 🟡 médio

**Resposta (2026-08-11):** **só no celular** — o desktop deixa de ser usado.

**Efeito, separando duas coisas que a resposta juntou:**

| Item | Decisão | Motivo |
|---|---|---|
| Formato `.chum5` idêntico ao desktop | **mantido** (PREM-010 segue 🔴) | é o oráculo do teste diferencial (DEC-004): sem ele, não há como detectar regressão de regra |
| Sincronização em nuvem e resolução de conflito | **cortado** (PREM-014) | era a parte cara da compatibilidade, e o PO não precisa dela |

Manter o formato custa zero, porque o código de save está sendo portado como está. O que a
resposta do PO de fato elimina é o trabalho de conflito de edição.

Gerou PEND-010.

---

## PEND-012 — Modo mestre depois do MVP · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-015 · **Reversão:** 🟢 baixo

**Resposta (2026-08-11):** o MVP é para **jogador**; o modo mestre vem depois, fora do MVP.

**Efeito:** mestre está fora do **MVP**, não fora do **projeto**. Consequência de desenho
imediata: `Program.OpenCharacters` já é uma coleção e o núcleo suporta N personagens
nativamente. Essa capacidade é **mantida viva no núcleo** mesmo com a UI do MVP abrindo um
personagem por vez — preservar agora é gratuito, reintroduzir depois é caro.

---

## PEND-013 — Regras da casa e dados customizados · RESPONDIDA
**Criada:** 2026-08-11 · **Premissa:** PREM-017 · **Reversão:** 🟡 médio

A mesa do PO usa regras padrão, algum dos 57 pacotes de `customdata/`, ou XML próprio?

**Por que importa:** se houver custom data própria, o app precisa carregar dados de fora do
pacote instalado, o que envolve armazenamento gravável no Android e um fluxo de importação.
Se for tudo padrão, a tela de configuração de ruleset pode ficar para depois do MVP — é uma
das mais complexas do desktop (`EditCharacterSettings`).

**Resposta (2026-08-12):** **Fora do MVP.** Regras da casa e dados customizados não entram. O aplicativo usa o ruleset padrão. Os 34 personagens de teste do repositório usam todos `default.xml`, então isso é exatamente o que já está coberto.

---

## PEND-014 — Ritmo de entrega · ABERTA
**Criada:** 2026-08-11 · **Premissa:** PREM-018 · **Reversão:** 🟢 baixo

APK instalável o quanto antes mesmo incompleto, só quando o MVP estiver pronto, ou marcos
a cada etapa?

**Resposta:**

---

## PEND-015 — Critério de aceite do objetivo final · ABERTA
**Criada:** 2026-08-11 · **Premissa:** PREM-019 · **Reversão:** 🟢 baixo

O objetivo é "APK executável e testado". O que faz o PO dizer que está pronto: jogar uma
sessão inteira sem recorrer ao papel, os números baterem com o desktop, criar um personagem
do zero no celular, ou os três?

**Por que importa:** define quando o projeto termina, e define o que o `qa-roteiro.md`
precisa cobrir.

**Resposta:**


---

## PEND-016 — Recomprimir fotos já guardadas · ABERTA
**Criada:** 2026-08-12 · **Premissa:** PREM-020 · **Reversão:** 🟢 baixo

A configuração de compressão de retrato passou a valer só para fotos **novas** (DEC-037).
Você sente falta de conseguir encolher as fotos de um personagem antigo?

**Por que importa:** se a resposta for sim, a forma certa é um comando explícito
("recomprimir fotos deste personagem"), não o efeito colateral de salvar que existia antes.
É trabalho pequeno, mas é tela nova — precisa entrar no escopo de propósito.

**Por que não bloqueia:** o comportamento novo é o mais conservador dos dois. Ele nunca
degrada uma foto sem que você peça; o antigo degradava a cada salvamento.

**Resposta:**
