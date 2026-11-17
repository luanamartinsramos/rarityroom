# Decisões de modelagem

Registro das decisões que moldaram o domínio, no formato
contexto → decisão → consequência.

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

## D3 — Identificador gerado pelo repositório, sob trava

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
