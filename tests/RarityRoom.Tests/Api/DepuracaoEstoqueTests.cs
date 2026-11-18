using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace RarityRoom.Tests.Api;

/// <summary>
/// Reprodução do defeito investigado em docs/av2/debugging.md (DEF-01):
/// estorno de compra paga somava a quantidade ao estoque em int sem
/// verificação, e um anúncio com estoque no limite do tipo ficava com
/// estoque negativo. O teste percorre o caminho real (API → serviço →
/// domínio → SQLite) e registra no log do teste os valores observados.
/// Na versão 166c21d ele falha (estoque -2147483648); a partir de 116d245 passa.
/// </summary>
public class DepuracaoEstoqueTests
{
    private readonly ITestOutputHelper log;

    public DepuracaoEstoqueTests(ITestOutputHelper log)
    {
        this.log = log;
    }

    [Fact]
    public async Task EstornoDeCompraPaga_ComEstoqueNoLimite_NaoDeixaEstoqueNegativo()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);

        await Entrar(vendedor, "Vera");
        await Entrar(comprador, "Caio");

        var criado = await vendedor.PostAsJsonAsync("/api/produtos", Anuncio(estoque: 2));
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        int produtoId = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        int pedidoId = await FluxoApiTests.ComprarEPagar(comprador, produtoId);
        log.WriteLine($"[1] compra paga        → estoque = {await Estoque(vendedor, produtoId)}");

        // O vendedor repõe o estoque ao máximo que o tipo int comporta.
        var edicao = await vendedor.PutAsJsonAsync($"/api/produtos/{produtoId}", Anuncio(estoque: int.MaxValue));
        log.WriteLine($"[2] PUT estoque={int.MaxValue} → HTTP {(int)edicao.StatusCode}; estoque = {await Estoque(vendedor, produtoId)}");

        var cancelamento = await comprador.PostAsync($"/api/pedidos/{pedidoId}/cancelar", null);
        int estoqueFinal = await Estoque(vendedor, produtoId);
        log.WriteLine($"[3] cancelar pedido pago → HTTP {(int)cancelamento.StatusCode}; estoque = {estoqueFinal}");

        Assert.Equal(HttpStatusCode.OK, cancelamento.StatusCode);
        Assert.True(estoqueFinal >= 0, $"Estoque negativo após estorno: {estoqueFinal}");
    }

    private static object Anuncio(int estoque)
    {
        return new
        {
            nome = "Estátua Limitada",
            descricao = "Item para depuração",
            preco = 100m,
            estoque,
            categoria = "Action Figures",
            marca = "Marca",
            colecao = "Coleção",
            condicao = "Novo",
            imagemUrl = string.Empty,
            modalidade = "PrecoFixo"
        };
    }

    private static HttpClient Cliente(FabricaApi fabrica)
    {
        return fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static async Task Entrar(HttpClient cliente, string nome)
    {
        string email = $"{nome.ToLowerInvariant()}{Guid.NewGuid():N}@teste.com";

        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync(
            "/api/autenticacao/cadastrar",
            new { nome = nome + " Teste", email, senha = "senha123" })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync(
            "/api/autenticacao/entrar",
            new { email, senha = "senha123" })).StatusCode);
    }

    private static async Task<int> Estoque(HttpClient cliente, int produtoId)
    {
        var produto = await cliente.GetFromJsonAsync<JsonElement>($"/api/produtos/{produtoId}");

        return produto.GetProperty("estoque").GetInt32();
    }
}
