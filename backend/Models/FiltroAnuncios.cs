namespace RarityRoom.Modelos;

/// <summary>
/// Filtro do catálogo. Todos os campos são opcionais: ausente significa
/// "não filtrar".
/// </summary>
public record FiltroAnuncios(
    string? Termo = null,
    string? Categoria = null,
    string? Estado = null,
    string? Franquia = null,
    string? Condicao = null,
    decimal? PrecoMinimo = null,
    decimal? PrecoMaximo = null,
    string? Ordem = null,
    bool? SomenteDestaque = null,
    int Pagina = 1,
    int TamanhoPagina = 0);
