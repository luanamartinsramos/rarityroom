# Requisitos — RarityRoom

Requisitos funcionais e não funcionais do marketplace. Os **três fluxos** e as
**cinco regras de negócio principais**, com critérios de aceitação e
rastreabilidade, estão em [`especificacao.md`](especificacao.md). Este
documento lista o restante, sempre com a evidência que o comprova.

## 1. Objetivo

Permitir que colecionadores anunciem, negociem e comprem itens geek, com as
regras de negócio aplicadas pelo domínio no backend, e não pela interface.

## 2. Escopo

**Dentro:** contas e autenticação por cookie; catálogo com busca, filtros,
7 ordenações e paginação no servidor; anúncios com ciclo de vida e
reabastecimento; negociação por propostas; favoritos; carrinho no servidor;
checkout; **pagamento simulado** (PIX, boleto e cartão, sem gateway real);
pedidos com pós-venda e avaliação por item; lojas, perfil, endereços,
notificações e administração; upload de imagem.

**Fora (limitações declaradas):** pagamento real e cálculo de frete;
expiração de PIX/boleto feita sob demanda (ao consultar), sem processo em
segundo plano; banco apenas SQLite.

## 3. Requisitos funcionais

| ID | Requisito | Evidência |
|---|---|---|
| RF01 | Cadastrar usuário com nome, e-mail e senha | `ServicoAutenticacao.Cadastrar`; `ServicoAutenticacaoTests` |
| RF02 | Autenticar e manter sessão por cookie | `AutenticacaoController.Entrar`; cookie `RarityRoom.Auth` |
| RF03 | Encerrar sessão | `AutenticacaoController.Sair` |
| RF04 | Consultar o usuário autenticado | `GET /api/autenticacao/usuário-atual` |
| RF05 | Pesquisar o catálogo por termo (palavras em qualquer ordem, nome parcial, sem acento), categoria, preço e destaque | `ServicoAnuncios.Listar`; `Pesquisa_ExistenteInexistenteParcialEPalavrasForaDeOrdem` |
| RF06 | Ordenar por relevância, menor/maior preço, mais recentes/antigos, mais vendidos e melhor avaliados, com paginação (`X-Total-Count`) | `Ordenacao_PrecoEDataEPaginacao`, `Ordenacao_MaisVendidosEMelhorAvaliados_UsamDadosReais` |
| RF07 | Consultar um anúncio com vendedor, vendas e avaliações reais | `GET /api/produtos/{id}` (`ProdutoCatalogo`) |
| RF08 | Publicar, editar, mudar estado e excluir anúncio (FL1) | `ProdutosController`; RN1/RN2 |
| RF09 | Reabastecer anúncio esgotado | `POST /api/produtos/{id}/reabastecer`; `ReabastecerApiTests` |
| RF10 | Listar os próprios anúncios em qualquer estado | `GET /api/produtos/meus`; `Vitrine_NaoMostraAnuncioEsgotado` |
| RF11 | Listar e cadastrar categorias | `CategoriasController`; 15 categorias semeadas |
| RF12 | Favoritar, desfavoritar e listar favoritos | `FavoritosController`; `ServicoFavoritosTests` |
| RF13 | Enviar, aceitar, recusar e cancelar proposta (FL2) | `PropostasController`; `ServicoPropostasTests` |
| RF14 | Carrinho no servidor: adicionar, alterar quantidade, remover, limpar | `CarrinhoController`; `Carrinho_AdicionarAlterarRemover_ERespeitarEstoque`, `Carrinho_PersisteEntreSessoes` |
| RF15 | Checkout a partir do carrinho com endereço e forma de pagamento (FL3) | `POST /api/pedidos`; `Checkout_DeveGuardarEntregaEPagamento` |
| RF16 | Pagamento simulado por PIX (QR e copia e cola fictícios), boleto (linha digitável fictícia) e cartão (validação, parcelas) | `PagamentosController`; `Pix_…`, `Boleto_…`, `Cartao_…` |
| RF17 | Cancelar compra aguardando pagamento e expirar PIX/boleto | `CancelarEExpirar_NaoBaixamEstoque` |
| RF18 | Histórico de compras e detalhe do pedido | `GET /api/pedidos`, `GET /api/pedidos/{id}` |
| RF19 | Pós-venda: enviar (vendedor), confirmar entrega (comprador), concluir (vendedor), cancelar com estorno | `ServicoPedidos.Enviar/Entregar/Concluir/Cancelar`; `CancelarPedidoPago_DevolveEstoque` |
| RF20 | Avaliar cada item após receber o pedido; reputação do vendedor e média do produto | `AvaliacoesController`; `AvaliacaoDoProduto_DeveAlimentarAMedia` |
| RF21 | Enviar imagem do anúncio | `POST /api/produtos/{id}/imagem`; `Upload_Valido_DeveGravarImagemNoDisco` |
| RF22 | Loja, perfil, endereços, recuperação de senha, notificações e painel de administração | controllers correspondentes; `FluxoApiTests` |

