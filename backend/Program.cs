using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Middleware;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Negociacao;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;

var builder = WebApplication.CreateBuilder(args);

// Pasta para dados que precisam sobreviver a reinícios (banco SQLite, imagens
// enviadas e chaves dos cookies de sessão). Em hospedagem, aponte para um
// disco persistente: Armazenamento__Pasta=/data. Vazio = comportamento local
// (banco na pasta do projeto, imagens em wwwroot/uploads).
string? pastaDados = builder.Configuration["Armazenamento:Pasta"];

if (!string.IsNullOrWhiteSpace(pastaDados))
{
    Directory.CreateDirectory(pastaDados);

    // Sem as chaves persistidas, todo reinício invalidaria os logins.
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(pastaDados, "chaves")));
}

// Atrás do proxy da hospedagem (Render etc.), o IP real do visitante e o
// esquema https chegam nos cabeçalhos X-Forwarded-*. Sem isto, o limite de
// tentativas por IP trataria todos os visitantes como um só.
builder.Services.Configure<ForwardedHeadersOptions>(opcoes =>
{
    opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // O proxy da plataforma não tem IP fixo conhecido. Com ForwardLimit = 1
    // (padrão) só o último salto, adicionado pelo próprio proxy, é usado:
    // um X-Forwarded-For forjado pelo cliente fica à esquerda e é ignorado.
    opcoes.KnownIPNetworks.Clear();
    opcoes.KnownProxies.Clear();
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enumerações trafegam como texto: a mudança de valor numérico não
        // quebra o contrato com o frontend.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
  options.AddPolicy("Frontend", policy =>
  {
    policy
          .WithOrigins("http://localhost:5173")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()
          .WithExposedHeaders("X-Total-Count");
  });
});

builder.Services.AddDbContext<RarityRoomContexto>(opcoes =>
{
    // Padrão local; pode ser sobrescrito por configuração ou pela variável
    // de ambiente ConnectionStrings__RarityRoom.
    string conexao = builder.Configuration.GetConnectionString("RarityRoom")
        ?? (string.IsNullOrWhiteSpace(pastaDados)
            ? "Data Source=rarityroom.db"
            : $"Data Source={Path.Combine(pastaDados, "rarityroom.db")}");

    opcoes.UseSqlite(conexao);
});

// Repositórios e serviços são scoped: acompanham o DbContext de cada
// requisição. Estratégias e hash não guardam estado e podem ser singleton.
builder.Services.AddScoped<IRepositorioUsuario, RepositorioUsuarioEf>();
builder.Services.AddScoped<IRepositorioProduto, RepositorioProdutoEf>();
builder.Services.AddScoped<IRepositorioCategoria, RepositorioCategoriaEf>();
builder.Services.AddScoped<IRepositorioProposta, RepositorioPropostaEf>();
builder.Services.AddScoped<IRepositorioFavorito, RepositorioFavoritoEf>();
builder.Services.AddScoped<IRepositorioPedido, RepositorioPedidoEf>();
builder.Services.AddScoped<IRepositorioAvaliacao, RepositorioAvaliacaoEf>();
builder.Services.AddScoped<IRepositorioNotificacao, RepositorioNotificacaoEf>();
builder.Services.AddScoped<IRepositorioLoja, RepositorioLojaEf>();
builder.Services.AddScoped<IRepositorioEndereco, RepositorioEnderecoEf>();
builder.Services.AddScoped<IRepositorioRecuperacaoSenha, RepositorioRecuperacaoSenhaEf>();
builder.Services.AddScoped<IRepositorioCarrinho, RepositorioCarrinhoEf>();
builder.Services.AddScoped<IRepositorioPagamento, RepositorioPagamentoEf>();
builder.Services.AddScoped<IEstatisticasCatalogo, EstatisticasCatalogoEf>();
builder.Services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoEf>();
builder.Services.AddScoped<RegrasCompra>();
builder.Services.AddScoped<IServicoCarrinho, ServicoCarrinho>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ProtecaoForcaBruta>();
builder.Services.AddSingleton<ICanalRecuperacaoSenha, CanalRecuperacaoSenhaLog>();

