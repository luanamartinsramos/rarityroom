using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Middleware;

/// <summary>
/// Traduz falhas de domínio em respostas ProblemDetails com status coerente e
/// código estável. Sem ele, cada controller repetiria try/catch e a regra de
/// negócio vazaria como erro 500.
/// </summary>
public class MiddlewareExcecoes
{
    private readonly RequestDelegate proximo;

    private readonly ILogger<MiddlewareExcecoes> logger;

    public MiddlewareExcecoes(
        RequestDelegate proximo,
        ILogger<MiddlewareExcecoes> logger)
    {
        this.proximo = proximo;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await proximo(contexto);
        }
        catch (ExcecaoDominio excecao)
        {
            logger.LogWarning(
                excecao,
                "Regra de negócio violada ({Codigo}) em {Metodo} {Caminho}",
                excecao.Codigo,
                contexto.Request.Method,
                contexto.Request.Path);

            await EscreverAsync(
                contexto,
                StatusDe(excecao),
                excecao.Codigo,
                excecao.Message);
        }
        catch (DbUpdateConcurrencyException excecao)
        {
            // Duas operações tentaram gravar o mesmo estoque ao mesmo tempo:
            // a segunda é rejeitada em vez de sobrescrever a primeira.
            logger.LogWarning(
                excecao,
                "Conflito de concorrência em {Metodo} {Caminho}",
                contexto.Request.Method,
                contexto.Request.Path);

            await EscreverAsync(
                contexto,
                StatusCodes.Status409Conflict,
                "concorrencia",
                "O estoque foi alterado por outra compra neste instante. Atualize a página e tente novamente.");
        }
        catch (Exception excecao)
        {
            logger.LogError(
                excecao,
                "Erro não tratado em {Metodo} {Caminho}",
                contexto.Request.Method,
                contexto.Request.Path);

            await EscreverAsync(
                contexto,
                StatusCodes.Status500InternalServerError,
                "erro-interno",
                "Ocorreu um erro inesperado. Tente novamente.");
        }
    }

    /// <summary>
    /// A ordem importa: <see cref="TransicaoInvalidaException"/> herda de
    /// <see cref="ConflitoException"/> e precisa ser avaliada antes.
    /// </summary>
    private static int StatusDe(ExcecaoDominio excecao)
    {
        return excecao switch
        {
            MuitasTentativasException => StatusCodes.Status429TooManyRequests,
            AcessoNegadoException => StatusCodes.Status403Forbidden,
            NaoEncontradoException => StatusCodes.Status404NotFound,
            TransicaoInvalidaException => StatusCodes.Status409Conflict,
            ConflitoException => StatusCodes.Status409Conflict,
            ValidacaoException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static async Task EscreverAsync(
        HttpContext contexto,
        int status,
        string codigo,
        string mensagem)
    {
        if (contexto.Response.HasStarted)
        {
            return;
        }

        contexto.Response.Clear();
        contexto.Response.StatusCode = status;

        var problema = new ProblemDetails
        {
            Status = status,
            Title = status >= 500
                ? "Erro interno"
                : "Não foi possível concluir a operação",
            Detail = mensagem,
            Instance = contexto.Request.Path
        };

        problema.Extensions["codigo"] = codigo;

        await contexto.Response.WriteAsJsonAsync(problema);
    }
}
