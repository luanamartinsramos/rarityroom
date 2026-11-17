using RarityRoom.Modelos.Enums;

namespace RarityRoom.Modelos.Negociacao;

/// <summary>
/// Somente o valor anunciado é aceito. Qualquer oferta abaixo é recusada,
/// por mais próxima que esteja.
/// </summary>
public class EstrategiaPrecoFixo : IEstrategiaNegociacao
{
    public ModalidadeNegociacao Modalidade => ModalidadeNegociacao.PrecoFixo;

    public decimal ValorMinimo(decimal precoAnuncio)
    {
        return Math.Round(precoAnuncio, 2);
    }

    public bool Aceita(decimal precoAnuncio, decimal valorOferta)
    {
        return valorOferta >= ValorMinimo(precoAnuncio);
    }
}
