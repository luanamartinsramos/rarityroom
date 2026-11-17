using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RarityRoom.Tests.Api;

/// <summary>
/// Cenários obrigatórios do marketplace, de ponta a ponta pela API HTTP real:
/// pesquisa e ordenação, carrinho e estoque, checkout, pagamentos simulados,
/// concorrência e segurança. Cada teste usa um banco SQLite próprio.
/// </summary>
public class MarketplaceApiTests
{
    private static HttpClient Cliente(FabricaApi fabrica)
    {
        return fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static async Task<int> Entrar(HttpClient cliente, string nome)
    {
        string email = $"{nome.ToLowerInvariant()}{Guid.NewGuid():N}@teste.com";

        var cadastro = await cliente.PostAsJsonAsync(
            "/api/autenticacao/cadastrar",
            new { nome = nome + " Teste", email, senha = "senha123" });

        Assert.Equal(HttpStatusCode.OK, cadastro.StatusCode);

        var login = await cliente.PostAsJsonAsync(
            "/api/autenticacao/entrar",
            new { email, senha = "senha123" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var corpo = await cadastro.Content.ReadFromJsonAsync<JsonElement>();

        return corpo.GetProperty("usuario").GetProperty("id").GetInt32();
    }

    private static async Task<int> Anunciar(
        HttpClient vendedor,
        string nome,
        decimal preco = 100m,
        int estoque = 3,
        string categoria = "Funko Pop",
        string descricao = "Item de coleção",
        string franquia = "")
    {
        var resposta = await vendedor.PostAsJsonAsync("/api/produtos", new
        {
            nome,
            descricao,
            preco,
            estoque,
            categoria,
            marca = "Marca",
            colecao = "Coleção",
            condicao = "Novo",
            imagemUrl = string.Empty,
            franquia
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return (await resposta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resposta)
    {
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string[]> Nomes(HttpClient cliente, string consulta)
    {
        var itens = await cliente.GetFromJsonAsync<JsonElement[]>("/api/produtos?" + consulta);

        return itens!.Select(item => item.GetProperty("nome").GetString()!).ToArray();
    }

    private static Task<HttpResponseMessage> AdicionarAoCarrinho(
        HttpClient cliente,
        int produtoId,
        int quantidade = 1)
    {
        return cliente.PostAsJsonAsync("/api/carrinho/itens", new { produtoId, quantidade });
    }

    private static async Task<JsonElement> Checkout(
        HttpClient cliente,
        string forma = "Pix",
        string? chave = null)
    {
        var resposta = await cliente.PostAsJsonAsync(
            "/api/pedidos",
            FluxoApiTests.Checkout(forma, chave));

        Assert.True(
            resposta.StatusCode == HttpStatusCode.Created,
            await resposta.Content.ReadAsStringAsync());

        return await Json(resposta);
    }

    private static async Task<int> Estoque(HttpClient cliente, int produtoId)
    {
        var produto = await cliente.GetFromJsonAsync<JsonElement>($"/api/produtos/{produtoId}");

        return produto.GetProperty("estoque").GetInt32();
    }

    private static object CartaoValido(string numero = "4111 1111 1111 1111", int parcelas = 1)
    {
        return new
        {
            nomeImpresso = "BRUNO TESTE",
            numero,
            validade = "12/30",
            cvv = "123",
            cpf = "529.982.247-25",
            parcelas
        };
    }

    // ==================================================================
    // Pesquisa e ordenação
    // ==================================================================

    [Fact]
    public async Task Pesquisa_ExistenteInexistenteParcialEPalavrasForaDeOrdem()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        await Entrar(vendedor, "Ana");

        await Anunciar(vendedor, "Funko Batman Clássico");
        await Anunciar(vendedor, "Katana Demon Slayer", categoria: "Animes", franquia: "Demon Slayer");
        await Anunciar(vendedor, "Carta Charizard", categoria: "Pokémon", descricao: "Holográfica rara");

        Assert.Equal(new[] { "Funko Batman Clássico" }, await Nomes(vendedor, "termo=Funko%20Batman"));
        Assert.Empty(await Nomes(vendedor, "termo=dragon%20ball"));

        // Parte do nome.
        Assert.Equal(new[] { "Funko Batman Clássico" }, await Nomes(vendedor, "termo=bat"));
        Assert.Equal(new[] { "Carta Charizard" }, await Nomes(vendedor, "termo=chari"));

        // Palavras fora de ordem e sem acento.
        Assert.Equal(new[] { "Funko Batman Clássico" }, await Nomes(vendedor, "termo=classico%20batman"));

        // Categoria (sem acento), característica (descrição) e franquia.
        Assert.Equal(new[] { "Carta Charizard" }, await Nomes(vendedor, "termo=pokemon"));
        Assert.Equal(new[] { "Carta Charizard" }, await Nomes(vendedor, "termo=holografica"));
        Assert.Equal(new[] { "Katana Demon Slayer" }, await Nomes(vendedor, "categoria=animes"));
    }

    [Fact]
    public async Task Relevancia_NomeVemAntesDeDescricao()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        await Entrar(vendedor, "Ana");

        await Anunciar(vendedor, "Caneca Geek", descricao: "Estampa do Goku");
        await Anunciar(vendedor, "Action Figure Goku", categoria: "Action Figures");

        var nomes = await Nomes(vendedor, "termo=goku");

        Assert.Equal(new[] { "Action Figure Goku", "Caneca Geek" }, nomes);
    }

    [Fact]
    public async Task Ordenacao_PrecoEDataEPaginacao()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        await Entrar(vendedor, "Ana");

        await Anunciar(vendedor, "Item Medio", preco: 50m);
        await Anunciar(vendedor, "Item Caro", preco: 90m);
        await Anunciar(vendedor, "Item Barato", preco: 10m);

        Assert.Equal(
            new[] { "Item Barato", "Item Medio", "Item Caro" },
            await Nomes(vendedor, "ordem=MenorPreco"));

        Assert.Equal(
            new[] { "Item Caro", "Item Medio", "Item Barato" },
            await Nomes(vendedor, "ordem=MaiorPreco"));

        Assert.Equal(
            new[] { "Item Barato", "Item Caro", "Item Medio" },
            await Nomes(vendedor, "ordem=MaisRecentes"));

        Assert.Equal(
            new[] { "Item Medio", "Item Caro", "Item Barato" },
            await Nomes(vendedor, "ordem=MaisAntigos"));

        // Paginação respeitando a ordenação.
        var pagina2 = await vendedor.GetAsync("/api/produtos?ordem=MenorPreco&pagina=2&tamanhoPagina=2");
        var itens = await pagina2.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal("3", pagina2.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal("Item Caro", Assert.Single(itens!).GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Ordenacao_MaisVendidosEMelhorAvaliados_UsamDadosReais()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int semVenda = await Anunciar(vendedor, "Sem Venda", estoque: 5);
        int vendeUm = await Anunciar(vendedor, "Vende Um", estoque: 5);
        int vendeDois = await Anunciar(vendedor, "Vende Dois", estoque: 5);

        // Sem histórico: ninguém tem vendas nem avaliações (nada inventado).
        var catalogo = await vendedor.GetFromJsonAsync<JsonElement[]>("/api/produtos");
        Assert.All(catalogo!, item =>
        {
            Assert.Equal(0, item.GetProperty("vendas").GetInt32());
            Assert.Equal(0, item.GetProperty("totalAvaliacoes").GetInt32());
        });

        // Compra paga: 1 unidade de "Vende Um" e 2 de "Vende Dois".
        await AdicionarAoCarrinho(comprador, vendeUm, 1);
        await AdicionarAoCarrinho(comprador, vendeDois, 2);
        var compra = await Checkout(comprador);
        await comprador.PostAsync($"/api/pagamentos/{compra.GetProperty("id").GetInt32()}/simular-confirmacao", null);

        // Compra não paga não conta como venda.
        await AdicionarAoCarrinho(comprador, semVenda, 3);
        await Checkout(comprador);

        Assert.Equal(
            new[] { "Vende Dois", "Vende Um", "Sem Venda" },
            await Nomes(vendedor, "ordem=MaisVendidos"));

        // Avaliações reais: "Vende Um" nota 5, "Vende Dois" nota 3.
        int pedidoId = compra.GetProperty("pedidos")[0].GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await vendedor.PostAsync($"/api/pedidos/{pedidoId}/concluir", null)).StatusCode);

        var nota5 = await comprador.PostAsJsonAsync("/api/avaliacoes", new { pedidoId, produtoId = vendeUm, nota = 5, comentario = "" });
        var nota3 = await comprador.PostAsJsonAsync("/api/avaliacoes", new { pedidoId, produtoId = vendeDois, nota = 3, comentario = "" });
        Assert.Equal(HttpStatusCode.Created, nota5.StatusCode);
        Assert.Equal(HttpStatusCode.Created, nota3.StatusCode);

        // Sem avaliação vai para o fim.
        Assert.Equal(
            new[] { "Vende Um", "Vende Dois", "Sem Venda" },
            await Nomes(vendedor, "ordem=MelhorAvaliados"));
    }

    [Fact]
    public async Task Vitrine_NaoMostraAnuncioEsgotado()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int unico = await Anunciar(vendedor, "Peça Única", estoque: 1);
        await FluxoApiTests.ComprarEPagar(comprador, unico);

        Assert.Empty(await Nomes(vendedor, "termo=peca"));

        // O vendedor continua vendo o próprio anúncio vendido.
        var meus = await vendedor.GetFromJsonAsync<JsonElement[]>("/api/produtos/meus");
        Assert.Equal("Vendido", Assert.Single(meus!).GetProperty("estado").GetString());
    }

    // ==================================================================
    // Carrinho e estoque
    // ==================================================================

    [Fact]
    public async Task Carrinho_AdicionarAlterarRemover_ERespeitarEstoque()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int tres = await Anunciar(vendedor, "Três Unidades", preco: 10m, estoque: 3);
        int outro = await Anunciar(vendedor, "Outro Item", preco: 5m, estoque: 1);

        // "Catálogo" e "detalhes" usam o mesmo endpoint: 1 + 2 unidades.
        Assert.Equal(HttpStatusCode.OK, (await AdicionarAoCarrinho(comprador, tres, 1)).StatusCode);
        var carrinho = await Json(await AdicionarAoCarrinho(comprador, tres, 2));
        Assert.Equal(3, carrinho.GetProperty("itens")[0].GetProperty("quantidade").GetInt32());

        // A quarta unidade não cabe no estoque.
        var quarta = await AdicionarAoCarrinho(comprador, tres, 1);
        Assert.Equal(HttpStatusCode.Conflict, quarta.StatusCode);
        Assert.Contains("3 unidade", (await Json(quarta)).GetProperty("detail").GetString());

        // Quantidade zero ou negativa é rejeitada.
        Assert.Equal(HttpStatusCode.BadRequest, (await AdicionarAoCarrinho(comprador, outro, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AdicionarAoCarrinho(comprador, outro, -2)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await comprador.PutAsJsonAsync($"/api/carrinho/itens/{tres}", new { quantidade = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await comprador.PutAsJsonAsync($"/api/carrinho/itens/{tres}", new { quantidade = 4 })).StatusCode);

        // Diminuir e somar outro produto: total recalculado no servidor.
        await comprador.PutAsJsonAsync($"/api/carrinho/itens/{tres}", new { quantidade = 2 });
        carrinho = await Json(await AdicionarAoCarrinho(comprador, outro, 1));
        Assert.Equal(25m, carrinho.GetProperty("total").GetDecimal());
        Assert.Equal(3, carrinho.GetProperty("quantidadeItens").GetInt32());
        Assert.Equal("Ana Teste", carrinho.GetProperty("itens")[0].GetProperty("vendedorNome").GetString());

        // Remover individualmente.
        carrinho = await Json(await comprador.DeleteAsync($"/api/carrinho/itens/{outro}"));
        Assert.Single(carrinho.GetProperty("itens").EnumerateArray());

        // Adicionar ao carrinho não altera o anúncio.
        Assert.Equal(3, await Estoque(vendedor, tres));
    }

    [Fact]
    public async Task Carrinho_PersisteEntreSessoes()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");

        string email = $"bruno{Guid.NewGuid():N}@teste.com";
        await comprador.PostAsJsonAsync("/api/autenticacao/cadastrar", new { nome = "Bruno Teste", email, senha = "senha123" });
        await comprador.PostAsJsonAsync("/api/autenticacao/entrar", new { email, senha = "senha123" });

        int produto = await Anunciar(vendedor, "Item Persistente");
        await AdicionarAoCarrinho(comprador, produto, 2);

        // Outro navegador, mesma conta.
        using var outroNavegador = Cliente(fabrica);
        await outroNavegador.PostAsJsonAsync("/api/autenticacao/entrar", new { email, senha = "senha123" });

        var carrinho = await outroNavegador.GetFromJsonAsync<JsonElement>("/api/carrinho");
        Assert.Equal(2, carrinho.GetProperty("itens")[0].GetProperty("quantidade").GetInt32());
    }

    [Fact]
    public async Task Carrinho_NaoAceitaAnuncioProprioNemSemLogin()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var anonimo = Cliente(fabrica);
        await Entrar(vendedor, "Ana");

        int meu = await Anunciar(vendedor, "Meu Item");

        Assert.Equal(HttpStatusCode.BadRequest, (await AdicionarAoCarrinho(vendedor, meu)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AdicionarAoCarrinho(anonimo, meu)).StatusCode);
    }

    // ==================================================================
    // Checkout e pagamentos
    // ==================================================================

    [Fact]
    public async Task Checkout_SemAutenticacao_DeveRetornar401()
    {
        using var fabrica = new FabricaApi();
        using var anonimo = Cliente(fabrica);

        var resposta = await anonimo.PostAsJsonAsync("/api/pedidos", FluxoApiTests.Checkout());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Checkout_CarrinhoVazio_DeveRetornar400()
    {
        using var fabrica = new FabricaApi();
        using var comprador = Cliente(fabrica);
        await Entrar(comprador, "Bruno");

        var resposta = await comprador.PostAsJsonAsync("/api/pedidos", FluxoApiTests.Checkout());

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Pix_EstoqueSoBaixaDepoisDaAprovacao()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "Funko Pix", preco: 40m, estoque: 3);
        await AdicionarAoCarrinho(comprador, produto, 2);

        var compra = await Checkout(comprador, "Pix");
        int id = compra.GetProperty("id").GetInt32();

        Assert.Equal("Pendente", compra.GetProperty("status").GetString());
        Assert.Equal(80m, compra.GetProperty("valor").GetDecimal());
        Assert.Contains("SIMULACAO", compra.GetProperty("codigoPix").GetString());
        Assert.True(compra.GetProperty("simulacao").GetBoolean());

        // Aguardando pagamento: estoque e carrinho intactos.
        Assert.Equal(3, await Estoque(vendedor, produto));
        var carrinho = await comprador.GetFromJsonAsync<JsonElement>("/api/carrinho");
        Assert.Single(carrinho.GetProperty("itens").EnumerateArray());

        var pago = await Json(await comprador.PostAsync($"/api/pagamentos/{id}/simular-confirmacao", null));

        Assert.Equal("Pago", pago.GetProperty("status").GetString());
        Assert.Equal("Pago", pago.GetProperty("pedidos")[0].GetProperty("estado").GetString());
        Assert.Equal(1, await Estoque(vendedor, produto));

        // Itens comprados saem do carrinho só depois da aprovação.
        carrinho = await comprador.GetFromJsonAsync<JsonElement>("/api/carrinho");
        Assert.Empty(carrinho.GetProperty("itens").EnumerateArray());

        // Confirmar de novo não baixa o estoque outra vez.
        await comprador.PostAsync($"/api/pagamentos/{id}/simular-confirmacao", null);
        Assert.Equal(1, await Estoque(vendedor, produto));

        // Histórico de compras.
        var pedidos = await comprador.GetFromJsonAsync<JsonElement[]>("/api/pedidos");
        var pedido = Assert.Single(pedidos!);
        Assert.StartsWith("RR-", pedido.GetProperty("codigo").GetString());
        Assert.Equal(2, pedido.GetProperty("itens")[0].GetProperty("quantidade").GetInt32());
        Assert.Equal(40m, pedido.GetProperty("itens")[0].GetProperty("precoUnitario").GetDecimal());
        Assert.Equal("Pago", pedido.GetProperty("statusPagamento").GetString());
    }

    [Fact]
    public async Task Boleto_GeraLinhaDigitavelEConfirma()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "HQ Boleto", preco: 123.45m, estoque: 1);
        await AdicionarAoCarrinho(comprador, produto);

        var compra = await Checkout(comprador, "Boleto");

        Assert.Equal(47, compra.GetProperty("codigoBarras").GetString()!.Length);
        Assert.True(compra.GetProperty("expiraEm").GetDateTime() > DateTime.UtcNow.AddDays(2));

        var pago = await Json(await comprador.PostAsync(
            $"/api/pagamentos/{compra.GetProperty("id").GetInt32()}/simular-confirmacao", null));

        Assert.Equal("Pago", pago.GetProperty("status").GetString());
        Assert.Equal(0, await Estoque(vendedor, produto));
    }

    [Fact]
    public async Task Cartao_AprovadoGuardaSoFinal_ERecusadoNaoMexeNoEstoque()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "Estátua Cartão", preco: 300m, estoque: 2);

