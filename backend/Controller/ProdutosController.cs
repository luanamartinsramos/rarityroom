using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Repositorios;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly IRepositorioProduto repositorioProduto;

    public ProdutosController(
        IRepositorioProduto repositorioProduto)
    {
        this.repositorioProduto = repositorioProduto;
    }

    [HttpGet]
    public IActionResult ObterTodos()
    {
        var produtos = repositorioProduto.ObterTodos();

        return Ok(produtos);
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        var produto = repositorioProduto.ObterPorId(id);

        if (produto == null)
        {
            return NotFound(new
            {
                mensagem = "Produto não encontrado."
            });
        }

        return Ok(produto);
    }

    [HttpPost]
    public IActionResult Criar(
        ModeloProduto modelo)
    {
        int id = repositorioProduto
            .ObterTodos()
            .Count + 1;

        var produto = new Produto(
            id,
            modelo.Nome,
            modelo.Descricao,
            modelo.Preco,
            modelo.Estoque,
            modelo.Categoria,
            modelo.Marca,
            modelo.Colecao,
            modelo.Condicao,
            modelo.ImagemUrl
        );

        repositorioProduto.Adicionar(produto);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = produto.Id },
            produto
        );
    }

    [HttpPut("{id}")]
    public IActionResult Atualizar(
        int id,
        ModeloProduto modelo)
    {
        var produto = repositorioProduto.ObterPorId(id);

        if (produto == null)
        {
            return NotFound(new
            {
                mensagem = "Produto não encontrado."
            });
        }

        produto.Atualizar(
            modelo.Nome,
            modelo.Descricao,
            modelo.Preco,
            modelo.Estoque,
            modelo.Categoria,
            modelo.Marca,
            modelo.Colecao,
            modelo.Condicao,
            modelo.ImagemUrl
        );

        repositorioProduto.Atualizar(produto);

        return Ok(produto);
    }

    [HttpDelete("{id}")]
    public IActionResult Remover(int id)
    {
        bool removido = repositorioProduto.Remover(id);

        if (!removido)
        {
            return NotFound(new
            {
                mensagem = "Produto não encontrado."
            });
        }

        return Ok(new
        {
            mensagem = "Produto removido com sucesso."
        });
    }
}