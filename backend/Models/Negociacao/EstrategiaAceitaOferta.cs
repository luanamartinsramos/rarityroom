using RarityRoom.Modelos.Enums;

namespace RarityRoom.Modelos.Negociacao;

/// <summary>
/// Aceita ofertas a partir de 85% do valor anunciado.
/// </summary>
public class EstrategiaAceitaOferta : IEstrategiaNegociacao
{
    private const decimal PercentualMinimo = 0.85m;

    public ModalidadeNegociacao Modalidade => ModalidadeNegociacao.AceitaOferta;

    public decimal ValorMinimo(decimal precoAnuncio)
    {
        return Math.Round(precoAnuncio * PercentualMinimo, 2);
    }

    public bool Aceita(decimal precoAnuncio, decimal valorOferta)
    {
        return valorOferta >= ValorMinimo(precoAnuncio);
    }
}
