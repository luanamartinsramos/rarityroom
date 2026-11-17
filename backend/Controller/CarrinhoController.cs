using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

/// <summary>
/// Carrinho do usuário autenticado. Toda resposta devolve o carrinho inteiro
/// recalculado no servidor (preços, subtotais, total e avisos de estoque).
/// </summary>
[ApiController]
[Route("api/carrinho")]
[Authorize]
public class CarrinhoController : ControllerBase
{
    private readonly IServicoCarrinho servicoCarrinho;

    public CarrinhoController(IServicoCarrinho servicoCarrinho)
    {
        this.servicoCarrinho = servicoCarrinho;
    }

    [HttpGet]
    public IActionResult Obter()
    {
        return Ok(servicoCarrinho.Obter(User.ObterUsuarioId()));
    }

    /// <summary>Adiciona (ou soma) unidades de um anúncio.</summary>
    [HttpPost("itens")]
    public IActionResult Adicionar(ModeloItemCarrinho modelo)
    {
        return Ok(servicoCarrinho.Adicionar(
            User.ObterUsuarioId(),
            modelo.ProdutoId,
            modelo.Quantidade));
    }

    /// <summary>Define a quantidade exata de um item.</summary>
    [HttpPut("itens/{produtoId:int}")]
    public IActionResult AlterarQuantidade(int produtoId, ModeloQuantidade modelo)
    {
        return Ok(servicoCarrinho.AlterarQuantidade(
            User.ObterUsuarioId(),
            produtoId,
            modelo.Quantidade));
    }

    [HttpDelete("itens/{produtoId:int}")]
    public IActionResult Remover(int produtoId)
    {
        return Ok(servicoCarrinho.Remover(User.ObterUsuarioId(), produtoId));
    }

    [HttpDelete]
    public IActionResult Limpar()
    {
        return Ok(servicoCarrinho.Limpar(User.ObterUsuarioId()));
    }
}
