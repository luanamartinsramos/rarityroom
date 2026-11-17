using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/notificacoes")]
[Authorize]
public class NotificacoesController : ControllerBase
{
    private readonly IServicoNotificacoes servicoNotificacoes;

    public NotificacoesController(IServicoNotificacoes servicoNotificacoes)
    {
        this.servicoNotificacoes = servicoNotificacoes;
    }

    [HttpGet]
    public IActionResult Listar()
    {
        int usuarioId = User.ObterUsuarioId();

        return Ok(new
        {
            naoLidas = servicoNotificacoes.ContarNaoLidas(usuarioId),
            itens = servicoNotificacoes.Listar(usuarioId)
        });
    }

    [HttpPost("{id:int}/ler")]
    public IActionResult MarcarComoLida(int id)
    {
        return Ok(servicoNotificacoes.MarcarComoLida(id, User.ObterUsuarioId()));
    }

    [HttpPost("ler-todas")]
    public IActionResult MarcarTodasComoLidas()
    {
        servicoNotificacoes.MarcarTodasComoLidas(User.ObterUsuarioId());

        return Ok(new { mensagem = "Notificações marcadas como lidas." });
    }
}
