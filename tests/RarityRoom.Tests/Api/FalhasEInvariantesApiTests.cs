using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RarityRoom.Modelos;
using RarityRoom.Repositorios;
using Xunit;

namespace RarityRoom.Tests.Api;

/// <summary>
/// Falhas e invariantes pela API real (docs/av2/excecoes.md):
/// S1 — violação de regra de negócio (estoque) → 409 com mensagem de negócio;
/// S2 — entrada inválida (cartão) → 400, sem mudar a compra;
/// S3 — falha excepcional inesperada → 500 genérico, registrada no log, sem
///      vazar detalhes internos;
/// e uma transição de estado proibida que não altera o dado persistido.
/// </summary>
public class FalhasEInvariantesApiTests
{
    private static HttpClient Cliente(WebApplicationFactory<Program> fabrica)
    {
        return fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static async Task Entrar(HttpClient cliente, string nome)
    {
        string email = $"{nome}{Guid.NewGuid():N}@teste.com";

        await cliente.PostAsJsonAsync("/api/autenticacao/cadastrar", new { nome = nome + " Teste", email, senha = "senha123" });
        var login = await cliente.PostAsJsonAsync("/api/autenticacao/entrar", new { email, senha = "senha123" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static async Task<int> Anunciar(HttpClient vendedor, int estoque = 3)
    {
        var resposta = await vendedor.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Funko Teste", descricao = "", preco = 100m, estoque,
            categoria = "Funko Pop", marca = "", colecao = "", condicao = "", imagemUrl = ""
        });

        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    // ------------------------------------------------------------------
    // A06 / F02 — transição proibida não deixa dado inconsistente
    // ------------------------------------------------------------------

    [Fact]
    public async Task TransicaoProibida_ViaApi_EhRejeitadaENaoAlteraOBanco()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        await Entrar(vendedor, "ana");

        int produto = await Anunciar(vendedor, estoque: 3);

        var pausa = await vendedor.PatchAsJsonAsync($"/api/produtos/{produto}/estado", new { operacao = "pausar" });
        Assert.Equal(HttpStatusCode.OK, pausa.StatusCode);

        // Pausado → Vender não existe na tabela de transições.
        var proibida = await vendedor.PatchAsJsonAsync($"/api/produtos/{produto}/estado", new { operacao = "vender" });
        var problema = await proibida.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Conflict, proibida.StatusCode);
        Assert.Equal("transicao-invalida", problema.GetProperty("codigo").GetString());

        // Releitura em nova requisição (novo DbContext): o banco não mudou.
        var relido = await vendedor.GetFromJsonAsync<JsonElement>($"/api/produtos/{produto}");

        Assert.Equal("Pausado", relido.GetProperty("estado").GetString());
        Assert.Equal(3, relido.GetProperty("estoque").GetInt32());
    }

    // ------------------------------------------------------------------
    // D01 / D04 — três situações de falha
    // ------------------------------------------------------------------

    [Fact]
    public async Task S1_RegraDeNegocio_EstoqueInsuficiente_Retorna409SemAlterarCarrinho()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "ana");
        await Entrar(comprador, "bruno");

        int produto = await Anunciar(vendedor, estoque: 3);

        var resposta = await comprador.PostAsJsonAsync("/api/carrinho/itens", new { produtoId = produto, quantidade = 4 });
        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("conflito", problema.GetProperty("codigo").GetString());
        Assert.Contains("Estoque insuficiente", problema.GetProperty("detail").GetString());