builder.Services.AddScoped<IServicoAutenticacao, ServicoAutenticacao>();
builder.Services.AddScoped<IServicoAnuncios, ServicoAnuncios>();
builder.Services.AddScoped<IServicoCategorias, ServicoCategorias>();
builder.Services.AddScoped<IServicoFavoritos, ServicoFavoritos>();
builder.Services.AddScoped<IServicoPropostas, ServicoPropostas>();
builder.Services.AddScoped<IServicoVendedores, ServicoVendedores>();
builder.Services.AddScoped<IServicoNotificacoes, ServicoNotificacoes>();
builder.Services.AddScoped<IServicoAdmin, ServicoAdmin>();
builder.Services.AddScoped<IServicoLojas, ServicoLojas>();
builder.Services.AddScoped<IServicoPerfil, ServicoPerfil>();
builder.Services.AddScoped<IServicoEnderecos, ServicoEnderecos>();
builder.Services.AddScoped<IServicoRecuperacaoSenha, ServicoRecuperacaoSenha>();
builder.Services.AddScoped<IServicoPedidos, ServicoPedidos>();
builder.Services.AddScoped<IServicoAvaliacoes, ServicoAvaliacoes>();

builder.Services.AddSingleton<IServicoImagens>(provedor =>
{
    var ambiente = provedor.GetRequiredService<IWebHostEnvironment>();

    return new ServicoImagensArquivo(PastaUploads(ambiente, pastaDados));
});

builder.Services.AddSingleton<IHashSenha, HashSenha>();

builder.Services.AddSingleton<IEstrategiaNegociacao, EstrategiaPrecoFixo>();
builder.Services.AddSingleton<IEstrategiaNegociacao, EstrategiaAceitaOferta>();
builder.Services.AddSingleton<IEstrategiaNegociacao, EstrategiaLiquidacao>();

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
      options.Cookie.Name = "RarityRoom.Auth";
      options.Cookie.HttpOnly = true;
      options.Cookie.SameSite = SameSiteMode.Lax;
      options.LoginPath = "/api/autenticacao/entrar";

      // Revalida a sessão a cada requisição: conta suspensa ou senha
      // redefinida derruba o cookie na hora; nome/e-mail alterados atualizam.
      options.Events.OnValidatePrincipal = SessaoUsuario.ValidarAsync;
    });

// Limite por IP nas rotas de autenticação (login, cadastro, recuperação).
// Configurável (Seguranca:LimiteAutenticacaoPorMinuto) para os testes, que
// fazem muitas chamadas do mesmo "IP". Lido na requisição, não aqui: a
// configuração final só existe depois do Build.
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opcoes.AddPolicy(PoliticasLimite.Autenticacao, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = contexto.RequestServices
                    .GetRequiredService<IConfiguration>()
                    .GetValue("Seguranca:LimiteAutenticacaoPorMinuto", 20),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    opcoes.OnRejected = async (contexto, cancelamento) =>
    {
        contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        await contexto.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            codigo = "muitas-tentativas",
            detail = "Muitas tentativas em pouco tempo. Aguarde um minuto e tente novamente."
        }, cancelamento);
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseForwardedHeaders();

AplicarMigracoes(app);
SemearCategorias(app);
SemearAdministrador(app);

// Primeiro na pilha: qualquer exceção de domínio lançada adiante é traduzida
// em ProblemDetails antes de chegar ao cliente.
app.UseMiddleware<MiddlewareExcecoes>();

// O navegador não deve "adivinhar" o tipo de um arquivo: um upload servido
// como imagem nunca pode ser interpretado como HTML/script.
app.Use(async (contexto, proximo) =>
{
    contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await proximo();
});

// Frontend compilado (wwwroot) e imagens enviadas.
app.UseDefaultFiles();
app.UseStaticFiles();

if (!string.IsNullOrWhiteSpace(pastaDados))
{
    string uploads = PastaUploads(app.Environment, pastaDados);

    Directory.CreateDirectory(uploads);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploads),
        RequestPath = "/uploads"
    });
}

// A documentação da API só fica exposta em desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

