using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/propostas")]
[Authorize]
public class PropostasController : ControllerBase
{
    private readonly IServicoPropostas servicoPropostas;

    public PropostasController(IServicoPropostas servicoPropostas)
    {
        this.servicoPropostas = servicoPropostas;
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(servicoPropostas.Listar(User.ObterUsuarioId()));
    }

    [HttpPost]
    public IActionResult Criar(ModeloProposta modelo)
    {
        var proposta = servicoPropostas.Criar(
            modelo.ProdutoId,
            modelo.Valor,
            modelo.Mensagem,
            User.ObterUsuarioId());

        return StatusCode(StatusCodes.Status201Created, proposta);
    }

    [HttpPost("{id:int}/aceitar")]
    public IActionResult Aceitar(int id)
    {
        return Ok(servicoPropostas.Aceitar(id, User.ObterUsuarioId()));
    }

    [HttpPost("{id:int}/recusar")]
    public IActionResult Recusar(int id)
    {
        return Ok(servicoPropostas.Recusar(id, User.ObterUsuarioId()));
    }

    [HttpPost("{id:int}/cancelar")]
    public IActionResult Cancelar(int id)
    {
        return Ok(servicoPropostas.Cancelar(id, User.ObterUsuarioId()));
    }
}
