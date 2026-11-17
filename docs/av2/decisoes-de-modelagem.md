# Decisões de modelagem

A **seção A** traz as decisões mais recentes com **comparação explícita de
alternativas** (o que foi considerado, critérios e por que a escolhida
venceu). A **seção B** é o registro histórico das decisões anteriores, no
formato contexto → decisão → consequência.

# A. Decisões com comparação de alternativas

## DA1 — Um pedido por vendedor × um pedido único para o carrinho inteiro

**Problema.** O carrinho pode ter anúncios de vendedores diferentes. Um
checkout precisa gerar registros que o comprador acompanhe e que cada
vendedor consiga atender (enviar, concluir, ser avaliado).

| Critério | Alternativa 1: um `Pedido` com itens de vários vendedores | Alternativa 2 (escolhida): um `Pedido` por vendedor + um `Pagamento` para o checkout |
|---|---|---|
| Quem envia/conclui | ambíguo: `Pedido.VendedorId` deixa de existir; "enviar" de um vendedor marcaria o pedido inteiro | cada pedido tem exatamente um `VendedorId`; `ValidarVendedor` continua simples |
| Avaliação e reputação | `Avaliacao.VendedorId` precisaria vir do item, e a reputação misturaria vendedores | `Avaliacao` herda o vendedor do pedido, como antes |
| Cancelamento com estorno | estornar parte do pedido exigiria estados por item | cancela um pedido inteiro sem afetar o de outro vendedor |
| Pagamento único | natural | exige a entidade `Pagamento` ligando 1..* pedidos |
| Impacto no código existente | reescrever `ServicoAdmin`, `ServicoVendedores`, `Avaliacao`, telas de vendas | mantém `Pedido.VendedorId` e todo o pós-venda |

**Escolha:** alternativa 2. O custo (uma entidade a mais, `Pagamento`) é menor
que tornar ambíguas as responsabilidades de envio, avaliação e estorno. É o
mesmo desenho dos marketplaces de referência (uma compra, várias entregas).
**Evidência:** `ServicoPedidos.Finalizar` (`GroupBy(UsuarioId)`),
`CarrinhoComDoisVendedores_GeraUmPedidoPorVendedor`.

## DA2 — Itens do pedido como composição com *snapshot* × referência viva ao anúncio

| Critério | Alternativa 1: `ItemPedido` só com `ProdutoId` e quantidade; preço lido do anúncio | Alternativa 2 (escolhida): `ItemPedido` composto pelo `Pedido`, copiando nome, imagem e preço |
|---|---|---|
| Histórico | editar o preço do anúncio reescreveria compras passadas | preço do momento da compra preservado (`Snapshot_NaoMudaQuandoOAnuncioMuda`) |
| Anúncio excluído | pedido perderia nome e preço | pedido continua legível |
| Duplicação de dados | nenhuma | nome/preço duplicados — **intencional**: representam fatos diferentes (preço praticado × preço atual) |
| Ciclo de vida | item independente, precisaria de repositório | item só existe dentro do pedido (`OwnsMany`, cascade) |
| Proteção do total | total recalculado a cada leitura, sujeito a mudança | `Total` calculado uma vez; lista privada somente leitura |

**Escolha:** alternativa 2. A regra "preservar o preço praticado no momento da
compra" é requisito; a duplicação é o preço dessa regra.

## DA3 — Estado nomeado com tabela de transições × flags booleanas

| Critério | Alternativa 1: `bool Pago`, `bool Enviado`, `bool Cancelado`... | Alternativa 2 (escolhida): `enum EstadoPedido`/`EstadoAnuncio` + dicionário `(origem, operação) → destino` |
|---|---|---|
| Combinações inválidas | possíveis (`Pago=false` e `Enviado=true`) | impossíveis: só há um estado por vez |
| Onde vive a regra | espalhada em `if`s nos serviços | uma tabela privada na entidade |
| Transição proibida | depende de cada chamador lembrar de verificar | `TransicaoInvalidaException` antes de alterar qualquer campo |
| Distinguir operações com mesmo destino | — | `Reativar` × `CancelarReserva` (ambas → `Disponivel`) são chaves diferentes |

**Escolha:** alternativa 2. **Evidência:** `ProdutoTransicoesTests`,
`TransicaoProibida_ViaApi_EhRejeitadaENaoAlteraOBanco`.

## DA4 — Carrinho no banco × carrinho no navegador (`localStorage`)

| Critério | Alternativa 1: `localStorage` (como era) | Alternativa 2 (escolhida): tabela `itens_carrinho` por usuário |
|---|---|---|
| Vínculo com o usuário | por navegador, não por conta | por conta (`UsuarioId`), persiste entre dispositivos |
| Validação de estoque/dono | só no cliente, contornável | no servidor, a cada operação (`RegrasCompra`) |
| Preço exibido | cópia antiga guardada no navegador | sempre o atual, calculado no servidor |
| Custo | zero no servidor | uma tabela e um serviço |

