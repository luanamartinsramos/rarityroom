# Requisitos — RarityRoom

Documento de requisitos verificáveis do marketplace de colecionáveis geek.
Cada requisito tem identificador estável e aponta para a evidência que o
comprova (arquivo, teste ou chamada HTTP).

## 1. Objetivo

Permitir que colecionadores anunciem, negociem e comprem itens geek, com
regras de negócio aplicadas pelo domínio e não pela interface.

## 2. Escopo

**Dentro:** contas, catálogo de categorias, anúncios com ciclo de vida,
negociação por propostas, favoritos, autenticação por cookie.

**Fora (registrado como limitação):** pagamento, envio, upload de imagem,
paginação e busca no servidor.

## 3. Requisitos funcionais

| ID | Requisito | Evidência |
|---|---|---|
| RF01 | Cadastrar usuário com nome, e-mail e senha | `ServicoAutenticacao.Cadastrar`; `ServicoAutenticacaoTests` |
| RF02 | Autenticar por e-mail e senha, mantendo sessão por cookie | `AutenticacaoController.Entrar`; cookie `RarityRoom.Auth` |
| RF03 | Encerrar sessão | `AutenticacaoController.Sair` |
| RF04 | Consultar o usuário autenticado | `GET /api/autenticacao/usuário-atual` |
| RF05 | Listar o catálogo de anúncios sem exigir login | `GET /api/produtos`; verificado HTTP 200 anônimo |
| RF06 | Consultar um anúncio por identificador | `GET /api/produtos/{id}` |
| RF07 | Publicar anúncio vinculado ao autor | `POST /api/produtos`; `Produto.UsuarioId` |
| RF08 | Editar anúncio | `PUT /api/produtos/{id}`; `ServicoAnuncios.Atualizar` |
| RF09 | Excluir anúncio | `DELETE /api/produtos/{id}`; `ServicoAnuncios.Remover` |
| RF10 | Alterar o estado do anúncio | `PATCH /api/produtos/{id}/estado` |
| RF11 | Listar e cadastrar categorias | `CategoriasController`; 7 categorias semeadas |
| RF12 | Favoritar e desfavoritar anúncio | `FavoritosController` |
| RF13 | Listar os próprios favoritos | `GET /api/favoritos` |
| RF14 | Enviar proposta de compra | `POST /api/propostas` |
| RF15 | Aceitar, recusar e cancelar proposta | `POST /api/propostas/{id}/aceitar|recusar|cancelar` |
| RF16 | Listar propostas enviadas e recebidas | `ServicoPropostas.Listar` |
| RF17 | Registrar pedido de compra com snapshot de nome e preço | `Pedido`; `FluxoApiTests` |
| RF18 | Concluir pedido (somente o vendedor) | `ServicoPedidos.Concluir` |
| RF19 | Cancelar pedido (comprador ou vendedor) | `ServicoPedidos.Cancelar` |
| RF20 | Avaliar vendedor após pedido concluído | `Avaliacao`; `AvaliacoesController` |
| RF21 | Consultar reputação do vendedor | `GET /api/avaliacoes/vendedor/{id}` |
| RF22 | Enviar imagem do anúncio | `POST /api/produtos/{id}/imagem` |
| RF23 | Pesquisar o catálogo por termo, categoria e estado | `GET /api/produtos?termo=&categoria=&estado=` |
| RF24 | Escolher tipo de conta no cadastro (comprador ou vendedor) | `ModeloCadastro.TipoConta`; `FluxoApiTests` |
| RF25 | Finalizar compra com endereço de entrega e forma de pagamento | `POST /api/pedidos`; `DadosEntrega` |

## 4. Regras de negócio

