# 01 — Visão Geral (não técnico)

## O que é o Chummer

O Chummer 5 é um programa para **criar e gerenciar fichas de personagem** do RPG de mesa
*Shadowrun, 5ª edição*. Ele faz duas coisas bem distintas, e entender essa divisão explica
quase toda a estrutura do programa:

1. **Criação de personagem** — um processo longo, cheio de regras, em que o jogador gasta
   pontos para comprar atributos, perícias, magias, implantes cibernéticos, armas,
   veículos e contatos. O programa impede escolhas ilegais e calcula custos.
2. **Uso durante o jogo** — depois que o personagem existe, o jogador o usa em sessões:
   marca dano, gasta munição, ganha karma e dinheiro, compra equipamento novo, evolui
   perícias. O programa vira uma planilha viva.

O programa distingue esses dois momentos internamente. Um personagem está em modo
**Create** (ainda sendo construído) ou em modo **Career** (já em jogo). As regras mudam
entre os dois: no Create você distribui pontos de construção; no Career você gasta karma
ganho jogando.

## Por que ele é complicado

Shadowrun 5ª edição é um sistema de regras notoriamente denso, publicado em dezenas de
livros ao longo de anos, com erratas, regras opcionais e contradições. O Chummer tenta
suportar **tudo isso ao mesmo tempo**, e de forma configurável.

Três consequências práticas:

- **Nada é fixo no código.** Armas, magias, implantes, qualidades — tudo vem de arquivos
  de dados XML que ficam em `Chummer/data/`. São 42 arquivos, cerca de 7 MB. Adicionar um
  livro novo ao jogo é adicionar dados, não programar.
- **Cada mesa joga diferente.** O programa tem um sistema de *rulesets* (conjuntos de
  regras) em que o mestre escolhe quais livros valem, quais regras opcionais estão
  ligadas, quantos pontos os jogadores recebem. Vêm 57 pacotes de modificação prontos em
  `Chummer/customdata/` — coisas como "Bone Lacing Adds to Body" ou "Critter Prices".
- **Tudo afeta tudo.** Um implante pode aumentar um atributo, que muda um limite, que
  muda um teste, que muda uma perícia. O programa tem um motor inteiro só para propagar
  esses efeitos em cadeia (ver [06 — Motor de Regras](06-motor-regras.md)).

## O que o usuário vê

O aplicativo é uma janela Windows clássica com abas. O fluxo típico:

1. **Tela inicial (Character Roster)** — lista dos personagens salvos, com favoritos e
   recentes.
2. **Criar ou abrir um personagem** — ao criar, o usuário escolhe o *método de construção*
   (Prioridade, Soma-para-Dez, Karma ou Life Modules) e a metatipo (humano, elfo, anão,
   orc, troll…).
3. **Abas de edição** — o personagem aberto tem abas: Atributos e Perícias, Magia/Ressonância,
   Qualidades, Estilo de Vida, Cyberware/Bioware, Armas, Armadura, Equipamento, Veículos,
   Contatos, Anotações, Melhorias, e uma aba de personagem/descrição.
4. **Compras** — cada aba tem botões de "Adicionar", que abrem uma janela de seleção com
   catálogo, busca e filtro. São **45 janelas de seleção diferentes** no programa.
5. **Ficha impressa** — um visualizador que gera a ficha formatada para impressão ou PDF,
   com mais de 20 layouts diferentes à escolha.

Além disso existem utilitários: rolador de dados, rastreador de iniciativa, painel para
o mestre acompanhar vários personagens, importador de fichas do Hero Lab (outro programa
concorrente), e um índice pesquisável de todo o conteúdo de regras.

## Idiomas

O programa é traduzido para **inglês, alemão, francês, japonês, português do Brasil e
chinês simplificado**. A tradução tem duas camadas: os textos da interface (~2.771 frases)
e a tradução do próprio conteúdo de jogo (nomes de armas, magias, etc.), em arquivos
separados. Ver [10 — Localização](10-localizacao.md).

## Onde os dados do usuário ficam

- **Personagens** são arquivos `.chum5` (XML) ou `.chum5lz` (o mesmo XML comprimido).
  O usuário escolhe onde salvar.
- **Configurações do programa** ficam no **Registro do Windows** — não em arquivo. Isso
  tem consequência direta para o porte.
- **Rulesets customizados** ficam na pasta `settings/`.
- **Dados customizados** ficam em `customdata/`.

## O ecossistema em volta

O repositório contém mais do que o aplicativo:

- **ChummerHub** — um serviço web (ASP.NET) para compartilhar personagens online.
- **Plugins** — sistema de extensões carregadas dinamicamente; o principal é o cliente do
  ChummerHub.
- **Translator** — ferramenta separada para a comunidade criar traduções.
- **CrashHandler** — captura e reporta falhas.
- **ChummerDataViewer** — visualizador dos relatórios de falha coletados.

Ver [11 — Projetos Satélite](11-projetos-satelite.md).

## Licença e conteúdo

O código é **GPL-3.0**. O conteúdo de regras de Shadowrun é propriedade de terceiros
(Catalyst Game Labs / Topps). Os arquivos de dados contêm estatísticas de jogo, mas não o
texto dos livros — cada item traz uma referência de página (`source` e `page`) para o
usuário consultar no livro que ele possui. Essa distinção é intencional e importa para
qualquer distribuição do aplicativo.