        var carrinho = await comprador.GetFromJsonAsync<JsonElement>("/api/carrinho");
        Assert.Empty(carrinho.GetProperty("itens").EnumerateArray());
    }

    [Fact]
    public async Task S2_EntradaInvalida_CartaoInvalido_Retorna400ECompraContinuaAguardando()
    {
        using var fabrica = new FabricaApi();
        using var vendedor = Cliente(fabrica);
        using var comprador = Cliente(fabrica);
        await Entrar(vendedor, "ana");
        await Entrar(comprador, "bruno");

        int produto = await Anunciar(vendedor);
        await comprador.PostAsJsonAsync("/api/carrinho/itens", new { produtoId = produto, quantidade = 1 });
        var compra = await (await comprador.PostAsJsonAsync("/api/pedidos", FluxoApiTests.Checkout("CartaoCredito")))
            .Content.ReadFromJsonAsync<JsonElement>();
        int id = compra.GetProperty("id").GetInt32();

        var resposta = await comprador.PostAsJsonAsync($"/api/pagamentos/{id}/cartao",
            new { nomeImpresso = "ANA", numero = "4111 1111 1111 1112", validade = "12/30", cvv = "123", parcelas = 1 });
        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("dados-invalidos", problema.GetProperty("codigo").GetString());

        var depois = await comprador.GetFromJsonAsync<JsonElement>($"/api/pagamentos/{id}");
        Assert.Equal("Pendente", depois.GetProperty("status").GetString());
    }

    [Fact]
    public async Task S3_FalhaInesperada_Retorna500GenericoSemVazarDetalhes_ERegistraNoLog()
    {
        var registros = new ConcurrentBag<(LogLevel Nivel, string Mensagem, Exception? Erro)>();

        using var fabricaBase = new FabricaApi();
        using var fabrica = fabricaBase.WithWebHostBuilder(construtor =>
        {
            construtor.ConfigureServices(servicos =>
            {
                // Simula o banco indisponível só na consulta de pagamentos.
                servicos.RemoveAll<IRepositorioPagamento>();
                servicos.AddScoped<IRepositorioPagamento, RepositorioPagamentoQuebrado>();

                servicos.AddLogging(log => log.AddProvider(new LogEmMemoria(registros)));
            });
        });

        using var comprador = Cliente(fabrica);
        await Entrar(comprador, "bruno");

        var resposta = await comprador.GetAsync("/api/pedidos");
        string corpo = await resposta.Content.ReadAsStringAsync();
        var problema = JsonDocument.Parse(corpo).RootElement;

        // Ao usuário: 500 com mensagem genérica e código estável.
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal("erro-interno", problema.GetProperty("codigo").GetString());
        Assert.Equal("Ocorreu um erro inesperado. Tente novamente.", problema.GetProperty("detail").GetString());

        // Nada interno vaza: nem a mensagem, nem o tipo, nem o stack trace.
        Assert.DoesNotContain(RepositorioPagamentoQuebrado.Detalhe, corpo);
        Assert.DoesNotContain("InvalidOperationException", corpo);
        Assert.DoesNotContain("   at ", corpo);

        // Não foi ocultada: o erro completo está no log do servidor.
        Assert.Contains(registros, registro =>
            registro.Nivel == LogLevel.Error
            && registro.Erro?.Message == RepositorioPagamentoQuebrado.Detalhe);
    }

    private sealed class RepositorioPagamentoQuebrado : IRepositorioPagamento
    {
        public const string Detalhe = "Falha simulada: conexão com srv-banco-interno:5432 recusada";

        public Pagamento? ObterPorId(int id) => throw new InvalidOperationException(Detalhe);

        public Pagamento? ObterPorChave(int compradorId, string chaveIdempotencia) => throw new InvalidOperationException(Detalhe);

        public List<Pagamento> ObterPendentesDoComprador(int compradorId) => throw new InvalidOperationException(Detalhe);

        public Pagamento Adicionar(Pagamento pagamento) => throw new InvalidOperationException(Detalhe);

        public void Atualizar(Pagamento pagamento) => throw new InvalidOperationException(Detalhe);
    }

    private sealed class LogEmMemoria : ILoggerProvider
    {
        private readonly ConcurrentBag<(LogLevel, string, Exception?)> destino;

        public LogEmMemoria(ConcurrentBag<(LogLevel, string, Exception?)> destino)
        {
            this.destino = destino;
        }

        public ILogger CreateLogger(string categoria) => new Registrador(destino);

        public void Dispose()
        {
        }

        private sealed class Registrador : ILogger
        {
            private readonly ConcurrentBag<(LogLevel, string, Exception?)> destino;

            public Registrador(ConcurrentBag<(LogLevel, string, Exception?)> destino)
            {
                this.destino = destino;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel nivel) => nivel >= LogLevel.Warning;

            public void Log<TState>(
                LogLevel nivel,
                EventId evento,
                TState estado,
                Exception? erro,
                Func<TState, Exception?, string> formatar)
            {
                if (IsEnabled(nivel))
                {
                    destino.Add((nivel, formatar(estado, erro), erro));
                }
            }
        }
    }
}
