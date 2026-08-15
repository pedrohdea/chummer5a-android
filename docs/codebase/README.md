# Documentação da Base de Código — Chummer5a

Documentação do estado atual do código, produzida como **Etapa 0** do projeto de porte
para Android. O objetivo é descrever o que existe hoje, com precisão e sem julgamento,
para que as etapas de refatoração partam de um mapa confiável em vez de suposições.

## Como navegar

| # | Documento | Público |
|---|---|---|
| 01 | [Visão Geral](01-visao-geral.md) | **Não técnico** — o que é o Chummer, quem usa, o que faz |
| 02 | [Glossário](02-glossario.md) | **Não técnico** — termos de Shadowrun e do aplicativo |
| 03 | [Arquitetura](03-arquitetura.md) | Técnico — mapa de projetos, camadas, números |
| 04 | [Modelo de Domínio](04-modelo-dominio.md) | Técnico — `Character` e os tipos do domínio |
| 05 | [Sistema de Dados XML](05-sistema-dados.md) | Técnico — `data/`, `XmlManager`, custom data |
| 06 | [Motor de Regras (Improvements)](06-motor-regras.md) | Técnico — como bônus e modificadores funcionam |
| 07 | [Concorrência e Notificação](07-concorrencia.md) | Técnico — async, locks, grafo de dependências |
| 08 | [Interface WinForms](08-interface.md) | Técnico — telas, controles, padrões de UI |
| 09 | [Fichas e Exportação](09-fichas-exportacao.md) | Técnico — XSLT, impressão, PDF |
| 10 | [Localização](10-localizacao.md) | Técnico — sistema de tradução |
| 11 | [Projetos Satélite](11-projetos-satelite.md) | Técnico — Hub, plugins, utilitários |
| 12 | [Build, CI e Dependências](12-build-ci.md) | Técnico — como se compila hoje |
| 13 | [Acoplamento à Plataforma](13-acoplamento-plataforma.md) | Técnico — **inventário Windows-only** |
| 14 | [Censo de Erros](14-censo-de-erros.md) | Técnico — **medição da frente de DECLARAÇÃO: 733 erros, hoje zerada** |
| 15 | [Acoplamento de Corpo de Método](15-acoplamento-de-corpo.md) | Técnico — **a outra metade: 1.584 erros que só apareceram quando as declarações zeraram** |

## Documentação que já existia no projeto

Esta documentação **não substitui** o que o upstream já mantinha. Duas fontes
continuam válidas e são referenciadas ao longo dos documentos:

- **`Chummer/docs/wiki/`** — 29 arquivos sobre *como escrever dados customizados*
  (`Armor.md`, `Gear.md`, `Weapons.md`, `Custom-Data-Files.md`,
  `Improvement-Manager.md`, `Developer-Guide.md`, …). É a referência de autoria de
  conteúdo de jogo, não de código.
- **`Chummer/docs/XPathConditionSystem.md`** e **`ComparisonOperators.md`** — o sistema
  de condições dos improvements.
- **`Chummer/Documentation/Lifemodule.md`** — o sistema de Life Modules.

## Escopo e limites

Esta documentação descreve o código **no commit em que foi escrita**. O projeto é um
*hard fork* de `chummer5a/chummer5a`, sem divergência de código no momento da redação.
Os números (contagens de linhas, arquivos, ocorrências) foram medidos diretamente na
árvore e estão sujeitos a variação conforme o código evolui.

Onde há incerteza — comportamento que exigiria execução para confirmar — o texto diz
explicitamente que é uma leitura estática, não um fato verificado em runtime.
