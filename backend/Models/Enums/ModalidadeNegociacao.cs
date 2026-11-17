namespace RarityRoom.Modelos.Enums;

/// <summary>
/// Modalidade escolhida pelo vendedor. Define qual estratégia decide se uma
/// oferta é aceitável.
/// </summary>
public enum ModalidadeNegociacao
{
    PrecoFixo = 1,

    AceitaOferta = 2,

    Liquidacao = 3
}
