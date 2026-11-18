using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RarityRoom.Modelos;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Monta as claims do cookie de sessão e as revalida a cada requisição.
///
/// Sem a revalidação, o cookie valeria até expirar mesmo que a conta fosse
/// suspensa ou a senha redefinida (alguém com a sessão roubada continuaria
/// dentro). O "carimbo" é derivado do hash da senha: trocar a senha muda o
/// carimbo e derruba todas as sessões antigas.
/// </summary>
public static class SessaoUsuario
{
    public const string ClaimCarimbo = "carimbo";

    public static ClaimsPrincipal CriarPrincipal(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new("tipoConta", usuario.TipoConta.ToString()),
            new("administrador", usuario.Administrador ? "true" : "false"),
            new(ClaimCarimbo, Carimbo(usuario)),
        };

        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static string Carimbo(Usuario usuario)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(usuario.SenhaHash));

        return Convert.ToHexString(bytes, 0, 12);
    }

    /// <summary>Evento OnValidatePrincipal do cookie.</summary>
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var principal = contexto.Principal;

        if (principal == null
            || !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out int id))
        {
            await Rejeitar(contexto);
            return;
        }

        var repositorio = contexto.HttpContext.RequestServices
            .GetRequiredService<IRepositorioUsuario>();

        var usuario = repositorio.ObterPorId(id);

        if (usuario == null
            || !usuario.Ativo
            || principal.FindFirstValue(ClaimCarimbo) != Carimbo(usuario))
        {
            await Rejeitar(contexto);
            return;
        }

        bool desatualizado =
            principal.FindFirstValue(ClaimTypes.Name) != usuario.Nome
            || principal.FindFirstValue(ClaimTypes.Email) != usuario.Email
            || principal.FindFirstValue("tipoConta") != usuario.TipoConta.ToString()
            || principal.FindFirstValue("administrador") != (usuario.Administrador ? "true" : "false");

        if (desatualizado)
        {
            contexto.ReplacePrincipal(CriarPrincipal(usuario));
            contexto.ShouldRenew = true;
        }
    }

    private static async Task Rejeitar(CookieValidatePrincipalContext contexto)
    {
        contexto.RejectPrincipal();

        await contexto.HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
