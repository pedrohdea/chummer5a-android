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

## PREM-003 — Alvo: Samsung Galaxy A56 · CONFIRMADA 🟡
**Criada:** 2026-08-11 · **Pendência:** PEND-001

Assumido até haver resposta. Com isso, a estratégia de dados é a atual do Chummer:
**carregar e cachear tudo em memória**.

**Se derrubada para baixo** (aparelhos de 2–3 GB): a carga de dados precisa virar acesso
sob demanda com índice, o que é retrabalho estrutural de semanas.

**Confirmada em 2026-08-11:** o PO respondeu que o alvo é apenas o celular atual dele, sem
exigência de aparelho fraco. A premissa deixa de ser 🔴 e vira 🟢: a estratégia "carrega
tudo e cacheia" fica mantida, e não há mais frente bloqueada.

**Refinada em 2026-08-11:** o PO especificou **Android intermediário, 4–6 GB de RAM** — e
não um aparelho folgado, como a primeira resposta sugeria. A estratégia "carrega tudo e
cacheia" segue mantida, mas a margem é menor do que eu havia assumido.

**Especificado em 2026-08-11:** **Samsung Galaxy A56**. Exynos 1580 (4 nm, núcleo principal
a 2,91 GHz), **6, 8 ou 12 GB de RAM** conforme a variante, Android 15 com One UI 7, lançado
em março de 2025 e com seis anos de atualizações prometidas.

**A marca continua 🟡, mas por um motivo diferente do que eu supunha.** O aparelho é
folgado; o problema não é a RAM do dispositivo, é o **limite de heap por aplicativo** que o
Android impõe. Um aparelho com 8 GB não dá 8 GB ao processo — o limite típico fica entre
256 e 512 MB, e `largeHeap` no manifesto empurra o teto, não o remove.

O que precisa ser medido, portanto, não é "cabe na memória do celular" e sim:

- quanto o DOM dos 21 MB de XML ocupa em memória gerenciada (documentos XML costumam
  expandir várias vezes sobre o texto cru);
- quanto disso se multiplica pelo cache do `XmlManager`, que guarda **um documento mesclado
  por combinação de idioma × custom data** (ver `docs/codebase/05-sistema-dados.md`);
- quanto sobra para a árvore do personagem, que num caso extremo do repositório chega a
  5,7 MB de arquivo.

Se o total se aproximar do teto de heap, a estratégia "carrega tudo e cacheia" cai e o
acesso sob demanda com índice volta à mesa — o mesmo risco de antes, com outra causa.

DEC-021 (retratos como bytes, sem decodificar) já ajuda nessa conta.

---

## PREM-004 — Empacotar `pt-br` e `en-us`; demais idiomas sob demanda · CONFIRMADA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-002

`en-us` é obrigatório: é o idioma base dos dados, e os nomes canônicos usados nos saves e na
tradução reversa dependem dele. `pt-br` porque é o idioma do PO.

**Confirmada em 2026-08-11:** o PO delegou a decisão ("tanto faz, decida você"). Mantida
como está: `pt-br` + `en-us` no APK, os outros quatro fora.

---

## PREM-005 — Telemetria removida, não apenas desligada · CONFIRMADA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-003

Nenhuma coleta: sem Application Insights, sem envio de relatório de falha. Falhas contam
com o mecanismo nativo do Android.

**Por quê:** é o padrão seguro. Ligar coleta depois é aditivo e exige consentimento
explícito; desligar depois de ter coletado não desfaz a coleta.

**Efeito colateral bom:** remove as dependências `Microsoft.ApplicationInsights.*`, uma das
quais (`PerfCounterCollector`) é específica de Windows.

---

## PREM-006 — Distribuição por APK direto, sem loja · CONFIRMADA 🟢
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

## PREM-008 — Sem identidade de marca própria · CONFIRMADA 🟢
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

---

## PREM-014 — Sem sincronização e sem resolução de conflito · CONFIRMADA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-011 (respondida)

O app não sincroniza com nuvem nem trata edição concorrente do mesmo arquivo em dois
lugares. O usuário gerencia seus arquivos.

**Importante não confundir com PREM-010:** o *formato* `.chum5` continua idêntico ao do
desktop. O que sai do escopo é a *sincronização*, não a *compatibilidade*. A distinção
importa porque o formato é o oráculo do teste diferencial (DEC-004) — abrir mão dele
custaria o mecanismo que detecta regressão de regra, enquanto abrir mão da sincronização
não custa nada.

---

## PREM-015 — MVP é para jogador; o núcleo continua multi-personagem · CONFIRMADA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-012 (respondida)

A UI do MVP abre **um** personagem por vez. O **núcleo** continua suportando N, porque
`Program.OpenCharacters` já é uma coleção observável e o modo mestre está no projeto,
apenas depois do MVP.

