using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoFavoritos
{
    List<Produto> Listar(int usuarioId);

    Favorito Adicionar(int produtoId, int usuarioId);

    void Remover(int produtoId, int usuarioId);
}
