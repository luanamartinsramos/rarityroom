using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

/// <summary>
/// Persistência relacional dos anúncios. O identificador vem do banco
/// (autoincremento), nunca de contagem da coleção — a classe de defeito do
/// id duplicado não tem como reaparecer aqui.
/// </summary>
public class RepositorioProdutoEf : IRepositorioProduto
{
    private readonly RarityRoomContexto contexto;

    public RepositorioProdutoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public List<Produto> ObterTodos()
    {
        return contexto.Produtos
            .AsNoTracking()
            .OrderBy(produto => produto.Id)
            .ToList();
    }

    public Produto? ObterPorId(int id)
    {
        return contexto.Produtos.FirstOrDefault(produto => produto.Id == id);
    }

    public Produto Adicionar(Produto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        contexto.Produtos.Add(produto);
        contexto.SaveChanges();

        return produto;
    }

    public bool Atualizar(Produto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        bool existe = contexto.Produtos.Any(item => item.Id == produto.Id);

        if (!existe)
        {
            return false;
        }

        contexto.Produtos.Update(produto);
        contexto.SaveChanges();

        return true;
    }

    public bool Remover(int id)
    {
        var produto = contexto.Produtos.FirstOrDefault(item => item.Id == id);

        if (produto == null)
        {
            return false;
        }

        contexto.Produtos.Remove(produto);
        contexto.SaveChanges();

        return true;
    }
}
