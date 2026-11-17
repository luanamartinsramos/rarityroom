using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RarityRoom.Tests.Api;

/// <summary>
/// Critérios de aceitação CA1, CA3, CA4 e CA5 de
/// docs/av2/especificacao-dscode.md, pela API real com releitura do banco.
/// </summary>
public class ReabastecerApiTests
{
    private static HttpClient Cliente(FabricaApi fabrica) =>
        fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task Entrar(HttpClient cliente, string nome)
    {
        string email = $"{nome}{Guid.NewGuid():N}@teste.com";
        await cliente.PostAsJsonAsync("/api/autenticacao/cadastrar", new { nome = nome + " Teste", email, senha = "senha123" });
        await cliente.PostAsJsonAsync("/api/autenticacao/entrar", new { email, senha = "senha123" });
    }

    private static async Task<JsonElement> Anuncio(HttpClient cliente, int id) =>
        await cliente.GetFromJsonAsync<JsonElement>($"/api/produtos/{id}");

    [Fact]
    public async Task CA1_DonoReabasteceAnuncioEsgotado_VoltaAoCatalogo()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "ana");
        await Entrar(comprador, "bruno");

        var criado = await vendedor.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Peça Esgotável", descricao = "", preco = 50m, estoque = 1,
            categoria = "Funko Pop", marca = "", colecao = "", condicao = "", imagemUrl = ""
        });
        int id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        await FluxoApiTests.ComprarEPagar(comprador, id);
        Assert.Equal("Vendido", (await Anuncio(vendedor, id)).GetProperty("estado").GetString());

        var resposta = await vendedor.PostAsJsonAsync($"/api/produtos/{id}/reabastecer", new { quantidade = 5 });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var relido = await Anuncio(vendedor, id);
        Assert.Equal(5, relido.GetProperty("estoque").GetInt32());
        Assert.Equal("Disponivel", relido.GetProperty("estado").GetString());

        var catalogo = await comprador.GetFromJsonAsync<JsonElement[]>("/api/produtos?termo=esgotavel");
        Assert.Single(catalogo!);
    }

    [Fact]
    public async Task CA3_CA4_CA5_Recusas_NaoAlteramOAnuncio()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var terceiro = Cliente(fabrica);
        await Entrar(vendedor, "ana");
        await Entrar(terceiro, "ivo");

        var criado = await vendedor.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Funko Negociado", descricao = "", preco = 100m, estoque = 2,
            categoria = "Funko Pop", marca = "", colecao = "", condicao = "", imagemUrl = "",
            modalidade = "Liquidacao"
        });
        int id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // CA5 — terceiro.
        var alheio = await terceiro.PostAsJsonAsync($"/api/produtos/{id}/reabastecer", new { quantidade = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, alheio.StatusCode);

        // CA4 — quantidade fora da faixa.
        foreach (int quantidade in new[] { 0, 1001 })
        {
            var invalida = await vendedor.PostAsJsonAsync($"/api/produtos/{id}/reabastecer", new { quantidade });
            Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        }

        // CA3 — reservado por proposta aceita.
        var proposta = await (await terceiro.PostAsJsonAsync("/api/propostas", new { produtoId = id, valor = 80m, mensagem = "" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        await vendedor.PostAsync($"/api/propostas/{proposta.GetProperty("id").GetInt32()}/aceitar", null);

        var reservado = await vendedor.PostAsJsonAsync($"/api/produtos/{id}/reabastecer", new { quantidade = 1 });
        var problema = await reservado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Conflict, reservado.StatusCode);
        Assert.Equal("transicao-invalida", problema.GetProperty("codigo").GetString());

        var relido = await Anuncio(vendedor, id);
        Assert.Equal(2, relido.GetProperty("estoque").GetInt32());
        Assert.Equal("Reservado", relido.GetProperty("estado").GetString());
    }
}
