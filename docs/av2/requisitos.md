# Especificação de requisitos — RarityRoom

Especificação verificável do marketplace de colecionáveis geek. Cada fluxo e
cada regra tem identificador estável, condições, critérios de aceitação no
formato **Dado / Quando / Então** e aponta para a implementação e para o teste
que o comprova. A matriz completa fica em [`testes.md`](testes.md) §4.

> Critérios atendidos por este documento: **A03** (cinco regras), **A05**
> (estados), **G01** (especificação verificável dos três fluxos e das cinco
> regras).

## 1. Objetivo e escopo

Permitir que colecionadores anunciem, negociem e comprem itens geek, com as
regras de negócio aplicadas pelo **domínio** (backend), nunca pela interface.

**Dentro do escopo:** contas (uma conta compra e vende), catálogo com busca no
servidor e paginação, anúncios com ciclo de vida, negociação por propostas,
carrinho, checkout, pagamento **simulado** (PIX, boleto e cartão fictícios),
estorno, avaliações, favoritos, upload de imagem.

**Fora do escopo (limitações declaradas):** pagamento real (nenhum gateway é
chamado; os códigos PIX e boleto são fictícios), cálculo de frete, envio de
e-mail real (o token de recuperação de senha vai para o log do servidor).

## 2. Atores

| Ator | Descrição |
|---|---|
| Visitante | navega no catálogo sem login |
| Usuário | conta autenticada; pode **vender** (anunciar) e **comprar** |
| Proprietário | usuário dono de um anúncio específico |
| Administrador | modera contas e pedidos |

## 3. Os três fluxos de negócio

### FL-01 — Publicar e gerenciar um anúncio

| | |
|---|---|
| **Ator** | Usuário (torna-se proprietário do anúncio) |
| **Pré-condição** | sessão autenticada; categoria existente no catálogo |
| **Passos** | 1. `POST /api/produtos` com nome, preço, estoque, categoria e modalidade → anúncio `Disponivel` vinculado ao autor. 2. `PUT /api/produtos/{id}` edita. 3. `PATCH /api/produtos/{id}/estado` aplica uma operação (`Pausar`, `Reativar`, `Reservar`, `Vender`, `CancelarReserva`). |
| **Pós-condição** | anúncio persistido no SQLite com dono, estado e estoque válidos |
| **Regras aplicadas** | R1, R2, R5 |
| **Implementação** | `ProdutosController` → `ServicoAnuncios` → `Produto` → `RepositorioProdutoEf` |

**Critérios de aceitação**

- **CA-01.1** Dado um usuário autenticado, quando publica um anúncio válido,
  então recebe `201` e o anúncio nasce `Disponivel` com `usuarioId` igual ao
  dele. *(`Criar_DeveVincularProprietario`)*
- **CA-01.2** Dado um anúncio `Pausado`, quando o dono pede `Vender`, então
  recebe `409 transicao-invalida` e o anúncio relido do banco continua
  `Pausado`. *(`Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado`)*
- **CA-01.3** Dado um anúncio de outra pessoa, quando um terceiro tenta editar,
  então recebe `403` e os dados não mudam. *(`Terceiro_NaoPodeEditarAnuncioAlheio`)*

### FL-02 — Negociar por proposta

| | |
|---|---|
| **Ator** | Usuário comprador (proponente) e proprietário |
| **Pré-condição** | anúncio `Disponivel` de outra pessoa |
| **Passos** | 1. `POST /api/propostas` com `produtoId` e `valor` → proposta `Pendente`. 2. O proprietário chama `POST /api/propostas/{id}/aceitar`. 3. A estratégia da modalidade do anúncio decide se o valor é aceitável. 4. Se aceita: proposta `Aceita`, anúncio `Reservado` e demais propostas pendentes `Recusada`. |
| **Pós-condição** | anúncio reservado só para o proponente vencedor |
| **Regras aplicadas** | R1, R2, R3, R4 |
| **Implementação** | `PropostasController` → `ServicoPropostas` → `Proposta` + `Produto` + `IEstrategiaNegociacao` |

**Critérios de aceitação**

- **CA-02.1** Dado um anúncio de R$ 249,90, quando o dono aceita uma oferta de
  R$ 200, então a resposta é `409` em `PrecoFixo` e `200` em `Liquidacao`.
  *(`Aceitar_MesmaOferta_DeveVariarPorModalidade`)*
- **CA-02.2** Dado que uma proposta foi aceita, então as demais pendentes do
  mesmo anúncio ficam `Recusada`. *(`Aceitar_DeveRecusarAsDemaisPendentes`)*
- **CA-02.3** Dado o próprio anúncio, quando o dono tenta propor, então recebe
  `400`. *(`Criar_NoProprioAnuncio_DeveFalhar`)*

