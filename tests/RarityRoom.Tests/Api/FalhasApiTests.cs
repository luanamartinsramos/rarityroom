using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using RarityRoom.Middleware;
using Xunit;

namespace RarityRoom.Tests.Api;

/// <summary>
/// As três situações de falha de docs/av2/excecoes.md, verificadas de ponta a
/// ponta: resposta HTTP, código estável e — tão importante quanto — o estado
/// que fica no banco depois da falha.
/// </summary>
public class FalhasApiTests
{
    // FALHA-01 — violação de regra de negócio: transição de estado proibida.
    [Fact]
    public async Task Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        await Entrar(vendedor, "Vera");

        int id = await Anunciar(vendedor, preco: 150m);

        var pausar = await vendedor.PatchAsJsonAsync($"/api/produtos/{id}/estado", new { operacao = "Pausar" });
        Assert.Equal(HttpStatusCode.OK, pausar.StatusCode);

        var vender = await vendedor.PatchAsJsonAsync($"/api/produtos/{id}/estado", new { operacao = "Vender" });

        Assert.Equal(HttpStatusCode.Conflict, vender.StatusCode);
        Assert.Equal("transicao-invalida", (await Json(vender)).GetProperty("codigo").GetString());

        // Releitura do banco (nova requisição, novo escopo do DbContext).
        var produto = await vendedor.GetFromJsonAsync<JsonElement>($"/api/produtos/{id}");
        Assert.Equal("Pausado", produto.GetProperty("estado").GetString());
        Assert.Equal(3, produto.GetProperty("estoque").GetInt32());
    }

    // FALHA-02 — entrada inválida: anúncio com preço zero.
    [Fact]
    public async Task Falha02_AnuncioComPrecoZero_Responde400_ENadaEGravado()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        await Entrar(vendedor, "Vera");

        var resposta = await vendedor.PostAsJsonAsync("/api/produtos", Corpo(preco: 0m));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await Json(resposta);
        Assert.Equal("dados-invalidos", problema.GetProperty("codigo").GetString());
        Assert.Contains("preço", problema.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);

        var catalogo = await vendedor.GetFromJsonAsync<JsonElement[]>("/api/produtos");
        Assert.Empty(catalogo!);
    }

    // FALHA-03 — situação excepcional: erro não previsto em qualquer camada.
    [Fact]
    public async Task Falha03_ErroInesperado_Responde500Generico_SemVazarDetalhes_ERegistraNoLog()
    {
        var logger = new LoggerMemoria();
        var middleware = new MiddlewareExcecoes(
            _ => throw new InvalidOperationException("SQLite Error 14: unable to open database file '/data/rarityroom.db'"),
            logger);

        var contexto = new DefaultHttpContext();
        contexto.Request.Method = "GET";
        contexto.Request.Path = "/api/produtos";
        contexto.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(contexto);

        contexto.Response.Body.Position = 0;
        string corpo = await new StreamReader(contexto.Response.Body, Encoding.UTF8).ReadToEndAsync();
        var problema = JsonDocument.Parse(corpo).RootElement;

        Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        Assert.Equal("erro-interno", problema.GetProperty("codigo").GetString());
        Assert.Equal("Ocorreu um erro inesperado. Tente novamente.", problema.GetProperty("detail").GetString());

        // O detalhe interno não chega ao cliente...
        Assert.DoesNotContain("SQLite", corpo);
        Assert.DoesNotContain("/data/", corpo);
        Assert.DoesNotContain("InvalidOperationException", corpo);

        // ...mas também não é escondido: fica registrado como erro no log.
        var registro = Assert.Single(logger.Registros);
        Assert.Equal(LogLevel.Error, registro.Nivel);
        Assert.IsType<InvalidOperationException>(registro.Excecao);
    }

    private static object Corpo(decimal preco)
    {
        return new
        {
            nome = "Funko Colecionável",
            descricao = "Item de coleção",
            preco,
            estoque = 3,
            categoria = "Funko Pop",
            marca = "Marca",
            colecao = "Coleção",
            condicao = "Novo",
            imagemUrl = string.Empty,
            modalidade = "PrecoFixo"
        };
    }

    private static async Task<int> Anunciar(HttpClient vendedor, decimal preco)
    {
        var resposta = await vendedor.PostAsJsonAsync("/api/produtos", Corpo(preco));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return (await Json(resposta)).GetProperty("id").GetInt32();
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

    private static async Task<JsonElement> Json(HttpResponseMessage resposta)
    {
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private sealed class LoggerMemoria : ILogger<MiddlewareExcecoes>
    {
        public List<(LogLevel Nivel, Exception? Excecao)> Registros { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Registros.Add((logLevel, exception));
        }
    }
}