**Regra de desenho que decorre disto:** nenhuma simplificação do núcleo pode assumir
"existe um único personagem". Singletons implícitos, estado global por personagem e caches
não-chaveados estão proibidos — são exatamente o tipo de atalho que parece inofensivo no
MVP e custa semanas quando o modo mestre chegar.

Fora do MVP e no projeto: `GameMasterDashboard`, `PlayerDashboard`, `InitiativeTracker`,
`AddToken`.

---

## PREM-016 — Durante o MVP, o personagem é criado no desktop · ATIVA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-010

O MVP continua sendo leitor puro. Até a Etapa 8, o Chummer desktop é a única forma de
produzir um personagem novo, e o `.chum5` é transferido manualmente para o celular.

**Se derrubada** (o MVP precisa criar): o recorte muda de forma séria — a criação por
Prioridade entraria no MVP, com metatipo, atributos, perícias e compras iniciais. Deixaria
de ser um MVP-leitor. Por isso a pergunta foi registrada mesmo sem resposta.

---

## PREM-017 — Mesa usa regras padrão; o ruleset gravado é respeitado · CONFIRMADA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-013

O MVP **não** ganha tela de configuração de ruleset. Mas o `CharacterSettings` gravado no
`.chum5` é lido e respeitado integralmente — inclusive quais livros estão ligados e quais
pacotes de custom data o personagem usa.

**A distinção que sustenta esta premissa:** *editar* ruleset é caro (`EditCharacterSettings`
é uma das telas mais complexas do desktop); *respeitar* o ruleset é obrigatório de qualquer
forma, porque sem isso os valores calculados divergem e o teste diferencial acusa.

**Se derrubada** (mesa usa custom data própria): entra o fluxo de importação de pasta
externa, que depende do armazenamento gravável do Android. Trabalho contido, mas antecipa
parte da Etapa 3.

---

## PREM-018 — APK instalável o quanto antes, mesmo incompleto · ATIVA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-014

Assim que houver algo que abra um `.chum5` e mostre um personagem, sai APK — mesmo feio e
parcial.

**Por quê, e o motivo não é só feedback:** PREM-003 subiu para 🟡 porque o alvo é um
aparelho intermediário e a carga de 21 MB de XML precisa de medição real. Um APK cedo é o
**instrumento dessa medição**. Sem ele, o número da Etapa 4 é extrapolação de desktop.

Ou seja: entregar cedo serve simultaneamente ao QA do PO e à decisão técnica de arquitetura
de dados. É a opção que rende duas coisas pelo preço de uma.

---

## PREM-019 — Aceite em três marcos sucessivos · ATIVA 🟢
**Criada:** 2026-08-11 · **Pendência:** PEND-015

O objetivo "APK executável e testado, com criação de personagem completa" é considerado
cumprido quando os três forem verdadeiros, nesta ordem:

1. **Fidelidade** — os valores do app conferem com os do desktop para os personagens do PO
   (QA-001). É pré-requisito dos outros dois: sem isso, jogar com o app é jogar errado.
2. **Uso real** — uma sessão inteira jogada só com o app, sem recorrer ao papel (QA-007).
3. **Completude** — criar um personagem do zero pelo método Prioridade, sem tocar no
   desktop (PREM-012).

**Por que nessa ordem, e não na do enunciado:** fidelidade primeiro porque é a única que
invalida as outras se falhar. Um app bonito que calcula o pool errado é pior que nenhum app.



---

## Confirmação em bloco — sem fins comerciais · 2026-08-12

O PO respondeu PEND-003, PEND-004, PEND-005 e PEND-006 com uma única razão: **o projeto não
tem fins comerciais**. Isso confirma quatro premissas de uma vez e, mais do que confirmar,
endurece três delas de 🟡 para 🟢 — deixaram de ser apostas e viraram consequência de uma
decisão de escopo.

O ganho técnico é concreto, e maior do que "uma pergunta a menos":

- **Telemetria some do porte.** Não é "desligada por padrão", é ausente. O pacote Application
  Insights sai da árvore de dependências, e com ele o `TelemetryClient` que a extração já
  vinha tratando como código de UI.
- **Etapa 10 encolhe.** Sem loja, não há política de plataforma, revisão, assinatura de
  publicação nem questão de propriedade intelectual em vitrine pública. Sobra assinar o APK
  para instalação direta.
- **PEND-005 deixa de existir** em vez de ser respondida: sem publicação, não há pergunta.

PEND-013 (regras da casa) foi respondida no mesmo bloco e aponta na mesma direção: o
aplicativo usa o ruleset padrão. Medido: os 34 personagens de teste do repositório usam
**todos** `default.xml` — o que já estava coberto é exatamente o que ficou no escopo.