**Escolha:** alternativa 2 — o requisito de não permitir manipular preço ou
quantidade pelo frontend inviabiliza a alternativa 1.

---

# B. Registro histórico de decisões

---

## D1 — Regra de negócio pertence à entidade

**Contexto.** No projeto original, `Produto` era um registro passivo com
`{ get; private set; }` e `Atualizar(...)` que aceitava qualquer valor. A
única validação era `[Range]` no modelo de requisição.

**Decisão.** `Produto`, `Usuario`, `Categoria`, `Proposta` e `Favorito`
validam no construtor e em todo método de mutação.

**Consequência.** Nenhum caminho (HTTP, serviço, teste) cria estado inválido.
Chamar a entidade direto é suficiente para barrar dados ruins.

---

## D2 — Transição indexada por (origem, operação)

**Contexto.** O primeiro modelo usava um dicionário `destino → origem`, o que
tornava `Reativar` e `CancelarReserva` indistinguíveis: ambas levam a
`Disponivel`. O efeito era `Reativar()` cancelar uma reserva em silêncio.

**Decisão.** A chave é o par `(EstadoAnuncio Origem, OperacaoAnuncio Operacao)`.

**Consequência.** Cada operação tem exatamente um efeito por estado de origem.
`Reativar_DeReservado_NaoDeveCancelarReserva` protege a regressão.

---

## D3 — Identificador gerado pelo repositório, sob trava (substituída por D12)

**Contexto.** `int id = repositorio.ObterTodos().Count + 1` reutilizava ids
após uma exclusão. Reprodução: três anúncios (1, 2, 3), exclusão do 2, novo
anúncio recebia id 3 — duplicando um id existente e fazendo o próximo
`DELETE` apagar o item errado.

**Decisão.** O repositório mantém um contador próprio, incrementado dentro de
`lock`, e entrega o id à entidade por um método `internal`.

**Consequência.** Ids são monotônicos e únicos, inclusive sob concorrência
(200 inclusões paralelas verificadas em teste).

---

## D4 — Propriedade verificada no domínio

**Contexto.** Qualquer usuário autenticado editava o anúncio de terceiros,
porque `Produto` não tinha dono e o controller só exigia `[Authorize]`.

**Decisão.** `Produto.UsuarioId` é obrigatório e `ValidarPropriedade` /
`ValidarExclusao` vivem na entidade; o serviço as invoca em toda escrita.

**Consequência.** A autorização é uma regra do modelo, não um detalhe de
transporte. Verificado por HTTP: terceiro recebe 403 em PUT, DELETE e PATCH.

---

## D5 — Exceções de domínio traduzidas por middleware

**Contexto.** O serviço lançava `InvalidOperationException` e cada controller
repetia `try/catch` para escolher o status.

**Decisão.** Hierarquia `ExcecaoDominio` com código estável
(`dados-invalidos`, `acesso-negado`, `nao-encontrado`, `conflito`,
`transicao-invalida`) e um middleware único que produz `ProblemDetails`.

**Consequência.** Controller não conhece status de erro de negócio. Um novo
tipo de exceção entra em um só lugar. Detalhes internos não vazam: o 500
responde mensagem genérica e registra o stack trace apenas no log.

---

## D6 — Polimorfismo em vez de `if` por modalidade

**Contexto.** A decisão "aceitar ou não a oferta" varia por modalidade
(preço fixo, aceita oferta, liquidação).

**Decisão.** Interface `IEstrategiaNegociacao` com três implementações,
injetadas como coleção; o serviço escolhe pelo enum do anúncio.

**Consequência.** Uma quarta modalidade não altera `ServicoPropostas`. As três
implementações são efetivamente usadas: mesma oferta de R$ 200 sobre preço de
R$ 249,90 é recusada no preço fixo e aceita na liquidação.

---

## D7 — Categoria como entidade, anúncio guarda o nome canônico

**Contexto.** `Categoria` era `string` livre: "Funko", "funko" e "FUNKO"
produziam três categorias e filtros quebrados.

**Decisão.** `Categoria` é entidade com `Nome` e `Slug` normalizado. O serviço
resolve a categoria pelo slug e grava no anúncio o **nome canônico** do
catálogo.

**Consequência.** O campo `categoria` do anúncio continua sendo texto (o
contrato do frontend não muda), mas o valor é sempre o do catálogo. A
migração para chave estrangeira fica para quando houver banco de dados.

---

## D8 — Proposta como agregado próprio, com recusa em cascata

**Contexto.** Negociação era apenas um botão de "comprar": não havia oferta,
contraproposta nem histórico.

**Decisão.** `Proposta` é entidade com ciclo próprio. Aceitar uma proposta
recusa automaticamente as demais pendentes do mesmo anúncio.

