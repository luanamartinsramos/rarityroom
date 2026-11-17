using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

/// <summary>
/// Pagamento SIMULADO da compra. Não existe gateway, banco ou API financeira:
/// tudo acontece neste servidor e fica gravado no banco do RarityRoom.
/// Somente o comprador acessa a própria compra.
/// </summary>
[ApiController]
[Route("api/pagamentos")]
[Authorize]
public class PagamentosController : ControllerBase
{
    private readonly IServicoPedidos servicoPedidos;

    public PagamentosController(IServicoPedidos servicoPedidos)
    {
        this.servicoPedidos = servicoPedidos;
    }

    [HttpGet("{id:int}")]
    public IActionResult Obter(int id)
    {
        return Ok(servicoPedidos.ObterCompra(id, User.ObterUsuarioId()));
    }

    /// <summary>PIX ou boleto: simula a confirmação do pagamento.</summary>
    [HttpPost("{id:int}/simular-confirmacao")]
    public IActionResult SimularConfirmacao(int id)
    {
        return Ok(servicoPedidos.SimularConfirmacao(id, User.ObterUsuarioId()));
    }

    /// <summary>
    /// Cartão de crédito simulado. Os dados são validados e descartados; só
    /// os 4 últimos dígitos, a bandeira e as parcelas ficam gravados.
    /// </summary>
    [HttpPost("{id:int}/cartao")]
    public IActionResult PagarComCartao(int id, ModeloCartao cartao)
    {
        return Ok(servicoPedidos.PagarComCartao(id, cartao, User.ObterUsuarioId()));
    }

    /// <summary>Demonstração: vence o prazo do PIX/boleto imediatamente.</summary>
    [HttpPost("{id:int}/simular-expiracao")]
    public IActionResult SimularExpiracao(int id)
    {
        return Ok(servicoPedidos.SimularExpiracao(id, User.ObterUsuarioId()));
    }

    [HttpPost("{id:int}/cancelar")]
    public IActionResult Cancelar(int id)
    {
        return Ok(servicoPedidos.CancelarCompra(id, User.ObterUsuarioId()));
    }
}
