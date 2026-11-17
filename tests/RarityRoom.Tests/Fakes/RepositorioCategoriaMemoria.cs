using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

/// <summary>Dublê de teste do repositório de categorias.</summary>
public class RepositorioCategoriaMemoria : IRepositorioCategoria
{
    private readonly List<Categoria> categorias = new();

    private readonly object trava = new();

    private int ultimoId;

    public List<Categoria> ObterTodas()
    {
        lock (trava)
        {
            return new List<Categoria>(categorias);
        }
    }

    public Categoria? ObterPorId(int id)
    {
        lock (trava)
        {
            return categorias.FirstOrDefault(categoria => categoria.Id == id);
        }
    }

    public Categoria? ObterPorSlug(string slug)
    {
        string procurado = (slug ?? string.Empty).Trim();

        lock (trava)
        {
            return categorias.FirstOrDefault(
                categoria => categoria.Slug.Equals(
                    procurado,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    public Categoria? ObterPorNome(string nome)
    {
        string slug = Categoria.GerarSlug(nome);

        if (slug.Length == 0)
        {
            return null;
        }

        return ObterPorSlug(slug);
    }

    public Categoria Adicionar(Categoria categoria)
    {
        ArgumentNullException.ThrowIfNull(categoria);

        lock (trava)
        {
            categoria.DefinirId(++ultimoId);

            categorias.Add(categoria);

            return categoria;
        }
    }
}
