namespace RarityRoom.Modelos.Admin;

/// <summary>Visão administrativa de uma conta. Não inclui hash de senha.</summary>
public record UsuarioAdmin(
    int Id,
    string Nome,
    string Email,
    string TipoConta,
    bool Ativo,
    bool Administrador,
    DateTime CriadoEm);
