using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/pedidos")]
[Authorize]
public class PedidosController : ControllerBase
{
    private readonly IServicoPedidos servicoPedidos;

    public PedidosController(IServicoPedidos servicoPedidos)
    {
        this.servicoPedidos = servicoPedidos;
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(servicoPedidos.Listar(User.ObterUsuarioId()));
    }

    [HttpGet("{id:int}")]
    public IActionResult ObterPorId(int id)
    {
        return Ok(servicoPedidos.ObterPorId(id, User.ObterUsuarioId()));
    }

    /// <summary>
    /// Checkout a partir do carrinho do usuário autenticado. Itens, preços e
    /// quantidades vêm do servidor; o corpo só traz entrega, forma de
    /// pagamento e a chave da tentativa. Cria a compra "aguardando pagamento"
    /// sem baixar estoque e sem limpar o carrinho.
    /// </summary>
    [HttpPost]
    public IActionResult Finalizar(ModeloCheckout modelo)
    {
        var entrega = DadosEntrega.Criar(
            modelo.Cep,
            modelo.Logradouro,
            modelo.Numero,
            modelo.Complemento,
            modelo.Bairro,
            modelo.Cidade,
            modelo.Estado);

        var compra = servicoPedidos.Finalizar(
            User.ObterUsuarioId(),
            entrega,
            FormaPagamentoDe(modelo.FormaPagamento),
            modelo.ChaveIdempotencia);

        return StatusCode(StatusCodes.Status201Created, compra);
    }

    [HttpPost("{id:int}/enviar")]
    public IActionResult Enviar(int id)
    {
        return Ok(servicoPedidos.Enviar(id, User.ObterUsuarioId()));
    }

    [HttpPost("{id:int}/entregar")]
    public IActionResult Entregar(int id)
    {
        return Ok(servicoPedidos.Entregar(id, User.ObterUsuarioId()));
    }

    /// <summary>
    /// Conclusão é ato do vendedor: é ela que transforma o anúncio em vendido.
    /// </summary>
    [HttpPost("{id:int}/concluir")]
    public IActionResult Concluir(int id)
    {
        return Ok(servicoPedidos.Concluir(id, User.ObterUsuarioId()));
    }

    [HttpPost("{id:int}/cancelar")]
    public IActionResult Cancelar(int id)
    {
        return Ok(servicoPedidos.Cancelar(id, User.ObterUsuarioId()));
    }

    private static FormaPagamento FormaPagamentoDe(string formaPagamento)
    {
        if (string.IsNullOrWhiteSpace(formaPagamento))
        {
            throw new ValidacaoException(
                "Informe a forma de pagamento: Pix, CartaoCredito ou Boleto.");
        }

        bool valida = Enum.TryParse(
            formaPagamento.Replace(" ", string.Empty),
            ignoreCase: true,
            out FormaPagamento resultado);

        if (!valida || !Enum.IsDefined(resultado))
        {
            throw new ValidacaoException(
                "Forma de pagamento inválida. Use Pix, CartaoCredito ou Boleto.");
        }

        return resultado;
    }
}
