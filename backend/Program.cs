using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Middleware;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Negociacao;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;

var builder = WebApplication.CreateBuilder(args);

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
        ?? "Data Source=rarityroom.db";

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

    string raiz = string.IsNullOrWhiteSpace(ambiente.WebRootPath)
        ? Path.Combine(ambiente.ContentRootPath, "wwwroot")
        : ambiente.WebRootPath;

    return new ServicoImagensArquivo(Path.Combine(raiz, "uploads"));
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
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

AplicarMigracoes(app);
SemearCategorias(app);
SemearAdministrador(app);

// Primeiro na pilha: qualquer exceção de domínio lançada adiante é traduzida
// em ProblemDetails antes de chegar ao cliente.
app.UseMiddleware<MiddlewareExcecoes>();

// Serve as imagens enviadas em wwwroot/uploads.
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

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
/// Cria a conta administrativa de demonstração quando ela não existe.
/// Em produção, a senha deve ser trocada e o e-mail definido por configuração.
/// </summary>
static void SemearAdministrador(WebApplication app)
{
    using var escopo = app.Services.CreateScope();

    var repositorio = escopo.ServiceProvider
        .GetRequiredService<IRepositorioUsuario>();

    var hash = escopo.ServiceProvider.GetRequiredService<IHashSenha>();

    const string email = "admin@rarityroom.com";

    var existente = repositorio.ObterPorEmail(email);

    if (existente == null)
    {
        repositorio.Adicionar(new Usuario(
            "Administrador",
            email,
            hash.CriarHash("admin123"),
            TipoConta.Ambos,
            administrador: true));

        return;
    }

    if (!existente.Administrador)
    {
        existente.TornarAdministrador();

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