### FL-03 — Comprar, pagar e estornar

| | |
|---|---|
| **Ator** | Usuário comprador e proprietário (vendedor) |
| **Pré-condição** | anúncio de outra pessoa, `Disponivel` (ou `Reservado` para o vencedor da proposta), com estoque |
| **Passos** | 1. `POST /api/carrinho/itens`. 2. `POST /api/pedidos` (checkout com endereço e forma de pagamento) → pedido `Pendente` + pagamento simulado. 3. `POST /api/pagamentos/{id}/simular-confirmacao` → pagamento aprovado, **estoque baixado**, pedido `Pago`. 4. Opcional: `POST /api/pedidos/{id}/cancelar` → pedido `Cancelado`, pagamento `Estornado`, **estoque reposto**. |
| **Pós-condição** | estoque coerente com as vendas efetivas; histórico do pedido preservado (snapshot) |
| **Regras aplicadas** | R4, R5 |
| **Implementação** | `PedidosController`/`PagamentosController` → `ServicoPedidos` → `Pedido` (compõe `ItemPedido`) + `Produto.BaixarEstoque`/`ReporEstoque` |

**Critérios de aceitação**

- **CA-03.1** Dado um anúncio com estoque 1, quando a compra é paga, então o
  estoque vai a 0 e o anúncio fica `Vendido`; ao cancelar a compra paga, o
  estoque volta a 1 e o pagamento fica `Estornado`.
  *(`CancelarPedidoPago_DevolveEstoque`)*
- **CA-03.2** Dado um pedido com quantidade maior que o estoque, então a baixa
  falha com `409` e o estoque não muda.
  *(`BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar`)*
- **CA-03.3** Dado um estorno sobre um anúncio com estoque alto, então o
  estoque nunca fica negativo nem passa de 100.000.
  *(`EstornoDeCompraPaga_ComEstoqueNoLimite_NaoDeixaEstoqueNegativo`,
  `ReporEstoque_NoTeto_NaoEstouraNemFicaNegativo`)*

## 4. As cinco regras de negócio centrais

| ID | Regra | Condição | Resultado esperado em caso de violação | Onde vive |
|---|---|---|---|---|
| **R1** | Só o proprietário altera o anúncio | editar, excluir ou mudar estado exige `solicitanteId == UsuarioId` | `403 acesso-negado`; dados inalterados | `Produto.ValidarPropriedade`, `Produto.ValidarExclusao` |
| **R2** | O estado do anúncio só muda por transição permitida | o par (estado de origem, operação) precisa existir na tabela de transições | `409 transicao-invalida`; estado inalterado | `Produto.AplicarOperacao` + tabela `Transicoes` |
| **R3** | O aceite de uma proposta depende da modalidade | `PrecoFixo` ≥ 100% do preço; `AceitaOferta` ≥ 85%; `Liquidacao` ≥ 70% | `409 conflito`; proposta segue `Pendente` | `IEstrategiaNegociacao` (3 implementações) |
| **R4** | Ninguém negocia o próprio anúncio | o proponente (proposta) ou comprador (pedido) não pode ser o dono | `400 dados-invalidos`; nada é criado | `Proposta`/`ServicoPropostas.Criar`, construtor de `Pedido` |
| **R5** | O estoque fica sempre entre 0 e 100.000 | baixa só até o disponível; reposição limitada ao teto; cadastro e edição validam o teto | `409` (baixa acima do disponível) ou `400` (estoque fora da faixa); estoque inalterado | `Produto.BaixarEstoque`, `Produto.ReporEstoque`, `Produto.EstoqueValido` |

### Critérios de aceitação por regra

- **R1** — Dado o dono, quando edita, então os dados mudam
  (`Atualizar_PeloDono_DeveAlterar`). Dado um terceiro, quando edita, então
  `AcessoNegadoException` e nada muda (`Atualizar_PorOutroUsuario_NaoDeveAlterarDados`).
- **R2** — Dado `Disponivel`, quando `Reservar`, então `Reservado`
  (`Reservar_DeDisponivel_DeveReservar`). Dado `Pausado`, quando
  `CancelarReserva`, então `TransicaoInvalidaException` e continua `Pausado`
  (`CancelarReserva_DePausado_DeveFalhar`).
- **R3** — Dada uma oferta exatamente no limite de 85%, `AceitaOferta` aceita
  (`OfertaNoLimiteExato_DeveSerAceitaPorAceitaOferta`). Dada a mesma oferta de
  R$ 200 sobre R$ 249,90, `PrecoFixo` recusa e `Liquidacao` aceita
  (`OfertaDe200EmPrecoDe249_90_DeveVariarPorModalidade`).
