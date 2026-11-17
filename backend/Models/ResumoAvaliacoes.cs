namespace RarityRoom.Modelos;

/// <summary>
/// Nota média de um anúncio, derivada das avaliações vinculadas a pedidos
/// concluídos.
/// </summary>
public record ResumoAvaliacoes(
    int ProdutoId,
    decimal Media,
    int Total,
    List<Avaliacao> Avaliacoes);
