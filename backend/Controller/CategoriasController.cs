using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly IServicoCategorias servicoCategorias;

    public CategoriasController(IServicoCategorias servicoCategorias)
    {
        this.servicoCategorias = servicoCategorias;
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(servicoCategorias.Listar());
    }

    [HttpPost]
    [Authorize]
    public IActionResult Criar(ModeloCategoria modelo)
    {
        var categoria = servicoCategorias.Criar(modelo.Nome);

        return StatusCode(
            StatusCodes.Status201Created,
            categoria);
    }
}