| ID | Regra | Onde vive | Teste |
|---|---|---|---|
| RN01 | Anúncio exige título, descrição opcional, preço > 0, estoque ≥ 0 e categoria existente | `Produto` + `ServicoAnuncios` | `ProdutoValidacaoTests`, `ServicoAnunciosCategoriaTests` |
| RN02 | Só o proprietário edita, exclui ou muda o estado | `Produto.ValidarPropriedade` | `ProdutoPropriedadeTests`, `ServicoAnunciosTests` |
| RN03 | Anúncio vendido não pode ser editado nem excluído | `Produto.Atualizar`, `ValidarExclusao` | `ProdutoTransicoesTests` |
| RN04 | Transições de estado só ocorrem pelo par (origem, operação) | `Produto.Transicoes` | `ProdutoTransicoesTests` |
| RN05 | Não vender anúncio sem estoque | `Produto.AplicarOperacao` | `ProdutoTransicoesTests` |
| RN06 | Usuário não favorita o próprio anúncio, nem duplica favorito | `Favorito` + `ServicoFavoritos` | `FavoritoTests`, `ServicoFavoritosTests` |
| RN07 | Categoria é única pelo slug (ignora acento e caixa) | `Categoria.GerarSlug` + `ServicoCategorias` | `CategoriaTests` |
| RN08 | Proposta exige valor > 0 e não pode ser feita no próprio anúncio | `Proposta` + `ServicoPropostas` | `PropostaTests`, `ServicoPropostasTests` |
| RN09 | Só anúncio disponível recebe proposta | `ServicoPropostas.Criar` | `ServicoPropostasTests` |
| RN10 | Aceite depende da modalidade de negociação | `IEstrategiaNegociacao` | `EstrategiasTests`, `ServicoPropostasTests` |
| RN11 | Aceitar uma proposta recusa as demais pendentes do anúncio | `ServicoPropostas.RecusarDemaisPendentes` | `ServicoPropostasTests` |
| RN12 | Só o proponente cancela a própria proposta | `Proposta.ValidarProponente` | `PropostaTests` |
| RN13 | E-mail de usuário é único, ignorando caixa | `ServicoAutenticacao` + repositório | `ServicoAutenticacaoTests` |
| RN14 | Senha nunca é armazenada em texto puro | `HashSenha` (PBKDF2 + salt) | `ServicoAutenticacaoTests` |
| RN15 | Pedido guarda nome e preço do momento da compra | `Pedido` | `PedidoTests` |
| RN16 | Um anúncio tem no máximo um pedido pendente | `ServicoPedidos.Criar` | `FluxoApiTests` |
| RN17 | Não comprar o próprio anúncio | `Pedido` | `PedidoTests` |
| RN18 | Avaliação exige pedido concluído, do comprador, nota 1–5, uma por pedido | `Avaliacao` + `ServicoAvaliacoes` | `AvaliacaoTests` |
| RN19 | Imagem até 2 MB, JPG/PNG/WEBP, nome gerado pelo servidor | `ServicoImagensArquivo` | `FluxoApiTests` |
| RN20 | Conta de comprador não publica anúncio | `ServicoAnuncios.Criar` | `FluxoApiTests` |
| RN21 | Conta de vendedor não compra | `ServicoPedidos.Criar` | `FluxoApiTests` |
| RN22 | Pedido exige endereço válido (CEP 8 dígitos, UF 2 letras) e forma de pagamento | `DadosEntrega` | `FluxoApiTests` |

## 5. Requisitos não funcionais

| ID | Requisito | Evidência |
|---|---|---|
| RNF01 | Erros de negócio respondem com status coerente e código estável | `MiddlewareExcecoes`; 400/403/404/409 observados |
| RNF02 | Identificadores de entidade nunca se repetem | `RepositorioProdutoTests`; HTTP id 4 após excluir id 2 |
| RNF03 | Repositórios são seguros sob concorrência | `Adicionar_EmParalelo_DeveGerarIdsUnicos` (200 threads) |
| RNF04 | Listas internas não escapam do repositório | `ObterTodos_DeveDevolverCopiaDaLista` |
| RNF05 | Compilação sem avisos | `dotnet build` — 0 avisos, 0 erros |
| RNF06 | Suíte automatizada executável e verde | `dotnet test` — 154 aprovados, 15 de integração |
| RNF07 | Dados persistem entre reinícios | SQLite + migração; verificado parando e subindo o servidor |

## 6. Critérios de aceitação globais

1. `dotnet build` sem erros nem avisos.
2. `dotnet test` com 100% de aprovação.
3. Fluxo completo por HTTP: cadastro → login → anúncio → proposta → aceite.
4. Nenhuma regra de negócio implementada apenas no controller.
