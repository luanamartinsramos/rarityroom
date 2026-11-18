using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RarityRoom.Tests.Api;

/// <summary>
/// Testes de integração HTTP: sobem a aplicação real (pipeline, controllers,
/// middleware, autenticação por cookie) e conversam por HTTP. Cada teste tem
/// seu próprio banco isolado.
/// </summary>
public class FluxoApiTests
{
    private static HttpClient CriarCliente(FabricaApi fabrica)
    {
        return fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static async Task<int> CadastrarELogar(
        HttpClient cliente,
        string nome,
        string email,
        string tipoConta = "Ambos")
    {
        var cadastro = await cliente.PostAsJsonAsync(
            "/api/autenticacao/cadastrar",
            new { nome, email, senha = "senha123", tipoConta });

        Assert.Equal(HttpStatusCode.OK, cadastro.StatusCode);

        var corpo = await cadastro.Content.ReadFromJsonAsync<JsonElement>();
        int id = corpo.GetProperty("usuario").GetProperty("id").GetInt32();

        var login = await cliente.PostAsJsonAsync(
            "/api/autenticacao/entrar",
            new { email, senha = "senha123" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return id;
    }

    private static async Task<int> CriarAnuncio(
        HttpClient cliente,
        string nome = "Funko Batman")
    {
        var resposta = await cliente.PostAsJsonAsync("/api/produtos", new
        {
            nome,
            descricao = "Item de coleção",
            preco = 249.90m,
            estoque = 1,
            categoria = "Funko Pop",
            marca = "Funko",
            colecao = "DC",
            condicao = "Novo",
            imagemUrl = string.Empty,
            modalidade = "PrecoFixo"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        return corpo.GetProperty("id").GetInt32();
    }

    internal static object Checkout(string formaPagamento = "Pix", string? chave = null)
    {
        return new
        {
            cep = "01310-100",
            logradouro = "Avenida Paulista",
            numero = "1000",
            complemento = "Apto 12",
            bairro = "Bela Vista",
            cidade = "São Paulo",
            estado = "SP",
            formaPagamento,
            chaveIdempotencia = chave ?? Guid.NewGuid().ToString("N")
        };
    }

    /// <summary>
    /// Fluxo real de compra: adiciona ao carrinho e finaliza. Se o carrinho
    /// recusar o item, devolve essa resposta.
    /// </summary>
    internal static async Task<HttpResponseMessage> FazerCheckout(
        HttpClient cliente,
        int produtoId,
        int quantidade = 1,
        string formaPagamento = "Pix")
    {
        var item = await cliente.PostAsJsonAsync(
            "/api/carrinho/itens",
            new { produtoId, quantidade });

        if (!item.IsSuccessStatusCode)
        {
            return item;
        }

        return await cliente.PostAsJsonAsync("/api/pedidos", Checkout(formaPagamento));
    }

    /// <summary>Compra e paga (PIX simulado). Devolve o id do pedido.</summary>
    internal static async Task<int> ComprarEPagar(HttpClient cliente, int produtoId)
    {
        var resposta = await FazerCheckout(cliente, produtoId);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var compra = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        int pagamentoId = compra.GetProperty("id").GetInt32();

        var pago = await cliente.PostAsync(
            $"/api/pagamentos/{pagamentoId}/simular-confirmacao",
            content: null);

        Assert.Equal(HttpStatusCode.OK, pago.StatusCode);

        return compra.GetProperty("pedidos")[0].GetProperty("id").GetInt32();
    }

    private static string EmailUnico(string prefixo)
    {
        return $"{prefixo}{Guid.NewGuid():N}@teste.com";
    }

    [Fact]
    public async Task Catalogo_DeveSerPublico()
    {
        using var fabrica = new FabricaApi();
        using var anonimo = CriarCliente(fabrica);

        var resposta = await anonimo.GetAsync("/api/produtos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarAnuncio_SemLogin_DeveRetornar401()
    {
        using var fabrica = new FabricaApi();
        using var anonimo = CriarCliente(fabrica);

        var resposta = await anonimo.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Funko Batman",
            preco = 249.90m,
            estoque = 1,
            categoria = "Funko Pop"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Terceiro_NaoPodeEditarAnuncioAlheio()
    {
        using var fabrica = new FabricaApi();
        using var dono = CriarCliente(fabrica);
        using var terceiro = CriarCliente(fabrica);

        await CadastrarELogar(dono, "Ana Lima", EmailUnico("ana"));
        await CadastrarELogar(terceiro, "Bruno Sa", EmailUnico("bruno"));

        int produtoId = await CriarAnuncio(dono);

        var resposta = await terceiro.PutAsJsonAsync($"/api/produtos/{produtoId}", new
        {
            nome = "Invadido",
            descricao = string.Empty,
            preco = 1m,
            estoque = 0,
            categoria = "Funko Pop",
            marca = string.Empty,
            colecao = string.Empty,
            condicao = string.Empty,
            imagemUrl = string.Empty,
            modalidade = "PrecoFixo"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Fluxo_Completo_CompraEAvaliacao()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);
        using var comprador = CriarCliente(fabrica);

        int vendedorId = await CadastrarELogar(
            vendedor, "Ana Lima", EmailUnico("ana"));

        await CadastrarELogar(comprador, "Bruno Sa", EmailUnico("bruno"));

        int produtoId = await CriarAnuncio(vendedor);

        var respostaPedido = await FazerCheckout(comprador, produtoId);

        Assert.Equal(HttpStatusCode.Created, respostaPedido.StatusCode);

        var compra = await respostaPedido.Content.ReadFromJsonAsync<JsonElement>();
        var pedido = compra.GetProperty("pedidos")[0];
        int pedidoId = pedido.GetProperty("id").GetInt32();

        // Snapshot: o pedido guarda o preço do momento da compra.
        var item = pedido.GetProperty("itens")[0];
        Assert.Equal(249.90m, item.GetProperty("precoUnitario").GetDecimal());
        Assert.Equal("Funko Batman", item.GetProperty("nomeProduto").GetString());

        var pagamento = await comprador.PostAsync(
            $"/api/pagamentos/{compra.GetProperty("id").GetInt32()}/simular-confirmacao",
            content: null);

        Assert.Equal(HttpStatusCode.OK, pagamento.StatusCode);

        var conclusao = await vendedor.PostAsync(
            $"/api/pedidos/{pedidoId}/concluir",
            content: null);

        Assert.Equal(HttpStatusCode.OK, conclusao.StatusCode);

        var anuncio = await comprador.GetFromJsonAsync<JsonElement>(
            $"/api/produtos/{produtoId}");

        Assert.Equal("Vendido", anuncio.GetProperty("estado").GetString());

        var respostaAvaliacao = await comprador.PostAsJsonAsync(
            "/api/avaliacoes",
            new { pedidoId, nota = 5, comentario = "Ótimo vendedor" });

        Assert.Equal(HttpStatusCode.Created, respostaAvaliacao.StatusCode);

        var reputacao = await comprador.GetFromJsonAsync<JsonElement>(
            $"/api/avaliacoes/vendedor/{vendedorId}");

        Assert.Equal(5m, reputacao.GetProperty("media").GetDecimal());
        Assert.Equal(1, reputacao.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Pedido_NoProprioAnuncio_DeveRetornar400()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"));

        int produtoId = await CriarAnuncio(vendedor);

        var resposta = await FazerCheckout(vendedor, produtoId);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Pedido_Duplicado_DeveRetornar409()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);
        using var comprador = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"));
        await CadastrarELogar(comprador, "Bruno Sa", EmailUnico("bruno"));

        int produtoId = await CriarAnuncio(vendedor);

        var primeiro = await FazerCheckout(comprador, produtoId);
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var segundo = await FazerCheckout(comprador, produtoId);
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task Avaliacao_AntesDeConcluir_DeveRetornar409()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);
        using var comprador = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"));
        await CadastrarELogar(comprador, "Bruno Sa", EmailUnico("bruno"));

        int produtoId = await CriarAnuncio(vendedor);

        var respostaPedido = await FazerCheckout(comprador, produtoId);

        var compra = await respostaPedido.Content.ReadFromJsonAsync<JsonElement>();
        int pedidoId = compra.GetProperty("pedidos")[0].GetProperty("id").GetInt32();

        var resposta = await comprador.PostAsJsonAsync(
            "/api/avaliacoes",
            new { pedidoId, nota = 5, comentario = "Adiantado" });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Upload_DeFormatoInvalido_DeveRetornar400()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"));

        int produtoId = await CriarAnuncio(vendedor);

        using var formulario = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent("nao sou imagem"u8.ToArray());
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        formulario.Add(arquivo, "arquivo", "nota.txt");

        var resposta = await vendedor.PostAsync(
            $"/api/produtos/{produtoId}/imagem",
            formulario);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Upload_Valido_DeveGravarImagemNoDisco()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"));

        int produtoId = await CriarAnuncio(vendedor);

        // Assinatura PNG mínima — o serviço valida tipo e tamanho, não conteúdo.
        byte[] bytesPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        using var formulario = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(bytesPng);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formulario.Add(arquivo, "arquivo", "capa.png");

        var resposta = await vendedor.PostAsync(
            $"/api/produtos/{produtoId}/imagem",
            formulario);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var produto = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        string? caminho = produto.GetProperty("imagemUrl").GetString();

        Assert.NotNull(caminho);
        Assert.StartsWith("/uploads/", caminho);

        string nomeArquivo = caminho!.Replace("/uploads/", string.Empty);
        Assert.True(File.Exists(Path.Combine(fabrica.PastaImagens, nomeArquivo)));
    }

    [Fact]
    public async Task Busca_DeveFiltrarNoServidor()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"));

        await CriarAnuncio(vendedor, "Funko Batman");
        await CriarAnuncio(vendedor, "Katana Demon Slayer");

        var porTermo = await vendedor.GetFromJsonAsync<JsonElement[]>(
            "/api/produtos?termo=batman");

        Assert.Single(porTermo!);
        Assert.Equal("Funko Batman", porTermo![0].GetProperty("nome").GetString());

        // "FUNKO" casa com a categoria dos dois anúncios, não só com o nome.
        var porCaixaAlta = await vendedor.GetFromJsonAsync<JsonElement[]>(
            "/api/produtos?termo=FUNKO");

        Assert.Equal(2, porCaixaAlta!.Length);

        var porCategoria = await vendedor.GetFromJsonAsync<JsonElement[]>(
            "/api/produtos?categoria=funko%20pop");

        Assert.Equal(2, porCategoria!.Length);

        var semResultado = await vendedor.GetFromJsonAsync<JsonElement[]>(
            "/api/produtos?termo=dragon%20ball");

        Assert.Empty(semResultado!);
    }

    [Fact]
    public async Task AvaliacaoDoProduto_DeveAlimentarAMedia()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);
        using var comprador = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"), "Vendedor");
        await CadastrarELogar(comprador, "Bruno Sa", EmailUnico("bruno"), "Comprador");

        int produtoId = await CriarAnuncio(vendedor);

        string rotaResumo = "/api/avaliacoes/produto/" + produtoId;

        // Sem avaliação: média zero.
        var vazio = await comprador.GetFromJsonAsync<JsonElement>(rotaResumo);

        Assert.Equal(0, vazio.GetProperty("total").GetInt32());

        int pedidoId = await ComprarEPagar(comprador, produtoId);

        await vendedor.PostAsync(
            "/api/pedidos/" + pedidoId + "/concluir",
            content: null);

        await comprador.PostAsJsonAsync("/api/avaliacoes", new
        {
            pedidoId,
            nota = 4,
            comentario = "Chegou bem embalado"
        });

        var resumo = await comprador.GetFromJsonAsync<JsonElement>(rotaResumo);

        Assert.Equal(4m, resumo.GetProperty("media").GetDecimal());
        Assert.Equal(1, resumo.GetProperty("total").GetInt32());
        Assert.Equal(
            produtoId,
            resumo.GetProperty("avaliacoes")[0].GetProperty("produtoId").GetInt32());
    }

    [Fact]
    public async Task ContaNova_DeveTerPedidosVazios()
    {
        using var fabrica = new FabricaApi();
        using var comprador = CriarCliente(fabrica);

        await CadastrarELogar(comprador, "Bruno Sa", EmailUnico("bruno"), "Comprador");

        var pedidos = await comprador.GetFromJsonAsync<JsonElement[]>("/api/pedidos");

        Assert.NotNull(pedidos);
        Assert.Empty(pedidos!);
    }

    [Fact]
    public async Task Checkout_SemEndereco_DeveRetornar400()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);
        using var comprador = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"), "Vendedor");
        await CadastrarELogar(comprador, "Bruno Sa", EmailUnico("bruno"), "Comprador");

        int produtoId = await CriarAnuncio(vendedor);

        var resposta = await comprador.PostAsJsonAsync("/api/pedidos", new { produtoId });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task ContaUnica_PodePublicarEComprar()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);
        using var comprador = CriarCliente(fabrica);

        // O cadastro não pergunta mais se a conta é de comprador ou vendedor.
        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"));
        await CadastrarELogar(comprador, "Bruno Sa", EmailUnico("bruno"));

        int produtoId = await CriarAnuncio(vendedor);

        var compra = await FazerCheckout(comprador, produtoId);

        Assert.Equal(HttpStatusCode.Created, compra.StatusCode);

        // A mesma conta que comprou também publica o próprio anúncio.
        var publicacao = await comprador.PostAsJsonAsync("/api/produtos", new
        {
            nome = "HQ do Bruno",
            descricao = "Item de coleção",
            preco = 80m,
            estoque = 1,
            categoria = "Funko Pop",
            marca = "Panini",
            colecao = "Marvel",
            condicao = "Usado",
            imagemUrl = string.Empty,
            modalidade = "PrecoFixo"
        });

        Assert.Equal(HttpStatusCode.Created, publicacao.StatusCode);
    }

    [Fact]
    public async Task Checkout_DeveGuardarEntregaEPagamento()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);
        using var comprador = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"), "Vendedor");
        await CadastrarELogar(comprador, "Bruno Sa", EmailUnico("bruno"), "Comprador");

        int produtoId = await CriarAnuncio(vendedor);

        var resposta = await FazerCheckout(comprador, produtoId);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var compra = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var pedido = compra.GetProperty("pedidos")[0];

        Assert.Equal("Pix", compra.GetProperty("forma").GetString());
        Assert.Equal("Pix", pedido.GetProperty("formaPagamento").GetString());
        Assert.Equal("01310-100", pedido.GetProperty("entrega").GetProperty("cep").GetString());
        Assert.Equal("São Paulo", pedido.GetProperty("entrega").GetProperty("cidade").GetString());
    }

