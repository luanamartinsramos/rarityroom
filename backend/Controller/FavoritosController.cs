using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/favoritos")]
[Authorize]
public class FavoritosController : ControllerBase
{
    private readonly IServicoFavoritos servicoFavoritos;

    public FavoritosController(IServicoFavoritos servicoFavoritos)
    {
        this.servicoFavoritos = servicoFavoritos;
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(servicoFavoritos.Listar(User.ObterUsuarioId()));
    }

    [HttpPost("{produtoId:int}")]
    public IActionResult Adicionar(int produtoId)
    {
        var favorito = servicoFavoritos.Adicionar(
            produtoId,
            User.ObterUsuarioId());

        return StatusCode(StatusCodes.Status201Created, favorito);
    }

    [HttpDelete("{produtoId:int}")]
    public IActionResult Remover(int produtoId)
    {
        servicoFavoritos.Remover(produtoId, User.ObterUsuarioId());

        return Ok(new
        {
            mensagem = "Anúncio removido dos favoritos."
        });
    }
}
