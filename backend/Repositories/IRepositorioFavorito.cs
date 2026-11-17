using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioFavorito
{
    List<Favorito> ObterPorUsuario(int usuarioId);

    Favorito? Obter(int usuarioId, int produtoId);

    Favorito Adicionar(Favorito favorito);

    bool Remover(int usuarioId, int produtoId);
}