- **R4** — Dado o anúncio de outra pessoa, a proposta nasce `Pendente`
  (`Construtor_DeveIniciarPendente`). Dado o próprio anúncio, a proposta e o
  pedido são recusados (`Criar_NoProprioAnuncio_DeveFalhar`,
  `Construtor_NoProprioAnuncio_DeveFalhar`).
- **R5** — Dado estoque 3, baixar 2 e depois 1 leva a 0 e `Vendido`
  (`BaixarEstoque_DeveDecrementar_EAoZerarFicaVendido`). Dado estoque 3, baixar
  4 falha e o estoque continua 3 (`BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar`).

## 5. Estados do objeto central (`Produto`)

| Origem \ Operação | Reservar | Vender | Pausar | Reativar | CancelarReserva |
|---|---|---|---|---|---|
| **Disponivel** | Reservado | Vendido | Pausado | — | — |
| **Reservado** | — | Vendido | — | — | Disponivel |
| **Pausado** | — | — | — | Disponivel | — |
| **Vendido** | — | — | — | — | — |

"—" = transição proibida → `TransicaoInvalidaException` (409), sem alterar o
objeto. A identidade (`Id`) nunca muda numa transição. A baixa e a reposição de
estoque (R5) também movem o estado: estoque 0 → `Vendido`; reposição de um
`Vendido` → `Disponivel`.

Estados secundários: `Proposta` (`Pendente` → `Aceita` | `Recusada` |
`Cancelada`) e `Pedido` (`Pendente` → `Pago` → `Enviado` → `Entregue` →
`Concluido`; `Cancelado` a partir de `Pendente` ou `Pago`).

## 6. Catálogo complementar

Regras e requisitos implementados além do núcleo avaliado. Não fazem parte das
cinco regras centrais, mas continuam testados.

| ID | Regra | Onde vive | Teste |
|---|---|---|---|
| RC-01 | Anúncio exige nome ≥ 3, preço > 0 e categoria existente | `Produto.AplicarDados`, `ServicoAnuncios` | `ProdutoValidacaoTests`, `ServicoAnunciosCategoriaTests` |
| RC-02 | Usuário não favorita o próprio anúncio nem duplica favorito | `Favorito`, `ServicoFavoritos` | `FavoritoTests`, `ServicoFavoritosTests` |
| RC-03 | Categoria é única pelo slug (ignora acento e caixa) | `Categoria.GerarSlug` | `CategoriaTests` |
| RC-04 | Só o proponente cancela a própria proposta | `Proposta.ValidarProponente` | `Cancelar_DeOutroUsuario_DeveNegar` |
| RC-05 | E-mail único, senha com hash PBKDF2, mínimo de 8 caracteres com letra e número | `ServicoAutenticacao`, `HashSenha`, `Usuario` | `ServicoAutenticacaoTests`, `SegurancaApiTests` |
| RC-06 | Pedido guarda nome e preço do momento da compra (snapshot) | `ItemPedido` | `Snapshot_NaoMudaQuandoOAnuncioMuda` |
| RC-07 | Avaliação só após recebimento, uma por item, nota 1–5 | `Avaliacao`, `ServicoAvaliacoes` | `AvaliacaoTests` |
| RC-08 | Imagem até 2 MB, assinatura binária JPG/PNG/WEBP, nome gerado pelo servidor | `ServicoImagensArquivo` | `Upload_DeFormatoInvalido_DeveRetornar400` |
| RC-09 | Uma conta compra e vende (não há papel exclusivo) | `ServicoAutenticacao.Cadastrar` (`TipoConta.Ambos`) | `ContaUnica_PodePublicarEComprar` |
| RC-10 | Checkout exige endereço válido (CEP 8 dígitos, UF) e forma de pagamento | `DadosEntrega.Criar` | `Checkout_SemEndereco_DeveRetornar400` |

## 7. Requisitos não funcionais

| ID | Requisito | Evidência |
|---|---|---|
| RNF01 | Falhas respondem com status coerente e código estável (ProblemDetails) | `MiddlewareExcecoes`; [`excecoes.md`](excecoes.md) |
| RNF02 | Falha inesperada não expõe detalhes internos | `Falha03_ErroInesperado_Responde500Generico_SemVazarDetalhes_ERegistraNoLog` |
| RNF03 | Dados persistem entre reinícios | SQLite + migrações EF Core em `backend/Dados/Migracoes` |
| RNF04 | Compilação sem avisos e suíte verde | `dotnet build` 0 avisos; `dotnet test` 245 aprovados |
| RNF05 | Compras simultâneas do mesmo estoque não vendem além do disponível | concorrência otimista; testes de concorrência em `MarketplaceApiTests` |
