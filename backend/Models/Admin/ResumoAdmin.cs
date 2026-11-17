namespace RarityRoom.Modelos.Admin;

/// <summary>Indicadores da plataforma para o painel administrativo.</summary>
public record ResumoAdmin(
    int Usuarios,
    int UsuariosAtivos,
    int AnunciosAtivos,
    int AnunciosVendidos,
    int Pedidos,
    decimal TotalVendido);
