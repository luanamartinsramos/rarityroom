using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioProdutoMemoria : IRepositorioProduto
{
    private readonly List<Produto> produtos = new();

    private int proximoId = 1;

    public List<Produto> ObterTodos()
    {
        return produtos;
    }

    public Produto? ObterPorId(int id)
    {
        return produtos.FirstOrDefault(produto => produto.Id == id);
    }

    public Produto Adicionar(Produto produto)
    {
        produtos.Add(produto);

        proximoId++;

        return produto;
    }

    public bool Atualizar(Produto produto)
    {
        var produtoExistente = ObterPorId(produto.Id);

        if (produtoExistente == null)
        {
            return false;
        }

        produtoExistente.Atualizar(
            produto.Nome,
            produto.Descricao,
            produto.Preco,
            produto.Estoque,
            produto.Categoria,
            produto.Marca,
            produto.Colecao,
            produto.Condicao,
            produto.ImagemUrl
        );

        return true;
    }

    public bool Remover(int id)
    {
        var produto = ObterPorId(id);

        if (produto == null)
        {
            return false;
        }

        produtos.Remove(produto);

        return true;
    }

    public int ObterProximoId()
    {
        return proximoId;
    }
}