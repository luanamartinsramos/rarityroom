namespace RarityRoom.Modelos.Enums;

/// <summary>
/// Operações que mudam o estado do pedido. Como no anúncio, a transição é
/// identificada pelo par (estado de origem, operação).
/// </summary>
public enum OperacaoPedido
{
    Pagar = 1,

    Enviar = 2,

    Entregar = 3,

    Concluir = 4,

    Cancelar = 5
}
