namespace RarityRoom.Modelos;

/// <summary>
/// Resumo da reputação de um vendedor. Média com duas casas e total de
/// avaliações; vendedor sem avaliação responde média zero e total zero.
/// </summary>
public record ReputacaoVendedor(decimal Media, int Total);
