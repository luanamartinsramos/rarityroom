using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioCarrinhoEf : IRepositorioCarrinho
{
    private readonly RarityRoomContexto contexto;

    public RepositorioCarrinhoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public List<ItemCarrinho> ObterPorUsuario(int usuarioId)
    {
        return contexto.ItensCarrinho
            .Where(item => item.UsuarioId == usuarioId)
            .OrderBy(item => item.AdicionadoEm)
            .ThenBy(item => item.Id)
            .ToList();
    }

    public ItemCarrinho? Obter(int usuarioId, int produtoId)
    {
        return contexto.ItensCarrinho.FirstOrDefault(
            item => item.UsuarioId == usuarioId && item.ProdutoId == produtoId);
    }

    public ItemCarrinho Adicionar(ItemCarrinho item)
    {
        contexto.ItensCarrinho.Add(item);
        contexto.SaveChanges();

        return item;
    }

    public void Atualizar(ItemCarrinho item)
    {
        contexto.ItensCarrinho.Update(item);
        contexto.SaveChanges();
    }

    public void Remover(ItemCarrinho item)
    {
        contexto.ItensCarrinho.Remove(item);
        contexto.SaveChanges();
    }

    public void RemoverProdutos(int usuarioId, IEnumerable<int> produtoIds)
    {
        var ids = produtoIds.ToHashSet();

        var itens = contexto.ItensCarrinho
            .Where(item => item.UsuarioId == usuarioId && ids.Contains(item.ProdutoId))
            .ToList();

        contexto.ItensCarrinho.RemoveRange(itens);
        contexto.SaveChanges();
    }
}
