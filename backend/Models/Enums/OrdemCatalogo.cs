namespace RarityRoom.Modelos.Enums;

/// <summary>Critério de ordenação do catálogo.</summary>
public enum OrdemCatalogo
{
    Relevancia = 1,

    MenorPreco = 2,

    MaiorPreco = 3,

    /// <summary>Anúncios publicados recentemente primeiro (antigo "Novidades").</summary>
    MaisRecentes = 4,

    MaisAntigos = 5,

    /// <summary>Quantidade de unidades vendidas em pedidos com pagamento aprovado.</summary>
    MaisVendidos = 6,

    /// <summary>Média das avaliações reais; anúncios sem avaliação vão ao fim.</summary>
    MelhorAvaliados = 7
}
