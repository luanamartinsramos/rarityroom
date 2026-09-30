using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using RarityRoom.Modelos;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/autenticacao")]
public class AutenticacaoController : ControllerBase
{
    private readonly IServicoAutenticacao servicoAutenticacao;

    public AutenticacaoController(
        IServicoAutenticacao servicoAutenticacao)
    {
        this.servicoAutenticacao = servicoAutenticacao;
    }

    [HttpPost("cadastrar")]
    public IActionResult Cadastrar(ModeloCadastro modelo)
    {
        try
        {
            var usuario = servicoAutenticacao.Cadastrar(
                modelo.Nome,
                modelo.Email,
                modelo.Senha
            );

            return Ok(new
            {
                mensagem = "Usuário cadastrado com sucesso.",
                usuario = new
                {
                    usuario.Id,
                    usuario.Nome,
                    usuario.Email
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpPost("entrar")]
    public async Task<IActionResult> Entrar(ModeloLogin modelo)
    {
        var usuario = servicoAutenticacao.Entrar(
            modelo.Email,
            modelo.Senha
        );

        if (usuario == null)
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha inválidos."
            });
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                usuario.Id.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                usuario.Nome
            ),

            new Claim(
                ClaimTypes.Email,
                usuario.Email
            )
        };

        var identidade = new ClaimsIdentity(
            claims,
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme
);

        var principal = new ClaimsPrincipal(identidade);

        await HttpContext.SignInAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );

        return Ok(new
        {
            mensagem = "Login realizado com sucesso.",
            usuario = new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Email
            }
        });
    }

    [Authorize]
    [HttpPost("sair")]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme
        );

        return Ok(new
        {
            mensagem = "Logout realizado com sucesso."
        });
    }

    [Authorize]
    [HttpGet("usuário-atual")]
    public IActionResult UsuarioAtual()
    {
        return Ok(new
        {
            id = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            ),

            nome = User.FindFirstValue(
                ClaimTypes.Name
            ),

            email = User.FindFirstValue(
                ClaimTypes.Email
            )
        });
    }

}
