namespace RarityRoom.Modelos;

/// <summary>
/// Página pública do vendedor: identidade, reputação, catálogo ativo e
/// histórico. Não expõe e-mail nem qualquer dado de contato.
/// </summary>
public record PerfilVendedor(
    int Id,
    string Nome,
    string TipoConta,
    DateTime Desde,
    decimal ReputacaoMedia,
    int TotalAvaliacoes,
    int AnunciosAtivos,
    int ProdutosVendidos,
    List<Avaliacao> Avaliacoes,
    List<Produto> Anuncios,
    LojaResumo? Loja);
