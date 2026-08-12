# Plano Mestre do Porte

Da base WinForms/net48 até o objetivo final: **APK executável e testado, com criação de
personagem completa** (DEC-007, método Prioridade apenas — PREM-012).

Cada etapa termina com **reavaliar o plano na totalidade**. Não é formalidade: o projeto já
teve duas etapas em que a medição derrubou a premissa de planejamento da etapa seguinte
(ver "Histórico de reavaliações", no fim). Seguir um plano sem revisá-lo é como confiar num
número que ninguém mediu.

Status: ⬜ não iniciada · 🔄 em andamento · ✅ concluída

---

## Etapa 0 — Documentação da base ✅

Descrever o código como ele é, antes de qualquer refatoração.

- ✅ 14 documentos em `docs/codebase/`, técnicos e não técnicos
- ✅ `CLAUDE.md` e o protocolo de `docs/po/`
- ✅ **Reavaliar o plano na totalidade** — *feito: o achado de que carregar um `.chum5`
  instancia 30 tipos distintos provou que não existe fatia fina do domínio, e fixou o
  recorte do MVP na UI e não no núcleo.*

## Etapa 1 — Toolchain, esqueleto e medição ✅

- ✅ 1.0 `scripts/setup-dev.sh`, SDK versionado
- ✅ 1.1 `src/Chummer.Core` + `Chummer.Port.sln` compilando em Linux; CI do porte
- ✅ 1.2/1.3 Censo de erros: 733 distintos, dos quais 451 de domínio
- 🔄 1.4 Spikes de risco — **absorvidos pela Etapa 2.5**, onde estão medidos. Tamanho do APK
  e carga dos dados: fechados. `XslCompiledTransform`: a dependência foi medida e a
  consequência registrada (DEC-037); falta só a confirmação no aparelho (QA-009)
- ⬜ 1.5 Desenho da interface `IUserPrompt`
- ✅ **Reavaliar o plano na totalidade** — *feito: o censo mostrou 38% dos erros em arquivos
  que nem sobem para o núcleo, e o build legado foi provado funcional (DEC-015), encerrando
  o maior risco do projeto.*

## Etapa 2 — Extração do núcleo 🔄

Tornar todo o `Backend/` compilável sob net9.0, sem UI e sem `System.Drawing`.

- ✅ Mecanismo de migração provado pelo CI (DEC-013): uma fonte da verdade, duas compilações
- ✅ Primeiro lote migrado: `Backend/Enums`, 11 arquivos
- 🔄 **Consertar em lugar** os arquivos com erro, na ordem do censo (DEC-016)
  - ✅ Interfaces separadas de suas extensões de UI (DEC-018) — 733 → 671
  - ✅ Infraestrutura de UI devolvida ao lugar dela (DEC-020) — 671 → **367**
  - ✅ Abstração de interação com o usuário, metade de mensagem (DEC-026) — 367 → 89
  - ✅ Extração dos membros restantes, com o extrator corrigido (DEC-033) — 89 → **20**
  - ⬜ **Retratos como bytes (DEC-021, desenhado em DEC-034)** — os 20 que sobram são
    todos deste subsistema, e são todo o acoplamento de declaração que resta no `Backend/`
- ⬜ **Metade de seleção** da abstração de interação: `ThreadSafeForm` e os diálogos
  `Select*`. Só fica visível ao censo depois que as declarações zerarem (DEC-032) — é
  acoplamento de corpo de método, e o compilador não o enxerga antes disso
- ⬜ Mover o `Backend/` inteiro de uma vez, quando o censo chegar a zero

> **O número do censo mudou de significado** (DEC-032). Ele conta erros de **declaração**;
> acoplamento dentro de corpos de método é invisível enquanto restar um erro de declaração
> no arquivo. A série 733 → 20 mede uma frente só. As 329 chamadas a `MessageBox`, as ~120
> instanciações de diálogo e os 146 `Application.DoEvents()` ainda não entraram na conta —
> e só entram quando esta primeira frente chegar a zero.
- ⬜ Testes existentes passando contra o `Chummer.Core`
- ⬜ **Reavaliar o plano na totalidade**

