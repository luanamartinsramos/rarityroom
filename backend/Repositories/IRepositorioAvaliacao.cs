using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioAvaliacao
{
    Avaliacao? ObterPorPedido(int pedidoId, int produtoId);

    List<Avaliacao> ObterPorVendedor(int vendedorId);

    List<Avaliacao> ObterPorProduto(int produtoId);

    Avaliacao Adicionar(Avaliacao avaliacao);
}
