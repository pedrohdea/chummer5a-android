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
  - ⬜ Retratos como bytes (DEC-021)
  - ⬜ `Character.cs` (63), equipamento (~80), `LanguageManager` (18), `Utils` (13)
- ⬜ Mover o `Backend/` inteiro de uma vez, quando o censo chegar a zero
- ⬜ Testes existentes passando contra o `Chummer.Core`
- ⬜ **Reavaliar o plano na totalidade**

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
