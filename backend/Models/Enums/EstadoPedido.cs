namespace RarityRoom.Modelos.Enums;

/// <summary>
/// Ciclo do pedido: nasce pendente, conclui na entrega confirmada ou é
/// cancelado enquanto ninguém recebeu o item.
/// </summary>
public enum EstadoPedido
{
    Pendente = 1,

    Concluido = 2,

    Pago = 3,

    Enviado = 4,

    Entregue = 5,

    Cancelado = 6
}