## Etapa 2.5 — APK esqueleto, em paralelo 🔄

**Não depende do `Chummer.Core`.** Existe para responder, com o aparelho na mão, as perguntas
de plataforma que estão em aberto desde a Etapa 1 (item 1.4, nunca feito) e que podem
invalidar decisões de arquitetura já tomadas.

- ✅ `Chummer.UI`, `Chummer.Android` e `Chummer.Desktop` criados e na `Chummer.Port.sln`
- ✅ `data/`, `lang/`, `sheets/` e `customdata/` embarcados como assets, por link (DEC-041)
- ✅ Tamanho do APK medido — e a premissa da etapa foi **invertida** (DEC-039)
- ✅ Tempo de carga e memória do XML medidos no desktop (controle)
- ✅ `XslCompiledTransform`: **a dependência dura foi identificada e medida** (DEC-037)
- ✅ A UI desenha, com PNG para provar (DEC-040)
- ✅ Job de CI que gera o APK e o publica como artefato
- ⬜ **PO instala no A56, abre e toca em "Medir"** (QA-009) — só ele pode fechar esta etapa
- ⬜ **Reavaliar o plano na totalidade** — depois dos números do aparelho

> **Por que sobe para cá.** Estas medições não competem com a Etapa 2 — são outra frente, e
> a resposta delas muda o plano das etapas 4, 6 e 7. Deixá-las para depois é carregar por
> mais tempo o risco de descobrir tarde que uma decisão de arquitetura não se sustenta no
> aparelho. E dá ao PO um APK instalável muito antes do MVP.

### O formato da entrega, e por que não é um "hello world"

A tela única do esqueleto **é o instrumento de medição**. Ela tem um botão "Medir" que roda
os spikes no aparelho e mostra os números. Um "hello world" provaria apenas que o APK
instala; este prova isso **e** traz de volta os números que só existem no aparelho.

O `Chummer.Desktop` roda exatamente as mesmas medições (`./scripts/dev.sh spikes`). Isso não
é conveniência: é o **controle**. Sem ele, um número ruim no celular não distingue "o Android
é lento" de "o código é lento".

### Onde o APK é construído — corrigido

Sai **deste contêiner**, em ~50 s de build incremental e 1 min 38 s do zero. A afirmação
anterior ("só no CI") caiu quando `setup-dev.sh --android` passou a descobrir a URL do
`commandline-tools` pelo índice do repositório. O job de CI continua existindo, mas para
**publicar o artefato ao PO**, não porque o contêiner não dê conta.

### Os números medidos · 2026-08-12

**Tamanho do APK** — três configurações, para separar o custo de cada coisa:

| Configuração | APK |
|---|---|
| arm64 + x86_64, com os dados | 31,10 MiB |
| arm64 + x86_64, sem os dados | 28,18 MiB |
| **arm64 apenas, com os dados** | **17,86 MiB** ← o que se distribui |

Composição do APK que se distribui:

| Grupo | Cru | Dentro do APK |
|---|---|---|
| `lib/arm64-v8a` (runtime .NET + Skia) | 24,98 MiB | **12,32 MiB** |
| dex, res, manifest | 6,75 MiB | 2,57 MiB |
| `assets/lang` (11 arquivos) | 9,52 MiB | 1,79 MiB |
| `assets/data` (42 arquivos) | 6,52 MiB | 0,63 MiB |
| `assets/customdata` (220 arquivos) | 1,41 MiB | 0,24 MiB |
| `assets/sheets` (174 arquivos) | 0,97 MiB | 0,14 MiB |
| **total** | 50,15 MiB | **17,69 MiB** |

> **O achado que inverte a premissa da etapa.** O plano tratava os "21 MB de dados" como o
> risco de tamanho. Eles somam 18,4 MiB crus e custam **2,80 MiB** dentro do APK — XML
> comprime ~6,6:1 e o zip do APK já faz isso. O custo real é a ABI `android-x64`, que
> ninguém usa fora de emulador e pesa 13,2 MiB: **quatro vezes e meia todos os dados
> juntos**. Ver DEC-039.

