using RarityRoom.Modelos.Enums;

namespace RarityRoom.Modelos.Negociacao;

/// <summary>
/// Aceita ofertas a partir de 70% do valor anunciado, para desova de estoque.
/// </summary>
public class EstrategiaLiquidacao : IEstrategiaNegociacao
{
    private const decimal PercentualMinimo = 0.70m;

    public ModalidadeNegociacao Modalidade => ModalidadeNegociacao.Liquidacao;

    public decimal ValorMinimo(decimal precoAnuncio)
    {
        return Math.Round(precoAnuncio * PercentualMinimo, 2);
    }

    public bool Aceita(decimal precoAnuncio, decimal valorOferta)
    {
        return valorOferta >= ValorMinimo(precoAnuncio);
    }
}
