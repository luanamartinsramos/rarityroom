using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Api;

/// <summary>
/// Regressões das falhas encontradas no teste de segurança do backend. Cada
/// teste reproduz o ataque original e confirma que ele não funciona mais.
/// </summary>
public class SegurancaApiTests
{
    private const string Senha = "senha123";

    private static HttpClient Cliente(FabricaApi fabrica)
    {
        return fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static string EmailUnico(string prefixo)
    {
        return $"{prefixo}.{Guid.NewGuid():N}@teste.com";
    }

    private static async Task<int> CadastrarELogar(HttpClient cliente, string email)
    {
        var cadastro = await cliente.PostAsJsonAsync(
            "/api/autenticacao/cadastrar",
            new { nome = "Ana Lima", email, senha = Senha });

        Assert.Equal(HttpStatusCode.OK, cadastro.StatusCode);

        var login = await Entrar(cliente, email, Senha);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var corpo = await cadastro.Content.ReadFromJsonAsync<JsonElement>();

        return corpo.GetProperty("usuario").GetProperty("id").GetInt32();
    }

    private static Task<HttpResponseMessage> Entrar(HttpClient cliente, string email, string senha)
    {
        return cliente.PostAsJsonAsync("/api/autenticacao/entrar", new { email, senha });
    }

    private static async Task<int> CriarAnuncio(HttpClient cliente)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Funko Batman",
            descricao = "Item de coleção",
            preco = 99.90m,
            estoque = 2,
            categoria = "Funko Pop",
            condicao = "Novo"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return (await resposta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();
    }

    // ------------------------------------------------------------------
    // Recuperação de senha
    // ------------------------------------------------------------------

    [Fact]
    public async Task RecuperarSenha_NaoDevolveOTokenNaResposta()
    {
        using var fabrica = new FabricaApi();
        using var vitima = Cliente(fabrica);
        using var atacante = Cliente(fabrica);

        string email = EmailUnico("vitima");
        await CadastrarELogar(vitima, email);

        var resposta = await atacante.PostAsJsonAsync(
            "/api/autenticacao/recuperar-senha", new { email });

        string corpo = await resposta.Content.ReadAsStringAsync();
        string? token = fabrica.CaixaDeEmail.UltimoToken(email);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotNull(token);
        Assert.DoesNotContain(token!, corpo);
        Assert.DoesNotContain("token", corpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecuperarSenha_RespondeIgualParaEmailExistenteEInexistente()
    {
        using var fabrica = new FabricaApi();
        using var cliente = Cliente(fabrica);

        string email = EmailUnico("existe");
        await CadastrarELogar(cliente, email);

        var existente = await cliente.PostAsJsonAsync(
            "/api/autenticacao/recuperar-senha", new { email });
        var inexistente = await cliente.PostAsJsonAsync(
            "/api/autenticacao/recuperar-senha", new { email = EmailUnico("ninguem") });

        Assert.Equal(existente.StatusCode, inexistente.StatusCode);
        Assert.Equal(
            await existente.Content.ReadAsStringAsync(),
            await inexistente.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RedefinirSenha_DerrubaSessoesAbertas()
    {
        using var fabrica = new FabricaApi();
        using var sessaoAntiga = Cliente(fabrica);
        using var anonimo = Cliente(fabrica);

        string email = EmailUnico("sessao");
        await CadastrarELogar(sessaoAntiga, email);

        Assert.Equal(HttpStatusCode.OK, (await sessaoAntiga.GetAsync("/api/carrinho")).StatusCode);

        await anonimo.PostAsJsonAsync("/api/autenticacao/recuperar-senha", new { email });

        var redefinicao = await anonimo.PostAsJsonAsync("/api/autenticacao/redefinir-senha", new
        {
            email,
            token = fabrica.CaixaDeEmail.UltimoToken(email),
            novaSenha = "outraSenha789"
        });

        Assert.Equal(HttpStatusCode.OK, redefinicao.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await sessaoAntiga.GetAsync("/api/carrinho")).StatusCode);
    }

    // ------------------------------------------------------------------
    // Sessão e conta
    // ------------------------------------------------------------------

    [Fact]
    public async Task ContaSuspensa_PerdeASessaoNaHora()
    {
        using var fabrica = new FabricaApi();
        using var admin = Cliente(fabrica);
        using var alvo = Cliente(fabrica);

        int alvoId = await CadastrarELogar(alvo, EmailUnico("suspensa"));

        await Entrar(admin, "admin@rarityroom.com", FabricaApi.SenhaAdmin);

        var suspensao = await admin.PostAsync($"/api/admin/usuarios/{alvoId}/suspender", null);

        Assert.Equal(HttpStatusCode.OK, suspensao.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await alvo.GetAsync("/api/carrinho")).StatusCode);

        var anuncio = await alvo.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Depois da suspensão",
            preco = 10m,
            estoque = 1,
            categoria = "Funko Pop"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, anuncio.StatusCode);
    }

    [Fact]
    public async Task AdminComSenhaPublicaAntiga_NaoEntra()
    {
        using var fabrica = new FabricaApi();
        using var cliente = Cliente(fabrica);

        var antiga = await Entrar(cliente, "admin@rarityroom.com", "admin123");
        var configurada = await Entrar(cliente, "admin@rarityroom.com", FabricaApi.SenhaAdmin);

        Assert.Equal(HttpStatusCode.Unauthorized, antiga.StatusCode);
        Assert.Equal(HttpStatusCode.OK, configurada.StatusCode);
    }

    [Fact]
    public async Task TrocarEmail_ExigeSenhaAtualEAtualizaASessao()
    {
        using var fabrica = new FabricaApi();
        using var cliente = Cliente(fabrica);

        await CadastrarELogar(cliente, EmailUnico("troca"));

        string novo = EmailUnico("novo");

        var semSenha = await cliente.PutAsJsonAsync("/api/perfil/email", new { email = novo });
        var senhaErrada = await cliente.PutAsJsonAsync(
            "/api/perfil/email", new { email = novo, senhaAtual = "errada999" });
        var correta = await cliente.PutAsJsonAsync(
            "/api/perfil/email", new { email = novo, senhaAtual = Senha });

        Assert.Equal(HttpStatusCode.BadRequest, semSenha.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, senhaErrada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, correta.StatusCode);

        // A sessão passa a refletir o e-mail novo (antes ficava o antigo no cookie).
        var atual = await cliente.GetFromJsonAsync<JsonElement>("/api/autenticacao/usuario-atual");

        Assert.Equal(novo, atual.GetProperty("email").GetString());
    }

    // ------------------------------------------------------------------
    // Força bruta
    // ------------------------------------------------------------------

    [Fact]
    public async Task Login_BloqueiaAContaAposCincoSenhasErradas()
    {
        using var fabrica = new FabricaApi();
        using var dono = Cliente(fabrica);
        using var atacante = Cliente(fabrica);

        string email = EmailUnico("alvo");
        await CadastrarELogar(dono, email);

        for (int i = 0; i < ProtecaoForcaBruta.TentativasPermitidas; i++)
        {
            var erro = await Entrar(atacante, email, $"chute{i}");
            Assert.Equal(HttpStatusCode.Unauthorized, erro.StatusCode);
        }

        // Bloqueada: nem a senha certa entra até o bloqueio expirar.
        var bloqueada = await Entrar(atacante, email, Senha);

        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
    }

    [Fact]
    public void ProtecaoForcaBruta_LiberaDepoisDoBloqueio()
    {
        var relogio = new RelogioFixo(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
        var protecao = new ProtecaoForcaBruta(relogio);

        for (int i = 0; i < ProtecaoForcaBruta.TentativasPermitidas; i++)
        {
            protecao.RegistrarFalha("ana@teste.com");
        }

        Assert.Throws<MuitasTentativasException>(() => protecao.GarantirLiberado("ANA@teste.com"));

        relogio.Avancar(ProtecaoForcaBruta.Bloqueio + TimeSpan.FromSeconds(1));

        protecao.GarantirLiberado("ana@teste.com");
    }

    [Fact]
    public async Task Login_TemLimitePorIp()
    {
        using var fabrica = new FabricaApi { LimiteAutenticacaoPorMinuto = 3 };
        using var cliente = Cliente(fabrica);

        var respostas = new List<HttpStatusCode>();

        for (int i = 0; i < 4; i++)
        {
            respostas.Add((await Entrar(cliente, EmailUnico($"spray{i}"), "qualquer1")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, respostas[^1]);
    }

    // ------------------------------------------------------------------
    // Categorias, estoque e upload
    // ------------------------------------------------------------------

    [Fact]
    public async Task Categoria_SoAdministradorCria()
    {
        using var fabrica = new FabricaApi();
        using var comum = Cliente(fabrica);
        using var admin = Cliente(fabrica);

        await CadastrarELogar(comum, EmailUnico("comum"));
        await Entrar(admin, "admin@rarityroom.com", FabricaApi.SenhaAdmin);

        var negada = await comum.PostAsJsonAsync("/api/categorias", new { nome = "Spam" });
        var criada = await admin.PostAsJsonAsync("/api/categorias", new { nome = "Retrô" });

        Assert.Equal(HttpStatusCode.Forbidden, negada.StatusCode);
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
    }

    [Fact]
    public async Task Anuncio_ComEstoqueAcimaDoTeto_EhRecusado()
    {
        using var fabrica = new FabricaApi();
        using var cliente = Cliente(fabrica);

        await CadastrarELogar(cliente, EmailUnico("teto"));

        var resposta = await cliente.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Estoque gigante",
            preco = 10m,
            estoque = int.MaxValue,
            categoria = "Funko Pop"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public void ReporEstoque_NoTeto_NaoEstouraNemFicaNegativo()
    {
        var produto = Apoio.Produto(estoque: Produto.EstoqueMaximo);

        produto.ReporEstoque(int.MaxValue);

        Assert.Equal(Produto.EstoqueMaximo, produto.Estoque);
    }

    [Fact]
    public async Task Upload_ComConteudoQueNaoEhImagem_EhRecusado()
    {
        using var fabrica = new FabricaApi();
        using var cliente = Cliente(fabrica);

        await CadastrarELogar(cliente, EmailUnico("html"));
        int produtoId = await CriarAnuncio(cliente);

        using var formulario = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent("<html><script>alert(1)</script></html>"u8.ToArray());
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formulario.Add(arquivo, "arquivo", "foto.png");

        var resposta = await cliente.PostAsync($"/api/produtos/{produtoId}/imagem", formulario);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.False(Directory.Exists(fabrica.PastaImagens)
            && Directory.EnumerateFiles(fabrica.PastaImagens).Any());
    }

    [Fact]
    public async Task Upload_EmAnuncioAlheio_NaoGravaArquivo()
    {
        using var fabrica = new FabricaApi();
        using var dono = Cliente(fabrica);
        using var intruso = Cliente(fabrica);

        await CadastrarELogar(dono, EmailUnico("dono"));
        await CadastrarELogar(intruso, EmailUnico("intruso"));
        int produtoId = await CriarAnuncio(dono);

        byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 };

        using var formulario = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(png);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formulario.Add(arquivo, "arquivo", "foto.png");

        var resposta = await intruso.PostAsync($"/api/produtos/{produtoId}/imagem", formulario);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.False(Directory.Exists(fabrica.PastaImagens)
            && Directory.EnumerateFiles(fabrica.PastaImagens).Any());
    }

    [Fact]
    public async Task Respostas_TemNosniff()
    {
        using var fabrica = new FabricaApi();
        using var cliente = Cliente(fabrica);

        var resposta = await cliente.GetAsync("/api/categorias");

        Assert.Equal("nosniff", resposta.Headers.GetValues("X-Content-Type-Options").Single());
    }

    private sealed class RelogioFixo : TimeProvider
    {
        private DateTimeOffset agora;

        public RelogioFixo(DateTimeOffset inicio)
        {
            agora = inicio;
        }

        public void Avancar(TimeSpan intervalo)
        {
            agora = agora.Add(intervalo);
        }

        public override DateTimeOffset GetUtcNow()
        {
            return agora;
        }
    }
}