// Frontend (SPA): rotas como /home ou /produto/3 devolvem o index.html e o
// React Router decide a tela. "nonfile" deixa de fora caminhos com extensão
// (/assets/app.js), que são servidos como arquivo; /api e /uploads
// inexistentes continuam 404.
if (File.Exists(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html")))
{
    app.MapFallbackToFile("{*caminho:regex(^(?!api/|uploads/).*$):nonfile}", "index.html");
}

app.Run();

static string PastaUploads(IWebHostEnvironment ambiente, string? pastaDados)
{
    if (!string.IsNullOrWhiteSpace(pastaDados))
    {
        return Path.Combine(pastaDados, "uploads");
    }

    string raiz = string.IsNullOrWhiteSpace(ambiente.WebRootPath)
        ? Path.Combine(ambiente.ContentRootPath, "wwwroot")
        : ambiente.WebRootPath;

    return Path.Combine(raiz, "uploads");
}

/// <summary>
/// Cria ou atualiza o banco a partir das migrações versionadas em
/// <c>Dados/Migracoes</c>.
/// </summary>
static void AplicarMigracoes(WebApplication app)
{
    using var escopo = app.Services.CreateScope();

    var contexto = escopo.ServiceProvider
        .GetRequiredService<RarityRoomContexto>();

    contexto.Database.Migrate();
}

/// <summary>
/// Cria a conta administrativa. E-mail e senha vêm da configuração
/// (Admin:Email / Admin:Senha — por exemplo, a variável de ambiente
/// Admin__Senha), nunca do código.
///
/// Sem senha configurada: em desenvolvimento é gerada uma senha aleatória,
/// mostrada uma única vez no terminal; fora de desenvolvimento nenhum admin
/// é criado. Uma conta antiga que ainda use a senha pública "admin123" tem a
/// senha trocada na inicialização.
/// </summary>
static void SemearAdministrador(WebApplication app)
{
    using var escopo = app.Services.CreateScope();

    var repositorio = escopo.ServiceProvider
        .GetRequiredService<IRepositorioUsuario>();

    var hash = escopo.ServiceProvider.GetRequiredService<IHashSenha>();

    var logger = escopo.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Administrador");

    string email = app.Configuration["Admin:Email"] ?? "admin@rarityroom.com";
    string? senhaConfigurada = app.Configuration["Admin:Senha"];

    string? SenhaParaUsar()
    {
        if (!string.IsNullOrWhiteSpace(senhaConfigurada))
        {
            return senhaConfigurada;
        }

        if (!app.Environment.IsDevelopment())
        {
            return null;
        }

        // Prefixo garante letra e número, exigidos pela regra de senha.
        string gerada = "Adm1-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6));

        logger.LogWarning(
            "Conta de administrador: {Email} / senha gerada: {Senha} — anote; ela não será mostrada de novo. "
                + "Para fixar uma senha, defina a variável de ambiente Admin__Senha.",
            email,
            gerada);

        return gerada;
    }

    var existente = repositorio.ObterPorEmail(email);

    if (existente == null)
    {
        string? senha = SenhaParaUsar();

        if (senha == null)
        {
            logger.LogWarning(
                "Nenhum administrador criado: defina Admin:Senha (variável Admin__Senha) para criar {Email}.",
                email);

            return;
        }

        Usuario.ValidarSenha(senha);

        repositorio.Adicionar(new Usuario(
            "Administrador",
            email,
            hash.CriarHash(senha),
            TipoConta.Ambos,
            administrador: true));

        return;
    }

    bool alterou = false;

    if (!existente.Administrador)
    {
        existente.TornarAdministrador();
        alterou = true;
    }

    // Bancos criados por versões antigas guardam a senha pública "admin123".
    if (hash.VerificarSenha("admin123", existente.SenhaHash))
    {
        string? senha = SenhaParaUsar();

        if (senha != null)
        {
            existente.AlterarSenha(hash.CriarHash(senha));
            existente.Ativar();
        }
        else
        {
            existente.Desativar();

            logger.LogWarning(
                "O administrador {Email} usava a senha pública e foi desativado. Defina Admin:Senha para reativá-lo.",
                email);
        }

        alterou = true;
    }
    else if (!string.IsNullOrWhiteSpace(senhaConfigurada)
        && !hash.VerificarSenha(senhaConfigurada, existente.SenhaHash))
    {
        // A configuração é a fonte da verdade da senha do admin.
        existente.AlterarSenha(hash.CriarHash(senhaConfigurada));
        existente.Ativar();
        alterou = true;
    }

    if (alterou)
    {
        repositorio.Atualizar(existente);
    }
}

/// <summary>
/// Catálogo inicial de categorias. Sem ele, nenhum anúncio poderia ser criado
/// na primeira execução, porque a categoria é validada contra o catálogo.
/// </summary>
static void SemearCategorias(WebApplication app)
{
    using var escopo = app.Services.CreateScope();

    var servicoCategorias = escopo.ServiceProvider
        .GetRequiredService<IServicoCategorias>();

    string[] iniciais =
    {
        "Action Figures",
        "Funko Pop",
        "Mangás",
        "HQs e Quadrinhos",
        "Jogos e Consoles",
        "Cartas Colecionáveis",
        "Pokémon",
        "Yu-Gi-Oh!",
        "Magic: The Gathering",
        "Lego",
        "Colecionáveis Raros",
        "Filmes e Séries",
        "Animes",
        "Acessórios Geek",
        "Outros"
    };

    foreach (string nome in iniciais)
    {
        string slug = Categoria.GerarSlug(nome);

        bool existe = servicoCategorias
            .Listar()
            .Any(categoria => categoria.Slug == slug);

        if (!existe)
        {
            servicoCategorias.Criar(nome);
        }
    }
}

/// <summary>
/// Torna o tipo Program acessível aos testes de integração
/// (WebApplicationFactory).
/// </summary>
public partial class Program
{
}
