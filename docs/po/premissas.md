# Premissas

O que foi assumido para o trabalho não parar. Cada premissa registra **o que muda se
estiver errada** e **quanto custa reverter** — para que o PO possa derrubá-la com noção do
preço, e para que o programador saiba onde é seguro construir por cima.

Status: `ATIVA` · `CONFIRMADA` · `DERRUBADA`

Reversão: 🟢 baixo · 🟡 médio · 🔴 alto (não construir por cima sem resposta)

---

## PREM-001 — Artefatos dourados são commitados no repositório · ATIVA 🟢
**Criada:** 2026-08-11 · **Pendência:** — (decidida pelo programador)

Os artefatos de referência do teste diferencial (XML de save e de impressão dos 34
personagens, gerados pelo build legado) ficam **versionados no repositório**, não gerados
como artefato de CI a cada execução.

**Por quê:** o diff aparece na revisão do PR. Uma mudança de comportamento de regra vira
linha vermelha e verde que o PO consegue ler sem rodar nada — o que é exatamente o papel de
QA. Um artefato de CI exige baixar e comparar manualmente, e na prática ninguém faz.

**Custo:** engorda o repositório. Mitigação: armazenar os dourados **comprimidos** e
normalizados, e não versionar os intermediários de execução (`TestRun-*`).

**Se derrubada:** troca de destino no job de CI. Poucas linhas.

---

## PREM-002 — O porte preserva o comportamento atual, inclusive os defeitos · ATIVA 🟢
**Criada:** 2026-08-11 · **Pendência:** —

Se uma regra do Chummer hoje calcula errado, o porte calcula igualmente errado. O teste
diferencial trata **qualquer** divergência do legado como regressão, mesmo quando a versão
nova parece mais correta.

**Por quê:** sem um oráculo estável, não há como distinguir "o porte quebrou" de "o porte
melhorou". Misturar as duas coisas torna toda divergência discutível, e aí ninguém confia
no diff.

**Consequência:** corrigir uma regra do Shadowrun é **tarefa separada**, aberta pelo PO,
com atualização deliberada do artefato dourado.

---

## PREM-003 — Alvo: Android 8.0 (API 26) ou superior, 4 GB de RAM · CONFIRMADA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-001

Assumido até haver resposta. Com isso, a estratégia de dados é a atual do Chummer:
**carregar e cachear tudo em memória**.

**Se derrubada para baixo** (aparelhos de 2–3 GB): a carga de dados precisa virar acesso
sob demanda com índice, o que é retrabalho estrutural de semanas.

**Confirmada em 2026-08-11:** o PO respondeu que o alvo é apenas o celular atual dele, sem
exigência de aparelho fraco. A premissa deixa de ser 🔴 e vira 🟢: a estratégia "carrega
tudo e cacheia" fica mantida, e não há mais frente bloqueada.

Continua valendo medir o consumo real no spike da Etapa 4 (QA-002) — não para decidir
arquitetura, mas para saber se a experiência é aceitável.

---

## PREM-004 — Empacotar `pt-br` e `en-us`; demais idiomas sob demanda · ATIVA 🟡
**Criada:** 2026-08-11 · **Pendência:** PEND-002

`en-us` é obrigatório: é o idioma base dos dados, e os nomes canônicos usados nos saves e na
tradução reversa dependem dele. `pt-br` porque é o idioma do PO.

**Se derrubada:** mudar quais arquivos entram nos assets e implementar (ou remover) o
download sob demanda. Trabalho contido.

---

## PREM-005 — Telemetria desligada no MVP · ATIVA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-003

Nenhuma coleta: sem Application Insights, sem envio de relatório de falha. Falhas contam
com o mecanismo nativo do Android.

**Por quê:** é o padrão seguro. Ligar coleta depois é aditivo e exige consentimento
explícito; desligar depois de ter coletado não desfaz a coleta.

**Efeito colateral bom:** remove as dependências `Microsoft.ApplicationInsights.*`, uma das
quais (`PerfCounterCollector`) é específica de Windows.

---

## PREM-006 — Distribuição por APK direto no MVP · ATIVA 🟡
**Criada:** 2026-08-11 · **Pendência:** PEND-004

O MVP é distribuído como APK assinado, publicado como release do GitHub. Sem loja.

**Por quê:** loja impõe requisitos (política de conteúdo, declaração de privacidade, API
mínima, assinatura gerenciada) que não fazem sentido resolver antes de existir um app
funcionando. O pipeline de build produz `.aab` também, para que a ida à loja depois seja
configuração, não reengenharia.

