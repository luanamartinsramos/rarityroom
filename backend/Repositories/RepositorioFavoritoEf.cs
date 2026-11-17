using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioFavoritoEf : IRepositorioFavorito
{
    private readonly RarityRoomContexto contexto;

    public RepositorioFavoritoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public List<Favorito> ObterPorUsuario(int usuarioId)
    {
        return contexto.Favoritos
            .AsNoTracking()
            .Where(favorito => favorito.UsuarioId == usuarioId)
            .OrderBy(favorito => favorito.Id)
            .ToList();
    }

    public Favorito? Obter(int usuarioId, int produtoId)
    {
        return contexto.Favoritos.FirstOrDefault(
            favorito => favorito.UsuarioId == usuarioId
                && favorito.ProdutoId == produtoId);
    }

    public Favorito Adicionar(Favorito favorito)
    {
        ArgumentNullException.ThrowIfNull(favorito);

        contexto.Favoritos.Add(favorito);
        contexto.SaveChanges();

        return favorito;
    }

    public bool Remover(int usuarioId, int produtoId)
    {
        var favorito = contexto.Favoritos.FirstOrDefault(
            item => item.UsuarioId == usuarioId
                && item.ProdutoId == produtoId);

        if (favorito == null)
        {
            return false;
        }

        contexto.Favoritos.Remove(favorito);
        contexto.SaveChanges();

        return true;
    }
}