**Carga dos dados** — 42 arquivos de `data/`, cada um num `XmlDocument` mantido vivo, que é
o que o `XmlManager` legado faz. Medido no desktop (x64, .NET 9.0.18):

| | |
|---|---|
| bytes lidos | 6,52 MiB |
| nós XML | 343.678 |
| tempo total | 172–239 ms |
| tempo por MiB | 26–37 ms |
| **heap gerenciado retido** | **25,11 MiB** |
| alocado durante a carga | 40,84 MiB |
| **amplificação XML→memória** | **3,8×** |

> A amplificação de 3,8× é o número que importa para o Android, e é o que PREM-003 pedia.
> `XmlDocument` custa quase quatro vezes o tamanho do arquivo, **retidos para sempre**,
> porque o cache do `XmlManager` nunca solta. Só `data/` já pede 25 MiB; com `lang/` a conta
> cresce. Aparelhos antigos dão 64–128 MiB de `memoryClass` por app — o A56 dá bem mais, mas
> a margem não é infinita, e isto é argumento concreto para trocar `XmlDocument` por
> `XPathDocument` (que é imutável e bem mais enxuto) na Etapa 4.

**XSLT** — o achado mais importante da etapa. Ver DEC-037.

| | |
|---|---|
| `msxsl:script` nas folhas do repositório | **0** ✅ |
| `document()` nas folhas | **0** ✅ |
| cadeia de `xsl:import` resolvida sem sistema de arquivos | **sim** ✅ |
| `Shadowrun 5.xsl` — compilar com imports | 131–177 ms |
| `Shadowrun 5.xsl` — transformar | 517–604 ms |
| com `IsDynamicCodeSupported=false` | **`TypeInitializationException`** ❌ |

> `XslCompiledTransform` compila a folha para IL via `Reflection.Emit`. **Sem código
> dinâmico ele não degrada: ele explode**, no inicializador de `XmlILModule`, antes de ler a
> primeira folha. Consequência dura: **NativeAOT está fora do `Chummer.Android`** enquanto a
> impressão for por XSLT. O modo padrão do .NET para Android mantém o JIT e portanto deve
> funcionar — mas isso é dedução, e a medição no aparelho é QA-009.

**Build** — no contêiner, 4 CPUs:

| Operação | Tempo |
|---|---|
| `dev.sh build` da solução inteira (Release, incremental) | 42 s |
| `dotnet publish` do APK, do zero | 1 min 38 s |

## Etapa 3 — Artefatos dourados e teste diferencial ⬜

- ⬜ Job Windows que gera save + impressão dos 34 personagens com MSBuild do VS (DEC-015)
- ⬜ Artefatos versionados (PREM-001), normalizados e comprimidos
- ⬜ Job de comparação, com triagem do ruído esperado (ICU vs NLS, formatação de cultura)
- ⬜ **Reavaliar o plano na totalidade**

## Etapa 4 — Camada de plataforma e assets ⬜

- ⬜ `GlobalSettings` sobre armazenamento Android (fachada preservada, 6.448 acessos)
- ⬜ Assets: `data/`, `lang/` (pt-br + en-us), `sheets/`, `customdata/`
- ⬜ **Medição real em aparelho**: tempo de abertura e pico de memória (PREM-003 é 🟡)
- ⬜ **Reavaliar o plano na totalidade**

## Etapa 5 — Saneamento do modelo assíncrono ⬜

- ⬜ Eliminar `DoEvents` (146) e execução síncrona de async (279) nos caminhos de UI
- ⬜ Testes de concorrência e detecção de deadlock por timeout
- ⬜ **Reavaliar o plano na totalidade**

## Etapa 6 — MVP ⬜

Leitor/gerenciador de sessão, em Avalonia, rodando no `Chummer.Desktop` e no Android.

- ⬜ Abrir `.chum5`/`.chum5lz`, exibir o personagem completo
- ⬜ Editar: dano, Edge, karma, nuyen, munição, **magias sustentadas**, **rolagem de dados** (PREM-013)
- ⬜ Recarregar arma — o exercício do `IUserPrompt`
- ⬜ Salvar preservando o formato (PREM-010)
- ⬜ **APK instalável assim que abrir uma ficha, mesmo tosco** (PREM-018)
- ⬜ **Reavaliar o plano na totalidade**

