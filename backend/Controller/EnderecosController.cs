using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/enderecos")]
[Authorize]
public class EnderecosController : ControllerBase
{
    private readonly IServicoEnderecos servicoEnderecos;

    public EnderecosController(IServicoEnderecos servicoEnderecos)
    {
        this.servicoEnderecos = servicoEnderecos;
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(servicoEnderecos.Listar(User.ObterUsuarioId()));
    }

    [HttpPost]
    public IActionResult Criar(ModeloEndereco modelo)
    {
        var endereco = servicoEnderecos.Criar(
            User.ObterUsuarioId(),
            modelo.ParaEntrega());

        return StatusCode(StatusCodes.Status201Created, endereco);
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, ModeloEndereco modelo)
    {
        return Ok(servicoEnderecos.Atualizar(
            id,
            User.ObterUsuarioId(),
            modelo.ParaEntrega()));
    }

    [HttpDelete("{id:int}")]
    public IActionResult Remover(int id)
    {
        servicoEnderecos.Remover(id, User.ObterUsuarioId());

        return NoContent();
    }

    [HttpPost("{id:int}/principal")]
    public IActionResult DefinirPrincipal(int id)
    {
        return Ok(servicoEnderecos.DefinirPrincipal(
            id,
            User.ObterUsuarioId()));
    }
}
