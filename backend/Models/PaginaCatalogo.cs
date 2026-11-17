namespace RarityRoom.Modelos;

/// <summary>
/// Página do catálogo com o total de itens que atendem ao filtro — é o total
/// que permite ao cliente desenhar a navegação por páginas.
/// </summary>
public record PaginaCatalogo(
    List<ProdutoCatalogo> Itens,
    int Total,
    int Pagina,
    int TamanhoPagina);