**Consequência.** O comprador nunca fica esperando por um item que já tem
dono. A operação é verificada por teste e por HTTP.

---

## D9 — Modelo de requisição só com o mínimo de `[Required]`

**Contexto.** As regras apareciam duas vezes: `[MinLength(6)]` no modelo HTTP
e validação no domínio. Duas cópias divergem com o tempo.

**Decisão.** `ModelosRequisicao` mantém apenas `[Required]`, para o binding não
receber nulo. Toda regra (tamanho, faixa, formato) vive no domínio.

**Consequência.** Uma única fonte de verdade. A mensagem de erro também é a do
domínio, específica por campo ("O preço deve ser maior que zero.").

---

## D10 — Persistência em SQLite com EF Core

**Contexto.** O projeto nasceu sem banco de dados: os dados viviam em listas na
memória e desapareciam a cada reinício. Como a arquitetura já isolava a
persistência atrás de `IRepositorioX`, a troca podia ser contida.

**Decisão.** EF Core 10 com SQLite, migração versionada em `Dados/Migracoes`
e aplicação automática na inicialização (`Database.Migrate()`).

**Consequência.** Os dados sobrevivem ao reinício — verificado parando e
subindo o servidor com anúncio, usuário e favorito criados. O domínio, os
serviços e os controllers não mudaram: apenas as implementações de repositório
e o registro de dependências.

---

## D11 — Ciclo de vida: serviços e repositórios em escopo

**Contexto.** Com repositórios em memória, o registro era `singleton`. O
`DbContext` do EF Core não é thread-safe e deve viver o escopo de uma
requisição.

**Decisão.** Repositórios e serviços passam a `Scoped`. Permanecem `Singleton`
apenas `IHashSenha` e as três estratégias, que são sem estado.

**Consequência.** Cada requisição tem um contexto próprio — sem vazamento de
estado entre usuários e sem condição de corrida no `SaveChanges`.

---

## D12 — Id gerado pelo banco, não pelo código

**Contexto.** O contador em memória resolvia o defeito de id duplicado, mas
ainda era responsabilidade da aplicação.

**Decisão.** A chave é `INTEGER PRIMARY KEY AUTOINCREMENT`: o SQLite gera o
identificador e nunca reutiliza um valor já emitido.

**Consequência.** Excluir o último anúncio e criar outro gera id novo
(verificado em `Adicionar_AposExcluirUltimo_NaoDeveReutilizarId`, com banco
real). A classe do defeito original deixa de existir por construção.

---

## D13 — Dublês em memória continuam existindo, mas só nos testes

**Contexto.** A suíte tinha 104 testes rápidos apoiados nos repositórios em
memória; a produção agora usa EF Core.

**Decisão.** Os repositórios em memória foram movidos para
`tests/RarityRoom.Tests/Fakes` e são **dublês de teste**. A persistência real
tem testes próprios (`RepositorioEfTests`) contra SQLite em memória.

**Consequência.** A suíte continua rápida (a maioria dos testes não abre banco)
e a camada que mudou tem cobertura própria. O risco de divergência entre dublê
e implementação real é reduzido pelos testes de EF.

---

## D14 — Pedido guarda snapshot, não referência viva

**Contexto.** Se o pedido apenas apontasse para o anúncio, editar o preço depois
reescreveria o histórico de uma compra já feita.

**Decisão.** `Pedido` copia `NomeProduto` e `PrecoUnitario` no momento da
compra. A referência ao anúncio continua existindo, mas não é fonte do valor.

**Consequência.** O histórico é imutável por construção.
`Snapshot_NaoMudaQuandoOAnuncioMuda` prova que editar o anúncio não altera o
pedido.

---

## D15 — Reputação é derivada, não armazenada

**Contexto.** Guardar uma coluna "média" no usuário exigiria recalculá-la a cada
avaliação e abriria espaço para divergência entre o número e as avaliações.

**Decisão.** `ReputacaoVendedor` é calculado em consulta: média arredondada e
contagem. Vendedor sem avaliação responde `0 / 0`.

**Consequência.** Não existe estado duplicado para sincronizar. O custo é uma
consulta a mais por leitura — irrelevante nesta escala.

---

## D16 — Imagem: nome do servidor, tipo declarado, tamanho limitado

**Contexto.** Aceitar o nome do arquivo enviado permite path traversal
(`../../appsettings`) e extensão forjada (`foto.png` que é executável).

**Decisão.** O nome final é `Guid` + extensão derivada do `Content-Type`
declarado, restrito a JPG/PNG/WEBP, com limite de 2 MB. O nome original é
descartado.

**Consequência.** O pior caso de um envio malicioso é um arquivo de imagem
inválido ocupando 2 MB. Verificado por HTTP: `text/plain` → 400, terceiro → 403.

**Limitação declarada.** O conteúdo binário não é inspecionado (não há parsing
de imagem); valida-se tipo declarado e tamanho.
