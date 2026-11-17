using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioPedido
{
    List<Pedido> ObterTodos();

    Pedido? ObterPorId(int id);

    List<Pedido> ObterPorComprador(int compradorId);

    List<Pedido> ObterPorVendedor(int vendedorId);

    List<Pedido> ObterPorPagamento(int pagamentoId);

    Pedido Adicionar(Pedido pedido);

    bool Atualizar(Pedido pedido);
}
