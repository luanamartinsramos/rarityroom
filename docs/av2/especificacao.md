# Especificação verificável — RarityRoom (AV2)

Especificação dos **três fluxos** e das **cinco regras de negócio** principais,
com condições, estados e critérios de aceitação. Cada critério aponta para o
código que o implementa e para os testes automatizados que o verificam
(`tests/RarityRoom.Tests`). Execute `dotnet test` em `tests/RarityRoom.Tests`
para conferir.

Convenções: **FL** = fluxo, **RN** = regra de negócio, **CA** = critério de
aceitação. Erros de negócio respondem `ProblemDetails` com o campo `codigo`
(`dados-invalidos` 400, `acesso-negado` 403, `nao-encontrado` 404,
`conflito`/`transicao-invalida` 409, `erro-interno` 500).

---

## 1. Fluxos

### FL1 — Publicar e gerenciar um anúncio (vendedor)

| | |
|---|---|
| Ator | usuário autenticado (vendedor) |
| Pré-condição | sessão válida (cookie `RarityRoom.Auth`); categoria cadastrada |
| Passos | 1. `POST /api/produtos` cria o anúncio (`Disponivel`) · 2. `PUT /api/produtos/{id}` edita · 3. `PATCH /api/produtos/{id}/estado` reserva, pausa, reativa, vende ou cancela reserva · 4. `POST /api/produtos/{id}/reabastecer` repõe estoque · 5. `DELETE /api/produtos/{id}` exclui |
| Pós-condição | anúncio persistido com dono (`UsuarioId`) e estado coerente |
| Regras | RN1, RN2 |
| Classes que colaboram | `Produto`, `Categoria`, `ServicoAnuncios`, `IRepositorioProduto` |

### FL2 — Negociar por proposta

| | |
|---|---|
| Atores | comprador (proponente) e vendedor (dono do anúncio) |
| Pré-condição | anúncio `Disponivel`, de outro usuário |
| Passos | 1. `POST /api/propostas` — comprador oferece um valor · 2. `POST /api/propostas/{id}/aceitar` (ou `/recusar`) — vendedor decide · 3. no aceite, a **estratégia da modalidade** decide se o valor é aceitável, a proposta vira `Aceita`, o anúncio vira `Reservado` e as demais propostas pendentes são recusadas · 4. o proponente compra pelo valor da proposta (FL3) |
| Pós-condição | proposta `Aceita`; anúncio `Reservado` só para o proponente |
| Regras | RN3 (e RN1 para quem pode aceitar) |
| Classes que colaboram | `Proposta`, `Produto`, `IEstrategiaNegociacao` (3 implementações), `ServicoPropostas`, `RegrasCompra` |

### FL3 — Comprar (carrinho → checkout → pagamento simulado → pedido)

| | |
|---|---|
| Atores | comprador; vendedor(es) no pós-venda |
| Pré-condição | sessão válida; anúncios de outros usuários com estoque |
| Passos | 1. `POST /api/carrinho/itens` adiciona (estoque **não** é reservado) · 2. `PUT/DELETE /api/carrinho/itens/{id}` ajusta · 3. `POST /api/pedidos` (checkout) valida itens, preços e estoque e cria a compra `Pagamento` (`Pendente`) com **um `Pedido` por vendedor**; o carrinho continua intacto · 4. `POST /api/pagamentos/{id}/simular-confirmacao` (PIX/boleto) ou `/cartao` (cartão) — revalida o estoque; se aprovado, baixa o estoque, marca pedidos `Pago` e tira os itens do carrinho, numa única transação · 5. vendedor `POST /api/pedidos/{id}/enviar`; comprador `/entregar`; vendedor `/concluir` · 6. comprador avalia cada item (`POST /api/avaliacoes`) |
| Pós-condição | pedido `Pago` com snapshot de nome/preço; estoque decrementado; histórico em `GET /api/pedidos` |
| Regras | RN4, RN5 |
| Classes que colaboram | `ItemCarrinho`, `Produto`, `Pagamento`, `Pedido`, `ItemPedido`, `DadosEntrega`, `RegrasCompra`, `ServicoCarrinho`, `ServicoPedidos`, `IUnidadeDeTrabalho` |

