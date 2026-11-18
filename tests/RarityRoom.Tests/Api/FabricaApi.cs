using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RarityRoom.Servicos;

namespace RarityRoom.Tests.Api;

/// <summary>
/// Sobe a aplicação HTTP real (pipeline completo, controllers, middleware) em
/// memória processual. Só duas coisas são trocadas: o banco (arquivo SQLite
/// temporário, para não tocar o banco de desenvolvimento) e o diretório das
/// imagens enviadas.
/// </summary>
public class FabricaApi : WebApplicationFactory<Program>
{
    /// <summary>Senha do administrador semeado nos testes (via configuração).</summary>
    public const string SenhaAdmin = "Admin@Teste123";

    /// <summary>Recebe os "e-mails" de recuperação de senha enviados pela API.</summary>
    public CanalRecuperacaoMemoria CaixaDeEmail { get; } = new();

    /// <summary>
    /// Limite de autenticação por IP. Todos os clientes de teste compartilham o
    /// mesmo "IP", então o padrão é alto; os testes do limite o reduzem.
    /// </summary>
    public int LimiteAutenticacaoPorMinuto { get; init; } = 100_000;

    private readonly string caminhoBanco = Path.Combine(
        Path.GetTempPath(),
        $"rarityroom-teste-{Guid.NewGuid():N}.db");

    public string PastaImagens { get; } = Path.Combine(
        Path.GetTempPath(),
        $"rarityroom-imagens-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuracao) =>
        {
            configuracao.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RarityRoom"] = $"Data Source={caminhoBanco}",
                    ["Admin:Senha"] = SenhaAdmin,
                    ["Seguranca:LimiteAutenticacaoPorMinuto"] = LimiteAutenticacaoPorMinuto.ToString()
                });
        });

        builder.ConfigureServices(servicos =>
        {
            servicos.RemoveAll<IServicoImagens>();
            servicos.AddSingleton<IServicoImagens>(
                new ServicoImagensArquivo(PastaImagens));

            servicos.RemoveAll<ICanalRecuperacaoSenha>();
            servicos.AddSingleton<ICanalRecuperacaoSenha>(CaixaDeEmail);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // O pool de conexões do SQLite mantém o arquivo aberto; limpar o pool
        // é o que permite apagá-lo.
        SqliteConnection.ClearAllPools();

        try
        {
            if (File.Exists(caminhoBanco))
            {
                File.Delete(caminhoBanco);
            }

            if (Directory.Exists(PastaImagens))
            {
                Directory.Delete(PastaImagens, recursive: true);
            }
        }
        catch (IOException)
        {
            // Limpeza best-effort de arquivo temporário: se o sistema ainda
            // estiver liberando o handle, o resíduo em %TEMP% é inofensivo.
        }
    }
}
