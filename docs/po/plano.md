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
- ⬜ 1.4 Spikes de risco: `XslCompiledTransform` no Android · carga dos 21 MB · tamanho do APK
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

## Etapa 2.5 — APK esqueleto, em paralelo ⬜

**Não depende do `Chummer.Core`.** Existe para responder, com o aparelho na mão, as perguntas
de plataforma que estão em aberto desde a Etapa 1 (item 1.4, nunca feito) e que podem
invalidar decisões de arquitetura já tomadas.

- ⬜ Projeto `Chummer.Android` com Avalonia, tela única, nada de domínio
- ⬜ Os 21 MB de `data/` embarcados como assets — **medir tempo de carga e pico de memória**
- ⬜ `XslCompiledTransform` rodando no Android — se não rodar, a Etapa 7 muda inteira
- ⬜ Tamanho do APK com os assets dentro
- ⬜ Job de CI que gera o APK e o publica como artefato
- ⬜ **PO instala no A56 e confirma que abre** (PREM-018)
- ⬜ **Reavaliar o plano na totalidade**

> **Por que sobe para cá.** Estas medições não competem com a Etapa 2 — são outra frente, e
> a resposta delas muda o plano das etapas 4, 6 e 7. Deixá-las para depois é carregar por
> mais tempo o risco de descobrir tarde que uma decisão de arquitetura não se sustenta no
> aparelho. E dá ao PO um APK instalável muito antes do MVP.
>
> **Onde o APK é construído:** no CI. Medido em 2026-08-12 — o workload .NET de Android
> instala neste contêiner, mas o SDK do Google não: `InstallAndroidDependencies` falha com
> `XAIAD7009` e o download direto do `commandline-tools` responde 404. O runner
> `ubuntu-latest` já traz o SDK.

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
