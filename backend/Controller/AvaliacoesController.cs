using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/avaliacoes")]
public class AvaliacoesController : ControllerBase
{
    private readonly IServicoAvaliacoes servicoAvaliacoes;

    public AvaliacoesController(IServicoAvaliacoes servicoAvaliacoes)
    {
        this.servicoAvaliacoes = servicoAvaliacoes;
    }

    [HttpPost]
    [Authorize]
    public IActionResult Avaliar(ModeloAvaliacao modelo)
    {
        var avaliacao = servicoAvaliacoes.Avaliar(
            modelo.PedidoId,
            User.ObterUsuarioId(),
            modelo.Nota,
            modelo.Comentario);

        return StatusCode(StatusCodes.Status201Created, avaliacao);
    }

    /// <summary>
    /// Reputação é informação pública de vitrine: qualquer visitante consulta.
    /// </summary>
    /// <summary>
    /// Nota média do anúncio. Público: qualquer visitante vê a reputação do
    /// item antes de comprar.
    /// </summary>
    [HttpGet("produto/{produtoId:int}")]
    [AllowAnonymous]
    public IActionResult ResumoDoProduto(int produtoId)
    {
        return Ok(servicoAvaliacoes.ObterResumoProduto(produtoId));
    }

    [HttpGet("vendedor/{vendedorId:int}")]
    [AllowAnonymous]
    public IActionResult Reputacao(int vendedorId)
    {
        var reputacao = servicoAvaliacoes.ObterReputacao(vendedorId);

        var avaliacoes = servicoAvaliacoes.ListarDoVendedor(vendedorId);

        return Ok(new
        {
            vendedorId,
            reputacao.Media,
            reputacao.Total,
            avaliacoes
        });
    }
}
