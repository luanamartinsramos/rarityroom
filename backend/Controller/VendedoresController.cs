using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/vendedores")]
public class VendedoresController : ControllerBase
{
    private readonly IServicoVendedores servicoVendedores;

    public VendedoresController(IServicoVendedores servicoVendedores)
    {
        this.servicoVendedores = servicoVendedores;
    }

    /// <summary>
    /// Perfil público: qualquer visitante consulta vendedor, reputação e
    /// catálogo ativo.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public IActionResult Obter(int id)
    {
        return Ok(servicoVendedores.Obter(id));
    }
}