## 4. Regras de negócio

As cinco principais (**RN1–RN5**) e as complementares (**RC1–RC6**) estão
especificadas, com critérios de aceitação, em `especificacao.md`. Regras
adicionais, também implementadas no domínio e testadas:

| ID | Regra | Onde vive | Teste |
|---|---|---|---|
| RA1 | Anúncio exige nome ≥ 3, preço > 0, estoque ≥ 0 e categoria cadastrada | `Produto` + `ServicoAnuncios` | `ProdutoValidacaoTests`, `ServicoAnunciosCategoriaTests` |
| RA2 | Anúncio vendido não pode ser editado nem excluído | `Produto.Atualizar`, `ValidarExclusao` | `Atualizar_AnuncioVendido_DeveFalhar`, `ValidarExclusao_AnuncioVendido_DeveFalhar` |
| RA3 | Usuário não favorita o próprio anúncio nem duplica favorito | `Favorito` + `ServicoFavoritos` | `FavoritoTests`, `ServicoFavoritosTests` |
| RA4 | Categoria é única pelo slug (ignora acento e caixa) | `Categoria.GerarSlug` | `CategoriaTests` |
| RA5 | Proposta exige valor > 0, anúncio disponível e de outro usuário; aceitar recusa as demais pendentes | `Proposta` + `ServicoPropostas` | `PropostaTests`, `ServicoPropostasTests` |
| RA6 | E-mail único (ignora caixa) e senha só em hash PBKDF2 | `ServicoAutenticacao`, `HashSenha` | `ServicoAutenticacaoTests` |
| RA7 | Endereço de entrega válido (CEP 8 dígitos, UF 2 letras) | `DadosEntrega.Criar` | `Checkout_SemEndereco_DeveRetornar400` |
| RA8 | Do cartão só ficam os 4 últimos dígitos, a bandeira e as parcelas | `Pagamento.RegistrarCartao`, `ValidadorCartao` | `Cartao_AprovadoGuardaSoFinal_ERecusadoNaoMexeNoEstoque` |
| RA9 | Pedidos e pagamentos só são visíveis para quem participa | `Pedido.Participa`, `ServicoPedidos` | `Seguranca_PedidoEPagamentoDeOutroUsuario_SaoNegados` |
| RA10 | Imagem até 2 MB, JPG/PNG/WEBP, nome gerado pelo servidor | `ServicoImagensArquivo` | `Upload_DeFormatoInvalido_DeveRetornar400` |

## 5. Requisitos não funcionais

| ID | Requisito | Evidência |
|---|---|---|
| RNF01 | Erros respondem `ProblemDetails` com status coerente e código estável, sem vazar detalhes internos | `MiddlewareExcecoes`; `excecoes.md`; `S3_FalhaInesperada_…` |
| RNF02 | Consistência sob concorrência: o mesmo estoque nunca é vendido duas vezes | `UnidadeDeTrabalhoEf` + token de concorrência; `ComprasSimultaneas_DoMesmoEstoque_SoUmaEhAprovada` |
| RNF03 | Identificadores nunca se repetem | autoincremento SQLite; `Adicionar_AposExcluirUltimo_NaoDeveReutilizarId` |
| RNF04 | Dados persistem entre reinícios; banco preparado automaticamente | SQLite + `Database.Migrate()` na inicialização |
| RNF05 | Backend compila sem erros nem avisos | `dotnet build` → 0 avisos, 0 erros |
| RNF06 | Suíte automatizada executável a partir de um clone limpo | `cd tests/RarityRoom.Tests && dotnet test` (ver `auditoria-final.md`) |