## Etapa 7 — Fichas e impressão ⬜

- ⬜ XSLT → `WebView`, reaproveitando as 20+ folhas
- ⬜ **Reavaliar o plano na totalidade**

## Etapa 8 — Criação de personagem ⬜

Apenas o método **Prioridade** (PREM-012).

- ⬜ `SelectMetatypePriority` e o fluxo de construção
- ⬜ Os diálogos de seleção sobre o `IUserPrompt`
- ⬜ **Reavaliar o plano na totalidade**

## Etapa 9 — Build, assinatura e distribuição ⬜

- ⬜ Pipeline de APK/AAB, keystore
- ⬜ APK direto por release do GitHub (PREM-006)
- ⬜ **Reavaliar o plano na totalidade**

## Etapa 10 — Aceite ⬜

Os três marcos de PREM-019, nesta ordem:

- ⬜ **Fidelidade** — valores conferem com o desktop (QA-001)
- ⬜ **Uso real** — uma sessão inteira só com o app (QA-007)
- ⬜ **Completude** — personagem criado do zero no celular
- ⬜ **Reavaliar o plano na totalidade**

---

## O que "reavaliar o plano na totalidade" significa

Ao fim de cada etapa, antes de começar a seguinte:

1. **O que foi medido contradiz o que foi planejado?** Números novos derrubam estimativas
   velhas. Duas vezes já aconteceu neste projeto.
2. **Alguma etapa seguinte ficou desnecessária, menor ou maior?**
3. **Alguma premissa ativa deveria ser derrubada por evidência**, sem esperar o PO?
4. **A ordem ainda é a certa?** O que era barato pode ter ficado caro, e vice-versa.
5. **O risco mais alto ainda é o mesmo?**

O resultado é registrado no histórico abaixo e, quando muda o rumo, vira decisão em
`decisoes.md`.

---

## Histórico de reavaliações

### Após a Etapa 0
Carregar um `.chum5` instancia 30 tipos distintos do domínio. **Não existe fatia fina do
`Backend/`** — os ~350.000 LOC são custo obrigatório da primeira entrega. Consequência: o
MVP fatia a UI, nunca o núcleo (DEC-003).

### Após a Etapa 1
Duas mudanças de rumo:

- O censo mediu **733 erros**, mas 38% estão em arquivos que **não sobem para o núcleo**
  (infraestrutura de UI e telemetria). O trabalho real da Etapa 2 são 451 erros em 56
  arquivos — bem menos assustador que a estimativa da Etapa 0.
- O maior risco do projeto — o build legado não funcionar neste fork, o que destruiria a
  estratégia de artefatos dourados — foi **encerrado por evidência** (DEC-015).

### Durante a Etapa 2 (reavaliação antecipada)
O plano de "migrar primeiro os 171 arquivos limpos" **foi derrubado por medição**: compilados
isoladamente, os 171 produzem **368 erros**. Eles referenciam tipos que moram nos 74
arquivos sujos, ou seja, não formam um conjunto fechado por dependência.

"Limpo quando compilado junto com todo o `Backend/`" não é "movível sozinho" — e eu havia
confundido as duas coisas ao escrever DEC-014.

Nova estratégia: **consertar em lugar, mover uma vez** (DEC-016).

### Revisão estratégica de 2026-08-12 — "o que falta para um APK funcional"

Provocada pelo PO. Quatro achados, o primeiro deles desconfortável.

**1. A Etapa 2 está muito menos adiantada do que o número sugeria.** O censo saiu de 733
para 20, e isso parecia 97%. Não é. O censo mede **acoplamento de declaração**; o de corpo
de método é invisível para ele enquanto restar um erro de declaração (DEC-032). Medido agora,
o que ele nunca contou:

| | |
|---|---|
| `ThreadSafeForm` no `Backend/` (diálogos de seleção) | **327** |
| arquivos do `Backend/` ainda com `using System.Windows.Forms` | **27** |
| linhas ainda em `Chummer/Backend/` | **321.901** |
| linhas já em `src/Chummer.Core/` | **747** |