---

## 2. Estados

### Anúncio (`Produto.Estado`) — tabela `Produto.Transicoes`, chave (origem, operação)

| Origem | Operação | Destino |
|---|---|---|
| Disponivel | Reservar | Reservado |
| Disponivel | Vender | Vendido |
| Disponivel | Pausar | Pausado |
| Reservado | Vender | Vendido |
| Reservado | CancelarReserva | Disponivel |
| Pausado | Reativar | Disponivel |
| *(qualquer outra combinação)* | — | **rejeitada** com `TransicaoInvalidaException` (409), sem alterar o objeto |

Operações de estoque: `BaixarEstoque` (pagamento aprovado) leva a `Vendido` ao
zerar; `ReporEstoque` (estorno) e `Reabastecer` (vendedor) levam `Vendido` de
volta a `Disponivel`.

### Pedido (`Pedido.Estado`)

`Pendente` (aguardando pagamento) → `Pago` → `Enviado` → `Entregue` → `Concluido`;
`Pendente` → `Cancelado` (pagamento recusado/expirado/cancelado);
`Pago` → `Cancelado` (estorno, devolve estoque).

### Pagamento (`Pagamento.Status`)

`Pendente` → `Pago` | `Recusado` | `Expirado` | `Cancelado`. Só sai de
`Pendente` uma vez (`TransicaoInvalidaException` nas demais tentativas).

---

## 3. Regras de negócio e critérios de aceitação

### RN1 — Somente o proprietário altera o anúncio

Editar, mudar estado, reabastecer, enviar imagem e excluir exigem que o
solicitante seja o dono (`Produto.UsuarioId`). Anúncio vendido não pode ser
editado nem excluído.

| CA | Dado | Quando | Então |
|---|---|---|---|
| RN1-CA1 | anúncio do usuário A | A altera | operação aceita |
| RN1-CA2 | anúncio do usuário A | B tenta editar, pausar ou excluir | 403 `acesso-negado`; anúncio inalterado |
| RN1-CA3 | sem sessão | tenta criar anúncio | 401 |

Implementação: `Produto.ValidarPropriedade`, `Produto.ValidarExclusao`
(`backend/Models/Produto.cs`), chamados por `ServicoAnuncios` em toda escrita.

### RN2 — O anúncio só muda de estado pelas transições permitidas

| CA | Dado | Quando | Então |
|---|---|---|---|
| RN2-CA1 | anúncio `Disponivel` | Reservar | `Reservado` |
| RN2-CA2 | anúncio `Reservado` | Reativar | 409 `transicao-invalida`; continua `Reservado` (não cancela a reserva) |
| RN2-CA3 | anúncio `Pausado` (API) | Vender | 409; ao reler pela API, estado e estoque **no banco** iguais |
| RN2-CA4 | anúncio sem estoque | Vender | 409 |

Implementação: `Produto.Transicoes` + `Produto.AplicarOperacao`.

### RN3 — O aceite de uma proposta depende da modalidade do anúncio

| Modalidade | Aceita a partir de |
|---|---|
| `PrecoFixo` | 100% do preço |
| `AceitaOferta` | 85% do preço |
| `Liquidacao` | 70% do preço |

| CA | Dado | Quando | Então |
|---|---|---|---|
| RN3-CA1 | preço R$ 249,90, oferta R$ 200 | modalidade `Liquidacao` | proposta aceita; anúncio `Reservado` |
| RN3-CA2 | mesma oferta | modalidade `PrecoFixo` | 409 `conflito`; proposta continua `Pendente` |
| RN3-CA3 | oferta exatamente no limite de `AceitaOferta` | aceite | aceita |

