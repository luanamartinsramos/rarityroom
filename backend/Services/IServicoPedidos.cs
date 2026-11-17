using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;

namespace RarityRoom.Servicos;

public interface IServicoPedidos
{
    List<Pedido> Listar(int usuarioId);

    Pedido ObterPorId(int id, int solicitanteId);

    Pedido Criar(
        int produtoId,
        DadosEntrega entrega,
        FormaPagamento formaPagamento,
        int compradorId);

    Pedido Pagar(int id, bool aprovado, int solicitanteId);

    Pedido Enviar(int id, int solicitanteId);

    Pedido Entregar(int id, int solicitanteId);

    Pedido Concluir(int id, int solicitanteId);

    Pedido Cancelar(int id, int solicitanteId);
}