    [Fact]
    public async Task Admin_DeveNegarAcessoParaContaComum()
    {
        using var fabrica = new FabricaApi();
        using var comum = CriarCliente(fabrica);

        await CadastrarELogar(comum, "Bruno Sa", EmailUnico("bruno"), "Comprador");

        var resposta = await comum.GetAsync("/api/admin/resumo");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Admin_DeveListarUsuariosEPedidos()
    {
        using var fabrica = new FabricaApi();
        using var administrador = CriarCliente(fabrica);

        var login = await administrador.PostAsJsonAsync(
            "/api/autenticacao/entrar",
            new { email = "admin@rarityroom.com", senha = FabricaApi.SenhaAdmin });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var resumo = await administrador.GetFromJsonAsync<JsonElement>(
            "/api/admin/resumo");

        Assert.True(resumo.GetProperty("usuarios").GetInt32() >= 1);

        var usuarios = await administrador.GetFromJsonAsync<JsonElement[]>(
            "/api/admin/usuarios");

        Assert.Contains(
            usuarios!,
            usuario => usuario.GetProperty("administrador").GetBoolean());

        var pedidos = await administrador.GetAsync("/api/admin/pedidos");

        Assert.Equal(HttpStatusCode.OK, pedidos.StatusCode);
    }

    [Fact]
    public async Task Admin_DeveSuspenderContaEImpedirLogin()
    {
        using var fabrica = new FabricaApi();
        using var administrador = CriarCliente(fabrica);
        using var vitima = CriarCliente(fabrica);

        string email = EmailUnico("vitima");

        await CadastrarELogar(vitima, "Vitima Silva", email, "Comprador");

        await administrador.PostAsJsonAsync(
            "/api/autenticacao/entrar",
            new { email = "admin@rarityroom.com", senha = FabricaApi.SenhaAdmin });

        var usuarios = await administrador.GetFromJsonAsync<JsonElement[]>(
            "/api/admin/usuarios");

        int alvo = usuarios!
            .Single(usuario => usuario.GetProperty("email").GetString() == email)
            .GetProperty("id")
            .GetInt32();

        var suspensao = await administrador.PostAsync(
            "/api/admin/usuarios/" + alvo + "/suspender",

            content: null);
        Assert.Equal(HttpStatusCode.OK, suspensao.StatusCode);

        using var novaSessao = CriarCliente(fabrica);

        var login = await novaSessao.PostAsJsonAsync(
            "/api/autenticacao/entrar",
            new { email, senha = "senha123" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Loja_DeveSerCriadaPeloVendedorEAparecerNoPerfil()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);

        int vendedorId = await CadastrarELogar(
            vendedor, "Ana Lima", EmailUnico("ana"), "Vendedor");

        var criacao = await vendedor.PostAsJsonAsync("/api/lojas", new
        {
            nome = "Coleção da Ana",
            descricao = "Funko, mangás e figure imports."
        });

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var loja = await criacao.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Coleção da Ana", loja.GetProperty("nome").GetString());

        var perfil = await vendedor.GetFromJsonAsync<JsonElement>(
            "/api/vendedores/" + vendedorId);

        Assert.Equal(
            "Coleção da Ana",
            perfil.GetProperty("loja").GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Loja_NaoPodeSerDuplicada()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = CriarCliente(fabrica);

        await CadastrarELogar(vendedor, "Ana Lima", EmailUnico("ana"), "Vendedor");

        await vendedor.PostAsJsonAsync("/api/lojas", new { nome = "Loja A" });

        var segunda = await vendedor.PostAsJsonAsync(
            "/api/lojas",
            new { nome = "Loja B" });

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Loja_QualquerContaPodeCriar()
    {
        using var fabrica = new FabricaApi();
        using var cliente = CriarCliente(fabrica);

        await CadastrarELogar(cliente, "Bruno Sa", EmailUnico("bruno"));

        var resposta = await cliente.PostAsJsonAsync(
            "/api/lojas",
            new { nome = "Loja do Bruno" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Perfil_DeveAtualizarNomeEEmail()
    {
        using var fabrica = new FabricaApi();
        using var cliente = CriarCliente(fabrica);

        await CadastrarELogar(cliente, "Ana Lima", EmailUnico("ana"), "Vendedor");

        var nome = await cliente.PutAsJsonAsync(
            "/api/perfil/nome",
            new { nome = "Ana Souza" });

        Assert.Equal(HttpStatusCode.OK, nome.StatusCode);

        string novoEmail = EmailUnico("novo");

        var email = await cliente.PutAsJsonAsync(
            "/api/perfil/email",
            new { email = novoEmail, senhaAtual = "senha123" });

        Assert.Equal(HttpStatusCode.OK, email.StatusCode);

        var perfil = await cliente.GetFromJsonAsync<JsonElement>("/api/perfil");

        Assert.Equal("Ana Souza", perfil.GetProperty("nome").GetString());
        Assert.Equal(novoEmail, perfil.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Enderecos_PrimeiroEhPrincipalEAoRemoverOutroAssume()
    {
        using var fabrica = new FabricaApi();
        using var cliente = CriarCliente(fabrica);

        await CadastrarELogar(cliente, "Ana Lima", EmailUnico("ana"), "Comprador");

        var primeiro = await cliente.PostAsJsonAsync("/api/enderecos", new
        {
            cep = "01310-100",
            logradouro = "Avenida Paulista",
            numero = "1000",
            complemento = "",
            bairro = "Bela Vista",
            cidade = "Sao Paulo",
            estado = "SP"
        });

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var segundo = await cliente.PostAsJsonAsync("/api/enderecos", new
        {
            cep = "04538-133",
            logradouro = "Avenida Brigadeiro Faria Lima",
            numero = "200",
            complemento = "casa 2",
            bairro = "Itaim Bibi",
            cidade = "Sao Paulo",
            estado = "SP"
        });

        Assert.Equal(HttpStatusCode.Created, segundo.StatusCode);

        var lista = await cliente.GetFromJsonAsync<JsonElement[]>("/api/enderecos");

        Assert.Equal(2, lista!.Length);
        Assert.True(lista[0].GetProperty("principal").GetBoolean());

        int principalId = lista[0].GetProperty("id").GetInt32();

        var remocao = await cliente.DeleteAsync("/api/enderecos/" + principalId);

        Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);

        var restante = await cliente.GetFromJsonAsync<JsonElement[]>("/api/enderecos");

        Assert.Single(restante!);
        Assert.True(restante![0].GetProperty("principal").GetBoolean());
    }

    [Fact]
    public async Task RedefinicaoDeSenha_DeveTrocarASenha()
    {
        using var fabrica = new FabricaApi();
        using var cliente = CriarCliente(fabrica);

        string email = EmailUnico("recupera");

        await CadastrarELogar(cliente, "Ana Lima", email, "Comprador");

        var pedido = await cliente.PostAsJsonAsync(
            "/api/autenticacao/recuperar-senha",
            new { email });

        var corpo = await pedido.Content.ReadFromJsonAsync<JsonElement>();

        // O token nunca volta na resposta: chega só pelo "e-mail".
        Assert.False(corpo.TryGetProperty("token", out _));

        string token = fabrica.CaixaDeEmail.UltimoToken(email)!;

        Assert.False(string.IsNullOrWhiteSpace(token));

        var redefinicao = await cliente.PostAsJsonAsync(
            "/api/autenticacao/redefinir-senha",
            new { email, token, novaSenha = "novaSenha456" });

        Assert.Equal(HttpStatusCode.OK, redefinicao.StatusCode);

        using var outraSessao = CriarCliente(fabrica);

        var antiga = await outraSessao.PostAsJsonAsync(
            "/api/autenticacao/entrar",
            new { email, senha = "senha123" });

        Assert.Equal(HttpStatusCode.Unauthorized, antiga.StatusCode);

        var nova = await outraSessao.PostAsJsonAsync(
            "/api/autenticacao/entrar",
            new { email, senha = "novaSenha456" });

        Assert.Equal(HttpStatusCode.OK, nova.StatusCode);
    }
}
