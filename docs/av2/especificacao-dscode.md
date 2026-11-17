# Especificação para o agente DsCode — Reabastecer anúncio esgotado

Pedido de alteração funcional escrito para ser executado por um agente de
código (DsCode), com contexto, limites e critérios verificáveis. A seção 8
registra a execução e a inspeção do resultado.

## 1. Contexto para o agente

- Projeto: RarityRoom (marketplace de colecionáveis). Backend ASP.NET Core 10
  em `backend/`, testes xUnit em `tests/RarityRoom.Tests/`, frontend React em
  `frontend/`.
- Arquitetura: `Controller → Services → Models (domínio) → Repositories`.
  **Regra de negócio fica no domínio**; o controller só traduz HTTP.
- O anúncio é `Produto` (`backend/Models/Produto.cs`). Estados:
  `Disponivel`, `Reservado`, `Vendido`, `Pausado`. A venda aprovada baixa o
  estoque em `Produto.BaixarEstoque`; ao chegar a zero o anúncio vira
  `Vendido`.
- Erros de negócio são exceções de `backend/Models/Excecoes`, traduzidas em
  ProblemDetails pelo `MiddlewareExcecoes` (400 `dados-invalidos`,
  403 `acesso-negado`, 404, 409 `conflito`/`transicao-invalida`).

## 2. Problema

Um anúncio esgotado fica `Vendido` para sempre: `Produto.Atualizar` recusa
editar anúncio vendido, então o vendedor não consegue repor unidades de um item
que voltou a ter em mãos. A limitação estava declarada no relatório da sessão
anterior.

## 3. Objetivo

Permitir que o **dono** reponha unidades de um anúncio, por uma operação
própria de domínio, sem abrir a edição geral de anúncios vendidos (RN03 continua
valendo para os demais campos).

## 4. Regras a implementar (RN-REAB)

| # | Condição | Resultado esperado |
|---|---|---|
| R1 | Dono, anúncio `Vendido`, quantidade entre 1 e 1000 | estoque += quantidade; estado → `Disponivel` |
| R2 | Dono, anúncio `Disponivel` ou `Pausado` | estoque += quantidade; estado **não muda** |
| R3 | Anúncio `Reservado` (proposta aceita em andamento) | recusar com `TransicaoInvalidaException` (409); estado e estoque inalterados |
| R4 | Quantidade ≤ 0 ou > 1000 | recusar com `ValidacaoException` (400); nada muda |
| R5 | Quem não é o dono | recusar com `AcessoNegadoException` (403); nada muda |

## 5. Escopo

**Dentro**
1. `Produto.Reabastecer(int quantidade)` — regra R1–R4 no domínio.
2. `IServicoAnuncios`/`ServicoAnuncios.Reabastecer(id, quantidade, solicitanteId)`
   — busca, `ValidarPropriedade` (R5), chama o domínio, persiste.
3. `POST /api/produtos/{id}/reabastecer` com corpo `{ "quantidade": n }`,
   `[Authorize]`, devolvendo o anúncio atualizado.
4. Frontend: botão **Repor estoque** na área "Vender → Meus anúncios",
   pedindo a quantidade.
5. Testes automatizados de sucesso e de violação para R1–R5.

**Fora** (não alterar)
- Regras de checkout, carrinho e pagamento.
- `Produto.Atualizar` continua recusando editar anúncio vendido (RN03).
- Nenhuma migração: a operação usa colunas existentes.

## 6. Critérios de aceitação (verificáveis)

| ID | Dado | Quando | Então |
|---|---|---|---|
| CA1 | anúncio do usuário A, `Vendido`, estoque 0 | A reabastece 5 | 200; estoque 5; estado `Disponivel`; volta a aparecer no catálogo |
| CA2 | anúncio `Pausado`, estoque 1 | dono reabastece 2 | estoque 3; estado `Pausado` |
| CA3 | anúncio `Reservado` | dono reabastece 1 | 409 `transicao-invalida`; estoque e estado iguais |
| CA4 | qualquer anúncio | quantidade 0 ou 1001 | 400 `dados-invalidos`; nada muda |
| CA5 | anúncio de A | usuário B reabastece | 403 `acesso-negado`; nada muda |
| CA6 | suíte completa | `dotnet test` | 100% aprovada, sem regressão |

## 7. Plano pedido ao agente

1. Escrever primeiro os testes de domínio (R1–R4) e vê-los falhar.
2. Implementar `Produto.Reabastecer`.
3. Serviço + controller + teste de API cobrindo CA1, CA3 e CA5 com releitura
   pela API (persistência).
4. Botão no frontend; `npx tsc` e `npm run build` sem erro.
5. Rodar a suíte completa e anexar a saída.
6. Entregar commits pequenos, um por passo.

## 8. Execução e inspeção do resultado

*(preenchida após a execução — ver commits seguintes no histórico Git)*
