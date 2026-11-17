namespace RarityRoom.Modelos;

/// <summary>Dados públicos da loja, usados na vitrine do vendedor.</summary>
public record LojaResumo(
    int Id,
    string Nome,
    string Descricao,
    string FotoUrl);