Implementação: `IEstrategiaNegociacao` (`EstrategiaPrecoFixo`,
`EstrategiaAceitaOferta`, `EstrategiaLiquidacao`) escolhida em
`ServicoPropostas.Aceitar` pela `Modalidade` do anúncio.

### RN4 — Quantidade comprada entre 1 e o estoque disponível

Validada **no servidor** ao adicionar ao carrinho, no checkout e de novo na
aprovação do pagamento. O estoque nunca fica negativo, inclusive com compras
simultâneas.

| CA | Dado | Quando | Então |
|---|---|---|---|
| RN4-CA1 | estoque 3 | comprador adiciona 1 + 2 | carrinho com 3 |
| RN4-CA2 | estoque 3, carrinho com 3 | adiciona mais 1 | 409 "Estoque insuficiente"; carrinho com 3 |
| RN4-CA3 | qualquer anúncio | quantidade 0 ou negativa | 400 |
| RN4-CA4 | carrinho com 3; vendedor reduz estoque para 2 | checkout | 409; nenhuma compra criada |
| RN4-CA5 | estoque 1; 5 compradores com checkout feito | todos confirmam o pagamento ao mesmo tempo | exatamente 1 aprovado; estoque final 0 |

Implementação: `RegrasCompra.Validar` (carrinho e checkout),
`Produto.BaixarEstoque` (nunca abaixo de zero), `UnidadeDeTrabalhoEf`
(transação `BEGIN IMMEDIATE`) + token de concorrência em `Produto.Estoque`.

### RN5 — O estoque só é baixado quando o pagamento simulado é aprovado

| CA | Dado | Quando | Então |
|---|---|---|---|
| RN5-CA1 | carrinho com 2 un. (estoque 3) | checkout PIX | compra `Pendente`; estoque 3; carrinho mantido |
| RN5-CA2 | compra `Pendente` | simular confirmação | `Pago`; estoque 1; itens saem do carrinho |
| RN5-CA3 | compra com cartão | cartão de recusa `4000 0000 0000 0002` | `Recusado`; estoque inalterado; carrinho mantido |
| RN5-CA4 | compra `Pendente` | cancelar ou expirar | `Cancelado`/`Expirado`; estoque inalterado |
| RN5-CA5 | pedido `Pago` | cancelar antes do envio | `Cancelado`/`Estornado`; estoque devolvido |

Implementação: `ServicoPedidos.Finalizar` (não mexe no estoque),
`ServicoPedidos.Aprovar` (revalida, `Produto.BaixarEstoque`,
`Pedido.ConfirmarPagamento`, `Pagamento.Aprovar`), `ServicoPedidos.Cancelar`
(`Produto.ReporEstoque`).

### Regras complementares (também implementadas e testadas)

| ID | Regra | Implementação | Teste |
|---|---|---|---|
| RC1 | Não comprar o próprio anúncio | `RegrasCompra.Validar`, construtor de `Pedido` | `Construtor_NoProprioAnuncio_DeveFalhar`, `Carrinho_NaoAceitaAnuncioProprioNemSemLogin` |
| RC2 | Pedido guarda nome e preço do momento da compra | `ItemPedido` | `Snapshot_NaoMudaQuandoOAnuncioMuda` |
| RC3 | Pedido não pode ser vazio | construtor de `Pedido` | `Construtor_SemItens_DeveFalhar` |
| RC4 | A mesma tentativa de checkout não gera duas compras | `Pagamento.ChaveIdempotencia` + índice único | `MesmaTentativaDeCheckout_NaoDuplicaPedido` |
| RC5 | Reabastecer só pelo dono e nunca em anúncio reservado | `Produto.Reabastecer` | `ProdutoReabastecerTests`, `ReabastecerApiTests` |
| RC6 | Avaliação só após receber o pedido, uma por item | `Avaliacao` | `AvaliacaoTests` |

---