        // Recusa simulada.
        await AdicionarAoCarrinho(comprador, produto);
        var compra = await Checkout(comprador, "CartaoCredito");
        int id = compra.GetProperty("id").GetInt32();

        // Simular confirmação não serve para cartão.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await comprador.PostAsync($"/api/pagamentos/{id}/simular-confirmacao", null)).StatusCode);

        // Dados inválidos: 400 e a compra continua aguardando.
        var invalido = await comprador.PostAsJsonAsync($"/api/pagamentos/{id}/cartao",
            new { nomeImpresso = "", numero = "1234", validade = "01/20", cvv = "1", parcelas = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        var recusado = await Json(await comprador.PostAsJsonAsync(
            $"/api/pagamentos/{id}/cartao", CartaoValido("4000 0000 0000 0002")));

        Assert.Equal("Recusado", recusado.GetProperty("status").GetString());
        Assert.Equal("Recusado", recusado.GetProperty("pedidos")[0].GetProperty("statusPagamento").GetString());
        Assert.Equal(2, await Estoque(vendedor, produto));

        // O carrinho continua com o item para tentar de novo.
        var carrinho = await comprador.GetFromJsonAsync<JsonElement>("/api/carrinho");
        Assert.Single(carrinho.GetProperty("itens").EnumerateArray());

        // Nova tentativa aprovada, em 3 parcelas.
        var nova = await Checkout(comprador, "CartaoCredito");
        var aprovado = await comprador.PostAsJsonAsync(
            $"/api/pagamentos/{nova.GetProperty("id").GetInt32()}/cartao", CartaoValido(parcelas: 3));
        string json = await aprovado.Content.ReadAsStringAsync();
        var corpo = JsonDocument.Parse(json).RootElement;

        Assert.Equal("Pago", corpo.GetProperty("status").GetString());
        Assert.Equal("1111", corpo.GetProperty("cartaoFinal").GetString());
        Assert.Equal(3, corpo.GetProperty("parcelas").GetInt32());
        Assert.Equal(100m, corpo.GetProperty("valorParcela").GetDecimal());
        Assert.DoesNotContain("4111111111111111", json);
        Assert.DoesNotContain("\"cvv\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await Estoque(vendedor, produto));
    }

    [Fact]
    public async Task CancelarEExpirar_NaoBaixamEstoque()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "Item Cancelável", estoque: 2);
        await AdicionarAoCarrinho(comprador, produto);

        var compra = await Checkout(comprador, "Pix");
        var cancelada = await Json(await comprador.PostAsync(
            $"/api/pagamentos/{compra.GetProperty("id").GetInt32()}/cancelar", null));

        Assert.Equal("Cancelado", cancelada.GetProperty("status").GetString());
        Assert.Equal("Cancelado", cancelada.GetProperty("pedidos")[0].GetProperty("estado").GetString());

        var boleto = await Checkout(comprador, "Boleto");
        int boletoId = boleto.GetProperty("id").GetInt32();
        var expirada = await Json(await comprador.PostAsync($"/api/pagamentos/{boletoId}/simular-expiracao", null));

        Assert.Equal("Expirado", expirada.GetProperty("status").GetString());

        // Pagar depois de expirar não é possível.
        Assert.Equal(HttpStatusCode.Conflict,
            (await comprador.PostAsync($"/api/pagamentos/{boletoId}/simular-confirmacao", null)).StatusCode);

        Assert.Equal(2, await Estoque(vendedor, produto));
    }

