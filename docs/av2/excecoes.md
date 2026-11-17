# Controle de exceções — RarityRoom

## 1. Três situações de falha (D01)

Nem todo problema é exceção, e nem toda exceção é "erro do sistema". O
RarityRoom separa três categorias:

| # | Situação | Categoria | Mecanismo | Comportamento esperado |
|---|---|---|---|---|
| S1 | Comprador pede mais unidades do que o estoque | **Violação de regra de negócio** (esperada, faz parte do domínio) | `RegrasCompra.Validar` lança `ConflitoException` | HTTP **409** `codigo: conflito`, mensagem de negócio ("Estoque insuficiente… há 3 unidade(s)…"); carrinho **inalterado** |
| S2 | Dados do cartão inválidos (Luhn, validade, CVV, CPF) | **Entrada inválida** (validação) | `ValidadorCartao.Validar` lança `ValidacaoException` com todas as mensagens | HTTP **400** `codigo: dados-invalidos`; a compra continua `Pendente` para o comprador corrigir |
| S3 | Falha inesperada de infraestrutura (ex.: banco inacessível) | **Situação excepcional** (não prevista pelo domínio) | qualquer `Exception` fora da hierarquia de domínio | HTTP **500** `codigo: erro-interno` com mensagem genérica; **detalhes só no log** do servidor |

Casos que **não** são exceção: **cartão recusado** pela simulação
(`4000 0000 0000 0002`) é um resultado normal do negócio. O serviço registra
`Pagamento.Recusar(...)`, devolve **200** com `status: "Recusado"` e o
frontend mostra o resultado. Também não usa exceção: item do carrinho que
ficou sem estoque depois de adicionado. `GET /api/carrinho` devolve
`disponivel: false` e um `aviso` na linha, e `podeFinalizar: false`.

Uma quarta situação, excepcional mas **prevista**: duas operações gravando o
mesmo estoque ao mesmo tempo (`DbUpdateConcurrencyException`). Ela é tratada
como 409 `concorrencia` ("atualize e tente novamente").

## 2. Caminho de lançamento → propagação → tratamento (D02)

Caminho reproduzível de S1 na **aprovação do pagamento**, com o estoque
alterado depois do checkout:

```
PagamentosController.SimularConfirmacao          (não captura nada)
 └─ ServicoPedidos.SimularConfirmacao
     └─ IUnidadeDeTrabalho.Executar(...)          ← BEGIN IMMEDIATE
         └─ ServicoPedidos.Aprovar
             └─ Produto.BaixarEstoque(quantidade)
                 └─ throw new ConflitoException("Estoque insuficiente…")   ← lançamento
         ← UnidadeDeTrabalhoEf: catch → Rollback() → ChangeTracker.Clear() → throw;   ← propagação sem engolir
 ← MiddlewareExcecoes: catch (ExcecaoDominio) → 409 ProblemDetails + log Warning   ← tratamento
```

Pontos de projeto:

- **Lançamento** onde a regra vive (`Produto`), antes de alterar o campo: o
  objeto não fica pela metade.
- **Propagação**: a única captura intermediária é a da unidade de trabalho,
  que **desfaz a transação e relança** (`throw;`). Assim o banco não fica
  inconsistente e a exceção não é escondida.
- **Tratamento** num lugar só, `backend/Middleware/MiddlewareExcecoes.cs`,
  o primeiro middleware do pipeline. Os controllers não têm `try/catch`.

Na prática, `Aprovar` primeiro **revalida** o estoque e, se faltar, encerra a
compra como `Cancelado` com motivo (resultado de negócio, sem exceção). A
exceção de `BaixarEstoque` fica como **última barreira**, se a revalidação
fosse contornada. O caminho com exceção é exercitado por
`S1_RegraDeNegocio_EstoqueInsuficiente_Retorna409SemAlterarCarrinho`
(lançada em `RegrasCompra.Validar` dentro de `ServicoCarrinho.Adicionar`,
sem captura intermediária, tratada pelo mesmo middleware) e pelo defeito documentado em
`debugging.md`. Nesse defeito, uma exceção do EF lançada dentro da transação
percorreu exatamente esse caminho: rollback, middleware, 500. O banco ficou
com **0** pagamentos e **0** pedidos, conforme a evidência
`docs/av2/evidencias/depuracao/02-hipoteses-e-diagnostico.txt`.

## 3. Falha excepcional tratada sem ocultar e sem vazar (D03)

`MiddlewareExcecoes.InvokeAsync`:

```csharp
catch (Exception excecao)
{
    logger.LogError(excecao, "Erro não tratado em {Metodo} {Caminho}", ...);   // não oculta
    await EscreverAsync(contexto, 500, "erro-interno",
        "Ocorreu um erro inesperado. Tente novamente.");                     // não vaza
}
```

- **Não oculta:** a exceção completa (tipo, mensagem, stack trace) vai para o
  log com nível `Error`.
- **Não vaza:** o corpo da resposta tem só `title`, `status`, `detail`
  genérico, `instance` e `codigo`. Não inclui tipo da exceção, mensagem
  interna, stack trace nem nome de servidor.

Prova real (log de produção do defeito investigado):
`docs/av2/evidencias/depuracao/01-log-servidor-com-defeito.txt`. O cliente
recebeu `{"title":"Erro interno","status":500,"detail":"Ocorreu um erro inesperado. Tente novamente.",...}`
e o log registrou `System.InvalidOperationException: The property 'DadosEntrega.PedidoId'…`
com o stack trace.

## 4. Testes das três situações (D04)

`tests/RarityRoom.Tests/Api/FalhasEInvariantesApiTests.cs` (API real,
SQLite temporário):

| Situação | Teste | O que verifica |
|---|---|---|
| S1 | `S1_RegraDeNegocio_EstoqueInsuficiente_Retorna409SemAlterarCarrinho` | 409, `codigo: conflito`, mensagem de estoque **e** carrinho vazio depois da recusa (estado preservado) |
| S2 | `S2_EntradaInvalida_CartaoInvalido_Retorna400ECompraContinuaAguardando` | 400, `codigo: dados-invalidos` **e** compra ainda `Pendente` |
| S3 | `S3_FalhaInesperada_Retorna500GenericoSemVazarDetalhes_ERegistraNoLog` | troca o repositório de pagamentos por um que lança `InvalidOperationException("…srv-banco-interno:5432…")`; verifica 500, `codigo: erro-interno`, mensagem genérica, que o corpo **não** contém a mensagem interna, o tipo da exceção nem stack trace, **e** que o log recebeu um `Error` com a exceção original |

Complementares: `Cartao_AprovadoGuardaSoFinal_ERecusadoNaoMexeNoEstoque`
(recusa como resultado de negócio, 200) e
`ComprasSimultaneas_DoMesmoEstoque_SoUmaEhAprovada` (concorrência, 409).

## 5. Hierarquia

`backend/Models/Excecoes/`: `ExcecaoDominio` (abstrata, com `Codigo`) →
`ValidacaoException` (400), `AcessoNegadoException` (403),
`NaoEncontradoException` (404), `ConflitoException` (409) →
`TransicaoInvalidaException` (409) e `PagamentoRecusadoException` (409). O
middleware avalia a especialização (`TransicaoInvalidaException`) antes da
base (`ConflitoException`).