## 4. Matriz de rastreabilidade (fluxo → regra → implementação → testes)

Testes de **sucesso** (✔) e de **violação** (✘). Classe de teste entre
parênteses.

| Fluxo | Regra | Implementação | Sucesso | Violação |
|---|---|---|---|---|
| FL1 | RN1 | `Produto.ValidarPropriedade`, `Produto.ValidarExclusao`, `ServicoAnuncios` | ✔ `ValidarPropriedade_QuandoEhDono_NaoDeveFalhar` (ProdutoPropriedadeTests) | ✘ `ValidarPropriedade_QuandoNaoEhDono_DeveNegar` (ProdutoPropriedadeTests) · ✘ `ValidarExclusao_DeOutroUsuario_DeveNegar` · ✘ `Seguranca_CompradorNaoEditaNemExcluiAnuncioDeTerceiro` (MarketplaceApiTests) · ✘ `CriarAnuncio_SemLogin_DeveRetornar401` (FluxoApiTests) |
| FL1 | RN2 | `Produto.Transicoes`, `Produto.AplicarOperacao` | ✔ `Reservar_DeDisponivel_DeveReservar` · ✔ `Vender_DeReservado_DeveVender` (ProdutoTransicoesTests) | ✘ `Reativar_DeReservado_NaoDeveCancelarReserva` · ✘ `Vender_SemEstoque_DeveFalhar` (ProdutoTransicoesTests) · ✘ `TransicaoProibida_ViaApi_EhRejeitadaENaoAlteraOBanco` (FalhasEInvariantesApiTests) |
| FL2 | RN3 | `IEstrategiaNegociacao` + 3 implementações, `ServicoPropostas.Aceitar` | ✔ `OfertaNoLimiteExato_DeveSerAceitaPorAceitaOferta` (EstrategiasTests) · ✔ `PropostaAceita_PermiteComprarPeloValorDaProposta` (MarketplaceApiTests) | ✘ `OfertaDe200EmPrecoDe249_90_DeveVariarPorModalidade` (EstrategiasTests) · ✘ `Aceitar_MesmaOferta_DeveVariarPorModalidade` (ServicoPropostasTests) |
| FL3 | RN4 | `RegrasCompra.Validar`, `Produto.BaixarEstoque`, `UnidadeDeTrabalhoEf` | ✔ `BaixarEstoque_DeveDecrementar_EAoZerarFicaVendido` (EstoqueTests) · ✔ `Carrinho_AdicionarAlterarRemover_ERespeitarEstoque` (MarketplaceApiTests) | ✘ `BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar` · ✘ `BaixarEstoque_QuantidadeInvalida_DeveFalhar` (EstoqueTests) · ✘ `S1_RegraDeNegocio_EstoqueInsuficiente_Retorna409SemAlterarCarrinho` (FalhasEInvariantesApiTests) · ✘ `Checkout_RevalidaEstoqueAlteradoDepoisDoCarrinho` · ✘ `ComprasSimultaneas_DoMesmoEstoque_SoUmaEhAprovada` (MarketplaceApiTests) |
| FL3 | RN5 | `ServicoPedidos.Finalizar/Aprovar/Cancelar`, `Pagamento`, `Pedido.ConfirmarPagamento` | ✔ `Pix_EstoqueSoBaixaDepoisDaAprovacao` · ✔ `Boleto_GeraLinhaDigitavelEConfirma` · ✔ `CancelarPedidoPago_DevolveEstoque` (MarketplaceApiTests) | ✘ `Cartao_AprovadoGuardaSoFinal_ERecusadoNaoMexeNoEstoque` · ✘ `CancelarEExpirar_NaoBaixamEstoque` (MarketplaceApiTests) · ✘ `Aprovar_JaRecusado_DeveFalhar` (PagamentoTests) |

Total de cenários desta matriz: **10 de sucesso e 17 de violação** (mínimo
exigido: 10).