Ou seja: das ~322 mil linhas do domínio, **0,2% foram migradas**. O que se fez até aqui foi
preparar o terreno — separar metades de UI, criar a abstração de interação, consertar as
ferramentas. Trabalho necessário, mas a mudança de endereço mal começou.

**2. Existem dois "APK" diferentes, e confundi-los atrasa o projeto.**

- **APK esqueleto** — instala, abre, não lê ficha. **Não depende do `Chummer.Core`.**
  Alcançável em dias, e é o que responde os riscos de plataforma.
- **APK funcional** — abre um `.chum5` e mostra o personagem. Depende da Etapa 2 inteira,
  porque carregar um `.chum5` instancia 30 tipos do domínio e não existe fatia fina
  (achado da Etapa 0).

O plano só previa o segundo. O primeiro virou a **Etapa 2.5**, em paralelo.

**3. Os spikes da Etapa 1.4 nunca foram feitos, e são os de maior risco restante.** Tempo de
carga dos 21 MB, pico de memória, `XslCompiledTransform` no Android, tamanho do APK. São as
únicas perguntas abertas capazes de derrubar decisões de arquitetura já tomadas — a de XSLT
sozinha define se a Etapa 7 existe como planejada. Estavam agendadas para "quando der"; agora
são a Etapa 2.5.

**4. A ordem das etapas 3 e 5 não precisa preceder o primeiro APK.** Artefatos dourados
(Etapa 3) são rede de segurança para a **mudança de comportamento**; saneamento assíncrono
(Etapa 5) é para não dar ANR. Nenhum dos dois é pré-requisito para um APK **existir**. Ficam
onde estão, mas deixam de bloquear a Etapa 2.5.

**Caminho crítico, em uma linha:** retratos → destrava a medição → metade de seleção (os 327)
→ mover 322 mil linhas → UI Avalonia → APK funcional. Em paralelo, e independente: Etapa 2.5.

### Durante a Etapa 2.5 (reavaliação parcial) — 2026-08-12

Parcial de propósito: a reavaliação completa só é honesta depois que o PO rodar QA-009 no
aparelho. Mas três medições já mudaram coisas, e segurá-las até lá não ajuda ninguém.

**1. O risco de tamanho estava no lugar errado.** O plano vinha tratando os "21 MB de
`data/`" como o problema de empacotamento. São 2,80 MiB dentro do APK. O custo real era uma
ABI de emulador, 13,2 MiB, que ninguém tinha olhado. APK final: 17,86 MiB — confortável.
**Nenhuma etapa precisa mudar por causa de tamanho.**

**2. A Etapa 4 ganha um item concreto que ela não tinha.** A amplificação de 3,8× do
`XmlDocument` (25,11 MiB retidos para 6,52 MiB de arquivo) é grande, e o cache do
`XmlManager` nunca solta. Trocar `XmlDocument` por `XPathDocument` no caminho de leitura sai
de "otimização se sobrar tempo" para item da etapa: o Chummer **lê** os dados de jogo, não
os edita, e o `XPathDocument` representa a mesma árvore em uma fração da memória.

**3. A Etapa 7 continua de pé, mas com uma restrição nova e permanente.** As folhas XSLT não
usam nada que o .NET moderno não suporte, e a cadeia de imports foi provada resolúvel a
partir dos assets — o que era o risco imaginado. O risco real é outro e não tinha sido
previsto: `XslCompiledTransform` **exige código dinâmico e explode sem ele**. Consequência
que atravessa todas as etapas seguintes: **NativeAOT está fora do `Chummer.Android`** e
precisa continuar fora. Fica registrado em DEC-037 para que ninguém o ligue mais adiante
"para ganhar desempenho" e quebre a impressão de fichas sem entender por quê.

**O maior risco ainda é o mesmo?** Não. O maior risco do projeto continua sendo a Etapa 2 —
mover 322 mil linhas — e nada aqui mudou isso. Mas o maior risco *de plataforma*, que era
"a arquitetura escolhida não se sustenta no aparelho", encolheu para uma única pergunta
binária com resposta a caminho.
