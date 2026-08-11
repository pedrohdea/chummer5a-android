# Pasta do PO

Espaço de coordenação entre o PO/QA e o programador, desenhado para **trabalho paralelo**:
o PO não precisa estar disponível para o desenvolvimento avançar, e o desenvolvimento não
precisa parar para perguntar.

## Os quatro arquivos

| Arquivo | O que é | Quem escreve |
|---|---|---|
| [`pendencias.md`](pendencias.md) | perguntas abertas que só o PO responde | programador registra · PO responde |
| [`premissas.md`](premissas.md) | o que foi assumido para não travar | programador |
| [`qa-roteiro.md`](qa-roteiro.md) | o que precisa de teste humano | programador acumula · PO executa |
| [`decisoes.md`](decisoes.md) | decisões técnicas tomadas e o porquê | programador |

## A regra que faz isso funcionar

> **Pendência nunca bloqueia o trabalho.**

Sempre que aparece uma pergunta cuja resposta é do PO, o programador:

1. registra a pendência em `pendencias.md`;
2. **escolhe a resposta mais provável** e registra em `premissas.md`, com o que muda se
   estiver errada e qual o custo de reverter;
3. **segue trabalhando** sob essa premissa.

Quando o PO responde, uma de duas coisas acontece: a premissa é confirmada e vira decisão,
ou é derrubada e o programador refaz — sabendo de antemão o custo, porque ele foi estimado
no momento em que a premissa foi criada.

A única exceção: premissa cujo custo de reversão é **alto** é sinalizada com 🔴 e o
programador evita construir por cima dela até haver resposta, trabalhando em outra frente
enquanto isso.

## Como o PO responde

Edite o arquivo direto (mude o status para `RESPONDIDA` e escreva a resposta), ou responda
no chat — o programador transcreve. As duas formas valem.

## Convenções

- IDs são estáveis e nunca reaproveitados: `PEND-001`, `PREM-001`, `QA-001`, `DEC-001`.
- Itens resolvidos **não são apagados** — mudam de status e vão para o fim do arquivo. O
  histórico é o que evita redecidir a mesma coisa daqui a três meses.
- Datas em `AAAA-MM-DD`.

## Custo de reversão

Escala usada em `premissas.md`:

| Marca | Significado |
|---|---|
| 🟢 baixo | trocar um valor de configuração ou algumas linhas |
| 🟡 médio | refazer um componente; horas a poucos dias |
| 🔴 alto | decisão estrutural; retrabalho de semanas — **não construir por cima sem resposta** |
