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

    private readonly IServicoAdmin servicoAdmin;

    public CategoriasController(
        IServicoCategorias servicoCategorias,
        IServicoAdmin servicoAdmin)
    {
        this.servicoCategorias = servicoCategorias;
        this.servicoAdmin = servicoAdmin;
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
        // Catálogo global: só administradores criam categorias (403 para os demais).
        var categoria = servicoAdmin.CriarCategoria(
            modelo.Nome,
            User.ObterUsuarioId());

        return StatusCode(
            StatusCodes.Status201Created,
            categoria);
    }
}
