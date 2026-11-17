namespace RarityRoom.Modelos.Enums;

/// <summary>
/// Operações que solicitam uma mudança de estado. A operação é a chave da
/// transição: destinos iguais alcançados por operações diferentes são
/// distinguidos (ex.: Reativar a partir de Pausado não é o mesmo que
/// CancelarReserva a partir de Reservado).
/// </summary>
public enum OperacaoAnuncio
{
    Reservar = 1,

    Vender = 2,

    Pausar = 3,

    Reativar = 4,

    CancelarReserva = 5
}
