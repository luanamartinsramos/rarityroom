using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioProduto
{
    List<Produto> ObterTodos();

    Produto? ObterPorId(int id);

    Produto Adicionar(Produto produto);

    bool Atualizar(Produto produto);

    bool Remover(int id);
}