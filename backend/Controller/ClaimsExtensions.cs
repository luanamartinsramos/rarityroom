using System.Security.Claims;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Controllers;

/// <summary>
/// Leitura do identificador do usuário autenticado a partir do cookie. Antes
/// cada controller repetia o mesmo parse; a falha também é única.
/// </summary>
internal static class ClaimsExtensions
{
    public static int ObterUsuarioId(this ClaimsPrincipal usuario)
    {
        string? identificador = usuario.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(identificador, out int usuarioId))
        {
            throw new AcessoNegadoException(
                "Sessão inválida. Entre novamente.");
        }

        return usuarioId;
    }
}
