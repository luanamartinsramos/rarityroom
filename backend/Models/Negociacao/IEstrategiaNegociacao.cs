using RarityRoom.Modelos.Enums;

namespace RarityRoom.Modelos.Negociacao;

/// <summary>
/// Contrato comum de avaliação de ofertas. Cada modalidade de negociação é
/// uma implementação; o serviço escolhe a estratégia pelo enum do anúncio e
/// nunca pergunta "qual é o tipo" — apenas chama <see cref="Aceita"/>.
/// </summary>
public interface IEstrategiaNegociacao
{
    ModalidadeNegociacao Modalidade { get; }

    /// <summary>
    /// Menor valor que esta modalidade aceita para o preço informado.
    /// </summary>
    decimal ValorMinimo(decimal precoAnuncio);

    bool Aceita(decimal precoAnuncio, decimal valorOferta);
}
