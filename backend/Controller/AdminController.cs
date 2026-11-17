using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

/// <summary>
/// Área administrativa. A autenticação é exigida aqui e o papel de
/// administrador é verificado em cada operação do serviço.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IServicoAdmin servicoAdmin;

    public AdminController(IServicoAdmin servicoAdmin)
    {
        this.servicoAdmin = servicoAdmin;
    }

    [HttpGet("resumo")]
    public IActionResult Resumo()
    {
        return Ok(servicoAdmin.Resumo(User.ObterUsuarioId()));
    }

    [HttpGet("usuarios")]
    public IActionResult Usuarios()
    {
        return Ok(servicoAdmin.ListarUsuarios(User.ObterUsuarioId()));
    }

    [HttpPost("usuarios/{id:int}/suspender")]
    public IActionResult Suspender(int id)
    {
        servicoAdmin.SuspenderUsuario(id, User.ObterUsuarioId());

        return Ok(new { mensagem = "Conta suspensa." });
    }

    [HttpPost("usuarios/{id:int}/reativar")]
    public IActionResult Reativar(int id)
    {
        servicoAdmin.ReativarUsuario(id, User.ObterUsuarioId());

        return Ok(new { mensagem = "Conta reativada." });
    }

    [HttpGet("anuncios")]
    public IActionResult Anuncios()
    {
        return Ok(servicoAdmin.ListarAnuncios(User.ObterUsuarioId()));
    }

    [HttpPost("anuncios/{id:int}/moderar")]
    public IActionResult Moderar(int id, [FromQuery] string acao)
    {
        return Ok(servicoAdmin.ModerarAnuncio(
            id,
            acao,
            User.ObterUsuarioId()));
    }

    [HttpGet("pedidos")]
    public IActionResult Pedidos()
    {
        return Ok(servicoAdmin.ListarPedidos(User.ObterUsuarioId()));
    }
}
