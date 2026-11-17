using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioCategoria
{
    List<Categoria> ObterTodas();

    Categoria? ObterPorId(int id);

    Categoria? ObterPorSlug(string slug);

    Categoria? ObterPorNome(string nome);

    Categoria Adicionar(Categoria categoria);
}