---

## PREM-007 — Os dados de jogo vão embarcados no aplicativo · ATIVA 🟡
**Criada:** 2026-08-11 · **Pendência:** PEND-005

O app já vem com `data/`, `lang/`, `sheets/` e `customdata/`, como o desktop faz.

**Se derrubada** (dados fornecidos pelo usuário): a arquitetura de assets muda para
importação e validação de pasta externa, e o primeiro uso ganha um fluxo de configuração.
Trabalho contido, mas afeta a experiência de instalação.

---

## PREM-008 — Identidade provisória do app · ATIVA 🟡
**Criada:** 2026-08-11 · **Pendência:** PEND-006

Nome de trabalho "Chummer" e package id provisório. **Não publicar em loja com esse id** —
package id é imutável após a primeira publicação.

---

## PREM-009 — Escopo de edição do MVP conforme definido · ATIVA 🟡
**Criada:** 2026-08-11 · **Pendência:** PEND-008

O MVP edita: dano físico e de atordoamento, Edge, karma, nuyen, munição/recarga. Nada além.

**Se derrubada:** cada item acrescentado (magias sustentadas, reagentes, rolagem de dados)
é incremento localizado sobre a mesma arquitetura — a estrutura do MVP-piloto foi desenhada
justamente para absorver isso. Custo por item: baixo a médio.

---

## PREM-010 — Compatibilidade bidirecional do formato `.chum5` · ATIVA 🔴
**Criada:** 2026-08-11 · **Pendência:** —

Um personagem salvo no Android abre no Chummer desktop, e vice-versa. O formato **não é
alterado** pelo porte.

**Por quê:** é o que permite ao usuário usar o celular na mesa e o desktop em casa, e é o
que torna o teste diferencial possível. Quebrar isso destrói simultaneamente o principal
caso de uso e o principal mecanismo de validação.

**🔴** Qualquer mudança de formato precisa de decisão explícita do PO, com plano de
migração.

---

## PREM-011 — Os quatro métodos de construção entram no escopo final · DERRUBADA
**Criada:** 2026-08-11 · **Pendência:** PEND-009

Prioridade, Soma-para-Dez, Karma e Life Modules, todos.

**Por quê:** "criação de personagem completa" foi o termo usado pelo PO, e o significado
literal é o escopo inteiro. Assumir o menor escopo arriscaria entregar algo que o PO
consideraria incompleto.

**Ordem de implementação:** Prioridade primeiro (é o método padrão e o mais usado), depois
Soma-para-Dez (compartilha quase toda a mecânica com Prioridade), depois Karma, e Life
Modules por último — é o mais isolado dos quatro e o de dados mais volumosos.

**Derrubada em 2026-08-11:** o PO respondeu **apenas Prioridade**. Como previsto, derrubar
esta premissa só encurtou trabalho — nada do que foi feito precisou ser refeito.

Substituída por PREM-012.

---

## PREM-012 — Criação de personagem cobre apenas o método Prioridade · CONFIRMADA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-009 (respondida)

Fora do escopo: Soma-para-Dez, Karma e Life Modules.

**Consequência no código:** entra `SelectMetatypePriority` e os dados de `priorities.xml`
(152 KB). Ficam de fora as telas `SelectBuildMethod`, `SelectMetatypeKarma` e
`SelectLifeModule`, e os 396 KB de `lifemodules.xml`.

**Ressalva:** o `Character` continua carregando personagens criados por qualquer método —
o `CharacterBuildMethod` é gravado no `.chum5` e precisa ser lido corretamente para não
quebrar PREM-010. O que sai do escopo é **criar** por esses métodos, não **abrir**.

---

## PREM-013 — MVP inclui magias sustentadas e rolagem de dados · CONFIRMADA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-008 (respondida)

O PO respondeu "tudo" ao escopo de edição em sessão. O MVP passa a cobrir, além de
dano/Edge/munição e karma/nuyen:

- **Magias sustentadas** — `Character.SustainedObjects` já existe; `SustainedObjectControl`
  no desktop serve de referência de requisito.
- **Rolagem de dados** — autocontido, sem impacto arquitetural. `DiceRoller` e
  `InitiativeRoller` no desktop são a referência; `ThreadSafeCachedRandom` e
  `XoshiroPRNG.Net` já estão no núcleo.

**Efeito líquido no cronograma:** somado a PREM-012, o escopo total **encolheu**. Três
métodos de construção a menos valem muito mais que dois recursos de sessão a mais.
