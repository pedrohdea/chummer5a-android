# 02 — Glossário

Ponte entre o vocabulário de *Shadowrun* e os nomes que aparecem no código. Quem for
mexer no `Backend/` precisa deste vocabulário: os identificadores do código usam os termos
do jogo em inglês, sem tradução.

## Termos do jogo

| Termo | O que é | Onde aparece no código |
|---|---|---|
| **Attribute** | Os 8 atributos base (Corpo, Agilidade, Reação, Força, Carisma, Intuição, Lógica, Vontade) mais os especiais | `Backend/Attributes/`, siglas `BOD AGI REA STR CHA INT LOG WIL` |
| **Edge** | Atributo de sorte, gasto e recuperado | sigla `EDG` |
| **Magic / Resonance / Depth** | Atributos especiais de personagens mágicos, technomancers e AIs | `MAG`, `MAGAdept`, `RES`, `DEP` |
| **Essence** | Quanto de humanidade resta; implantes consomem Essence | `ESS` |
| **Karma** | Moeda de experiência: constrói e evolui o personagem | `Karma`, `CareerKarma` |
| **Nuyen** | Dinheiro do cenário | `Nuyen`, `NuyenString`, `ExpenseLogEntry` |
| **Metatype** | Espécie: humano, elfo, anão, orc, troll e variantes | `data/metatypes.xml` (780 KB) |
| **Quality** | Vantagens e desvantagens (positivas custam karma, negativas devolvem) | `Backend/Uniques/Quality.cs`, `data/qualities.xml` |
| **Skill / Skill Group / Specialization** | Perícias, agrupamentos e especializações | `Backend/Skills/` |
| **Knowledge Skill** | Perícias de conhecimento, com regras próprias | `KnowledgeSkill.cs` |
| **Exotic Skill** | Perícias que exigem qualificador obrigatório | `ExoticSkill.cs` |
| **Cyberware / Bioware** | Implantes mecânicos e biológicos | `Backend/Equipment/Cyberware.cs` |
| **Grade** | Qualidade do implante (Standard, Alphaware, Betaware, Deltaware, Used…), afeta custo e Essence | nós `<grades>` em `data/cyberware.xml` |
| **Gear** | Equipamento genérico, aninhável (um item dentro de outro) | `Backend/Equipment/Gear.cs`, `data/gear.xml` (640 KB) |
| **Availability** | Dificuldade legal de conseguir um item | `Backend/Datastructures/AvailabilityValue.cs` |
| **Lifestyle** | Padrão de vida mensal | `Backend/Equipment/Lifestyle.cs` |
| **Condition Monitor** | Trilhas de dano Físico e de Atordoamento | `PhysicalCM`, `StunCM` |
| **Initiative** | Ordem de ação em combate | `Initiative`, `InitiativeDice`, `MatrixInitiative` |
| **Limit** | Limites Físico, Mental e Social que restringem sucessos | `LimitTabUserControl`, `Backend/Uniques/LimitModifier.cs` |
| **Adept / Magician / Mystic Adept** | Arquétipos mágicos | `AdeptEnabled`, `MagicianEnabled` |
| **Power** | Poderes de adepto | `Backend/Uniques/Power.cs` |
| **Spell / Tradition** | Magias e tradição mágica | `data/spells.xml`, `data/traditions.xml` |
| **Spirit** | Espíritos invocados | `Backend/Uniques/Spirit.cs` |
| **Technomancer** | Personagem que manipula a Matrix organicamente | `TechnomancerEnabled` |
| **Complex Form / Sprite / Echo** | Equivalentes technomânticos de magias, espíritos e metamagias | `data/complexforms.xml`, `data/echoes.xml` |
| **Metamagic** | Aprimoramentos de iniciação mágica | `Backend/Uniques/Metamagic.cs` |
| **Initiation / Submersion** | Progressão avançada mágica / technomântica | `InitiationGrade` |
| **Matrix** | A rede; itens têm atributos de Matrix (Attack, Sleaze, Data Processing, Firewall) | espalhado em `Equipment/` |
| **Critter** | Criaturas e NPCs não-metahumanos | `data/critters.xml` (940 KB, o maior arquivo) |
| **Contact** | NPCs aliados, com Conexão e Lealdade | `Backend/Uniques/Contact.cs` |
| **Martial Art / Technique** | Artes marciais e suas técnicas | `Backend/Uniques/MartialArt*.cs` |
| **Mentor Spirit** | Entidade patrona que concede bônus e restrições | `Backend/Uniques/MentorSpirit.cs` |
| **PACKS Kit** | Pacote pré-montado de equipamento | `data/packs.xml` |
| **Life Module** | Método de criação por etapas biográficas | `data/lifemodules.xml`, `Chummer/Documentation/Lifemodule.md` |
| **Drain / Fading** | Desgaste de conjurar magia / usar formas complexas | propriedades em `Character.cs` |

## Termos do aplicativo

| Termo | Significado |
|---|---|
| **Create mode** | Personagem ainda em construção. `Character.Created == false`. |
| **Career mode** | Personagem finalizado e em jogo. `Character.Created == true`. A transição é de mão única. |
| **Build Method** | Como o personagem é construído. Enum `CharacterBuildMethod`: `Karma`, `Priority`, `SumtoTen`, `LifeModule`. |
| **Character Settings** | O *ruleset*: quais livros valem, regras opcionais, orçamentos. Um personagem carrega uma referência ao seu ruleset. Ver `Backend/Character Settings/CharacterSettings.cs`. |
| **Global Settings** | Preferências do aplicativo (idioma, cores, pastas). Persistidas no **Registro do Windows**. |
| **Custom Data Directory** | Pasta com XML que modifica ou acrescenta conteúdo de jogo. 57 pacotes vêm inclusos em `customdata/`. |
| **Improvement** | A unidade atômica de "algo modifica algo". O conceito central das regras — ver [06](06-motor-regras.md). |
| **Bonus** | O nó XML que, ao ser processado, gera Improvements. |
| **Amend** | Mecanismo de custom data que altera nós já existentes em vez de acrescentar. |
| **Source / Page** | Referência bibliográfica de cada item (sigla do livro + página). |
| **Book** | Livro-fonte. O ruleset liga e desliga livros; itens de livros desligados somem. |
| **Sheet** | Layout de ficha impressa (`Chummer/sheets/*.xsl`). |
| **Hub** | O serviço web de compartilhamento (ChummerHub). |
| **Chummer** | Gíria do cenário para "camarada". É o nome do programa e o tratamento do usuário. |
| **`.chum5` / `.chum5lz`** | Arquivo de personagem: XML puro / XML comprimido com LZMA. |

## Convenções de nomenclatura do código

O código segue uma notação húngara consistente que **precisa ser lida corretamente** para
navegar no `Backend/`:

| Prefixo | Tipo |
|---|---|
| `str` | `string` |
| `int` | `int` |
| `dec` | `decimal` |
| `bln` | `bool` |
| `obj` | objeto/referência |
| `lst` | lista |
| `dic` | dicionário |
| `set` | conjunto |
| `xml` / `xpath` | nó XML / navegador XPath |
| `frm` | formulário WinForms |
| `e` (enum) | valor de enumeração |
| `s_` | campo estático |
| `_` | campo de instância |

Exemplo real: `lstEnabledCustomDataPaths` é uma lista de strings; `blnSync` é um booleano
que seleciona execução síncrona; `objCharacter` é a referência ao personagem.