    [Fact]
    public async Task CancelarPedidoPago_DevolveEstoque()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "Item Estornável", estoque: 1);
        int pedidoId = await FluxoApiTests.ComprarEPagar(comprador, produto);

        Assert.Equal(0, await Estoque(vendedor, produto));

        var cancelado = await Json(await comprador.PostAsync($"/api/pedidos/{pedidoId}/cancelar", null));

        Assert.Equal("Cancelado", cancelado.GetProperty("estado").GetString());
        Assert.Equal("Estornado", cancelado.GetProperty("statusPagamento").GetString());
        Assert.Equal(1, await Estoque(vendedor, produto));
    }

    [Fact]
    public async Task Checkout_RevalidaEstoqueAlteradoDepoisDoCarrinho()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "Item Reduzido", estoque: 3);
        await AdicionarAoCarrinho(comprador, produto, 3);

        // O vendedor reduz o estoque depois que o item foi para o carrinho.
        await vendedor.PutAsJsonAsync($"/api/produtos/{produto}", new
        {
            nome = "Item Reduzido", descricao = "", preco = 100m, estoque = 2,
            categoria = "Funko Pop", marca = "", colecao = "", condicao = "", imagemUrl = ""
        });

        var carrinho = await comprador.GetFromJsonAsync<JsonElement>("/api/carrinho");
        Assert.False(carrinho.GetProperty("podeFinalizar").GetBoolean());
        Assert.False(string.IsNullOrEmpty(carrinho.GetProperty("itens")[0].GetProperty("aviso").GetString()));

        var resposta = await comprador.PostAsJsonAsync("/api/pedidos", FluxoApiTests.Checkout());

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("Estoque insuficiente", (await Json(resposta)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ComprasSimultaneas_DoMesmoEstoque_SoUmaEhAprovada()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        await Entrar(vendedor, "Ana");

        int produto = await Anunciar(vendedor, "Última Unidade", estoque: 1);

        // Cinco compradores colocam a mesma unidade no carrinho e finalizam
        // (o checkout não reserva estoque).
        var compradores = new List<(HttpClient Cliente, int PagamentoId)>();

        for (int i = 0; i < 5; i++)
        {
            var cliente = Cliente(fabrica);
            await Entrar(cliente, "Comprador" + i);
            await AdicionarAoCarrinho(cliente, produto);
            var compra = await Checkout(cliente);
            compradores.Add((cliente, compra.GetProperty("id").GetInt32()));
        }

        // Todos confirmam o pagamento ao mesmo tempo.
        var respostas = await Task.WhenAll(compradores.Select(item =>
            item.Cliente.PostAsync($"/api/pagamentos/{item.PagamentoId}/simular-confirmacao", null)));

        var status = new List<string>();

        foreach (var resposta in respostas)
        {
            if (resposta.StatusCode == HttpStatusCode.OK)
            {
                status.Add((await Json(resposta)).GetProperty("status").GetString()!);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
                status.Add("Conflito");
            }
        }

        Assert.Equal(1, status.Count(valor => valor == "Pago"));
        Assert.Equal(0, await Estoque(vendedor, produto));

        foreach (var item in compradores)
        {
            item.Cliente.Dispose();
        }
    }

    [Fact]
    public async Task MesmaTentativaDeCheckout_NaoDuplicaPedido()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "Item Idempotente");
        await AdicionarAoCarrinho(comprador, produto);

        // Duplo clique: duas requisições com a mesma chave, ao mesmo tempo.
        var corpo = FluxoApiTests.Checkout("Pix", "tentativa-123");
        var respostas = await Task.WhenAll(
            comprador.PostAsJsonAsync("/api/pedidos", corpo),
            comprador.PostAsJsonAsync("/api/pedidos", corpo));

        var ids = new HashSet<int>();

        foreach (var resposta in respostas)
        {
            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            ids.Add((await Json(resposta)).GetProperty("id").GetInt32());
        }

        Assert.Single(ids);

        var pedidos = await comprador.GetFromJsonAsync<JsonElement[]>("/api/pedidos");
        Assert.Single(pedidos!);

        // Um novo checkout substitui a tentativa pendente anterior: nunca há
        // duas compras aguardando pagamento para o mesmo carrinho.
        await Checkout(comprador);
        pedidos = await comprador.GetFromJsonAsync<JsonElement[]>("/api/pedidos");
        Assert.Equal(1, pedidos!.Count(pedido => pedido.GetProperty("estado").GetString() == "Pendente"));
    }

    [Fact]
    public async Task CarrinhoComDoisVendedores_GeraUmPedidoPorVendedor()
    {
        using var fabrica = new FabricaApi();
        using var ana = Cliente(fabrica);
        using var carlos = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        int anaId = await Entrar(ana, "Ana");
        int carlosId = await Entrar(carlos, "Carlos");
        await Entrar(comprador, "Bruno");

        int daAna = await Anunciar(ana, "Item da Ana", preco: 10m);
        int doCarlos = await Anunciar(carlos, "Item do Carlos", preco: 20m);

        await AdicionarAoCarrinho(comprador, daAna, 2);
        await AdicionarAoCarrinho(comprador, doCarlos, 1);

        var compra = await Checkout(comprador);
        var pedidos = compra.GetProperty("pedidos").EnumerateArray().ToList();

        Assert.Equal(40m, compra.GetProperty("valor").GetDecimal());
        Assert.Equal(2, pedidos.Count);
        Assert.Contains(pedidos, pedido => pedido.GetProperty("vendedorId").GetInt32() == anaId
            && pedido.GetProperty("total").GetDecimal() == 20m);
        Assert.Contains(pedidos, pedido => pedido.GetProperty("vendedorId").GetInt32() == carlosId
            && pedido.GetProperty("total").GetDecimal() == 20m);

        // Cada vendedor vê só o próprio pedido.
        Assert.Single((await ana.GetFromJsonAsync<JsonElement[]>("/api/pedidos"))!);
        Assert.Single((await carlos.GetFromJsonAsync<JsonElement[]>("/api/pedidos"))!);
    }

    [Fact]
    public async Task PropostaAceita_PermiteComprarPeloValorDaProposta()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        using var terceiro = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");
        await Entrar(terceiro, "Carla");

        var criado = await vendedor.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Funko Negociável", descricao = "", preco = 100m, estoque = 2,
            categoria = "Funko Pop", marca = "", colecao = "", condicao = "", imagemUrl = "",
            modalidade = "AceitaOferta"
        });
        int produto = (await Json(criado)).GetProperty("id").GetInt32();

        var proposta = await Json(await comprador.PostAsJsonAsync("/api/propostas",
            new { produtoId = produto, valor = 85m, mensagem = "" }));
        var aceite = await vendedor.PostAsync($"/api/propostas/{proposta.GetProperty("id").GetInt32()}/aceitar", null);
        Assert.Equal(HttpStatusCode.OK, aceite.StatusCode);

        // Reservado: só quem teve a proposta aceita compra, e uma unidade.
        Assert.Equal(HttpStatusCode.Conflict, (await AdicionarAoCarrinho(terceiro, produto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await AdicionarAoCarrinho(comprador, produto, 2)).StatusCode);

        var carrinho = await Json(await AdicionarAoCarrinho(comprador, produto));
        Assert.Equal(85m, carrinho.GetProperty("total").GetDecimal());

        var compra = await Checkout(comprador);
        var pago = await Json(await comprador.PostAsync(
            $"/api/pagamentos/{compra.GetProperty("id").GetInt32()}/simular-confirmacao", null));

        Assert.Equal("Pago", pago.GetProperty("status").GetString());

        // Sobrou uma unidade: o anúncio volta a ficar disponível pelo preço normal.
        var anuncio = await vendedor.GetFromJsonAsync<JsonElement>($"/api/produtos/{produto}");
        Assert.Equal(1, anuncio.GetProperty("estoque").GetInt32());
        Assert.Equal("Disponivel", anuncio.GetProperty("estado").GetString());
    }

    // ==================================================================
    // Segurança
    // ==================================================================

    [Fact]
    public async Task Seguranca_PedidoEPagamentoDeOutroUsuario_SaoNegados()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        using var intruso = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");
        await Entrar(intruso, "Ivo");

        int produto = await Anunciar(vendedor, "Item Privado");
        await AdicionarAoCarrinho(comprador, produto);
        var compra = await Checkout(comprador);
        int pagamentoId = compra.GetProperty("id").GetInt32();
        int pedidoId = compra.GetProperty("pedidos")[0].GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.Forbidden, (await intruso.GetAsync($"/api/pedidos/{pedidoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await intruso.GetAsync($"/api/pagamentos/{pagamentoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await intruso.PostAsync($"/api/pagamentos/{pagamentoId}/simular-confirmacao", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await intruso.PostAsync($"/api/pedidos/{pedidoId}/cancelar", null)).StatusCode);

        // Nem o vendedor confirma o pagamento do comprador.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await vendedor.PostAsync($"/api/pagamentos/{pagamentoId}/simular-confirmacao", null)).StatusCode);

        Assert.Empty((await intruso.GetFromJsonAsync<JsonElement[]>("/api/pedidos"))!);
    }

    [Fact]
    public async Task Seguranca_PrecoEQuantidadeEnviadosPeloNavegadorSaoIgnorados()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "Item Caro", preco: 500m, estoque: 2);

        // Campos extras de preço no carrinho não têm efeito.
        await comprador.PostAsJsonAsync("/api/carrinho/itens",
            new { produtoId = produto, quantidade = 1, preco = 0.01m, precoUnitario = 0.01m });

        // Nem no checkout.
        var resposta = await comprador.PostAsJsonAsync("/api/pedidos", new
        {
            cep = "01310-100", logradouro = "Avenida Paulista", numero = "1", bairro = "Centro",
            cidade = "São Paulo", estado = "SP", formaPagamento = "Pix",
            total = 0.01m, valor = 0.01m, quantidade = 99, produtoId = produto,
            itens = new[] { new { produtoId = produto, quantidade = 99, precoUnitario = 0.01m } }
        });

        var compra = await Json(resposta);

        Assert.Equal(500m, compra.GetProperty("valor").GetDecimal());
        Assert.Equal(1, compra.GetProperty("pedidos")[0].GetProperty("itens")[0].GetProperty("quantidade").GetInt32());

        // O antigo endpoint em que o cliente decidia "aprovado" não existe mais.
        int pedidoId = compra.GetProperty("pedidos")[0].GetProperty("id").GetInt32();
        var pagarAntigo = await comprador.PostAsJsonAsync($"/api/pedidos/{pedidoId}/pagar", new { aprovado = true });
        Assert.NotEqual(HttpStatusCode.OK, pagarAntigo.StatusCode);
    }

    [Fact]
    public async Task Seguranca_CompradorNaoEditaNemExcluiAnuncioDeTerceiro()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "Ana");
        await Entrar(comprador, "Bruno");

        int produto = await Anunciar(vendedor, "Item Protegido", preco: 50m);

        var edicao = await comprador.PutAsJsonAsync($"/api/produtos/{produto}", new
        {
            nome = "Hackeado", descricao = "", preco = 1m, estoque = 999,
            categoria = "Funko Pop", marca = "", colecao = "", condicao = "", imagemUrl = ""
        });

        Assert.Equal(HttpStatusCode.Forbidden, edicao.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await comprador.DeleteAsync($"/api/produtos/{produto}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await comprador.PatchAsJsonAsync(
            $"/api/produtos/{produto}/estado", new { operacao = "pausar" })).StatusCode);

        var anuncio = await vendedor.GetFromJsonAsync<JsonElement>($"/api/produtos/{produto}");
        Assert.Equal("Item Protegido", anuncio.GetProperty("nome").GetString());
        Assert.Equal(50m, anuncio.GetProperty("preco").GetDecimal());
    }
}
