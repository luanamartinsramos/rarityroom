using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

/// <summary>Dublê de teste do repositório de favoritos.</summary>
public class RepositorioFavoritoMemoria : IRepositorioFavorito
{
    private readonly List<Favorito> favoritos = new();

    private readonly object trava = new();

    private int ultimoId;

    public List<Favorito> ObterPorUsuario(int usuarioId)
    {
        lock (trava)
        {
            return favoritos
                .Where(favorito => favorito.UsuarioId == usuarioId)
                .ToList();
        }
    }

    public Favorito? Obter(int usuarioId, int produtoId)
    {
        lock (trava)
        {
            return favoritos.FirstOrDefault(
                favorito => favorito.UsuarioId == usuarioId
                    && favorito.ProdutoId == produtoId);
        }
    }

    public Favorito Adicionar(Favorito favorito)
    {
        ArgumentNullException.ThrowIfNull(favorito);

        lock (trava)
        {
            favorito.DefinirId(++ultimoId);

            favoritos.Add(favorito);

            return favorito;
        }
    }

    public bool Remover(int usuarioId, int produtoId)
    {
        lock (trava)
        {
            int indice = favoritos.FindIndex(
                favorito => favorito.UsuarioId == usuarioId
                    && favorito.ProdutoId == produtoId);

            if (indice < 0)
            {
                return false;
            }

            favoritos.RemoveAt(indice);

            return true;
        }
    }
}
