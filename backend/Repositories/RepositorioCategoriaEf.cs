using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioCategoriaEf : IRepositorioCategoria
{
    private readonly RarityRoomContexto contexto;

    public RepositorioCategoriaEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public List<Categoria> ObterTodas()
    {
        return contexto.Categorias
            .AsNoTracking()
            .OrderBy(categoria => categoria.Nome)
            .ToList();
    }

    public Categoria? ObterPorId(int id)
    {
        return contexto.Categorias.FirstOrDefault(categoria => categoria.Id == id);
    }

    public Categoria? ObterPorSlug(string slug)
    {
        string procurado = (slug ?? string.Empty).Trim().ToLowerInvariant();

        if (procurado.Length == 0)
        {
            return null;
        }

        return contexto.Categorias.FirstOrDefault(
            categoria => categoria.Slug.ToLower() == procurado);
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

        contexto.Categorias.Add(categoria);
        contexto.SaveChanges();

        return categoria;
    }
}
