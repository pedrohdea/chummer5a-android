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
**Etapa:** 4 (assets) · **Aparelho:** Samsung Galaxy A56

Quanto tempo leva do toque no ícone até o personagem na tela.

**Critério que proponho:** até 5 s é aceitável; acima de 10 s é reprovado e obriga a mudar a
estratégia de carga de dados.

**O que eu meço junto, e importa mais:** pico de memória gerenciada contra o limite de heap
do processo. O A56 tem RAM de sobra, mas o Android limita cada aplicativo a algo entre 256 e
512 MB. Se o DOM dos 21 MB de XML mais o cache do `XmlManager` chegarem perto disso, o
aplicativo é encerrado sem aviso — e isso não aparece como lentidão, aparece como o app
fechando sozinho.

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

---

## QA-008 — Foto do personagem sobrevive ao round-trip · AGUARDANDO
**Etapa:** 2 (extração do núcleo) · **Decisão:** DEC-034

Abrir no app um personagem seu que tenha foto, salvar sem mexer em nada, e reabrir.

**O que quero saber:** a foto continua lá, na mesma qualidade. E, se você tiver como
comparar, se ela ficou **igual** à do arquivo original em vez de um pouco pior.

**Por que existe:** hoje o Chummer recomprime a foto em JPEG a cada salvamento, então a
imagem degrada um pouco toda vez que você salva o personagem. A mudança para guardar os
bytes crus elimina isso. É melhoria, mas é mudança de comportamento — e mudança de
comportamento tem que ser vista por olho humano antes de virar verdade.

**Também vale testar:** trocar a foto por uma nova, salvar, reabrir. E um personagem com
várias fotos, trocando qual é a principal.

---

## QA-009 — Importar personagem do Hero Lab com foto · AGUARDANDO
**Etapa:** 2 (extração do núcleo) · **Decisão:** DEC-034

Importar um `.por` do Hero Lab que tenha retrato e conferir que o retrato aparece.

**O que quero saber:** a foto aparece, e aparece inteira — não cortada, não girada, não em
preto.

**Por que existe:** a importação lia a imagem do zip com GDI+, convertia o formato de pixel
e guardava o objeto de imagem. Agora ela copia os bytes do zip direto, sem abrir a imagem.
É mais simples e não perde nada, mas passa a aceitar qualquer formato que o Hero Lab
gravar — inclusive algum que o decodificador só vá reclamar na hora de exibir.

**Só vale se você tiver um `.por`.** Se não tiver, ignore: é caminho de importação, não do
uso normal.

---

## QA-010 — Mudar a compressão de retrato nas configurações · AGUARDANDO
**Etapa:** 2 (extração do núcleo) · **Decisão:** DEC-037

Em Configurações, mudar "compressão de retrato", salvar um personagem que **já** tinha foto,
e depois adicionar uma foto nova.

**O que quero saber:** você concorda com o comportamento novo — a mudança vale para a foto
**nova**, e a foto antiga fica como estava.

**Por que existe:** antes, mudar a compressão e salvar reprocessava as fotos já guardadas.
Agora a compressão acontece uma vez, quando você escolhe a foto. É melhoria (evitava-se
empilhar gerações de JPEG), mas é comportamento diferente do que o Chummer faz hoje, e é o
tipo de coisa que só o usuário pode dizer se incomoda.

**Se incomodar:** a saída não é voltar atrás, é um comando explícito de "recomprimir fotos
deste personagem". Diga se quiser que ele entre no escopo.
