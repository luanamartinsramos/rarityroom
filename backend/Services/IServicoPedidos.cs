using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;

namespace RarityRoom.Servicos;

public interface IServicoPedidos
{
    List<Pedido> Listar(int usuarioId);

    Pedido ObterPorId(int id, int solicitanteId);

    CompraResumo ObterCompra(int pagamentoId, int solicitanteId);

    CompraResumo Finalizar(
        int compradorId,
        DadosEntrega entrega,
        FormaPagamento formaPagamento,
        string chaveIdempotencia);

    CompraResumo SimularConfirmacao(int pagamentoId, int solicitanteId);

    CompraResumo PagarComCartao(int pagamentoId, ModeloCartao cartao, int solicitanteId);

    CompraResumo SimularExpiracao(int pagamentoId, int solicitanteId);

    CompraResumo CancelarCompra(int pagamentoId, int solicitanteId);

    Pedido Enviar(int id, int solicitanteId);

    Pedido Entregar(int id, int solicitanteId);

    Pedido Concluir(int id, int solicitanteId);

    Pedido Cancelar(int id, int solicitanteId);
}
