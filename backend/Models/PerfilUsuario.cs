namespace RarityRoom.Modelos;

/// <summary>Dados da própria conta, sem hash de senha.</summary>
public record PerfilUsuario(
    int Id,
    string Nome,
    string Email,
    string TipoConta,
    bool Administrador,
    string FotoUrl,
    DateTime Desde);
