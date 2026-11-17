using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/autenticacao")]
public class AutenticacaoController : ControllerBase
{
    private readonly IServicoAutenticacao servicoAutenticacao;

    private readonly IServicoRecuperacaoSenha servicoRecuperacaoSenha;

    public AutenticacaoController(
        IServicoAutenticacao servicoAutenticacao,
        IServicoRecuperacaoSenha servicoRecuperacaoSenha)
    {
        this.servicoAutenticacao = servicoAutenticacao;
        this.servicoRecuperacaoSenha = servicoRecuperacaoSenha;
    }

    /// <summary>
    /// Gera o token de redefinição. Não há provedor de e-mail configurado:
    /// o token volta na resposta, marcado como demonstração.
    /// </summary>
    [HttpPost("recuperar-senha")]
    [AllowAnonymous]
    public IActionResult RecuperarSenha(ModeloRecuperacao modelo)
    {
        string? token = servicoRecuperacaoSenha.Solicitar(modelo.Email);

        return Ok(new
        {
            mensagem = token == null
                ? "Se este e-mail estiver cadastrado, um token foi gerado."
                : "Token gerado. Use-o para definir a nova senha.",
            demonstracao = true,
            token
        });
    }

    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    public IActionResult RedefinirSenha(ModeloRedefinicao modelo)
    {
        servicoRecuperacaoSenha.Redefinir(
            modelo.Email,
            modelo.Token,
            modelo.NovaSenha);

        return Ok(new { mensagem = "Senha redefinida com sucesso." });
    }

    /// <summary>
    /// Sem try/catch local: o middleware de exceções traduz o conflito de
    /// e-mail duplicado em 409 com código estável.
    /// </summary>
    [HttpPost("cadastrar")]
    public IActionResult Cadastrar(ModeloCadastro modelo)
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
            ),

            new Claim("tipoConta", usuario.TipoConta.ToString()),

            new Claim(
                "administrador",
                usuario.Administrador ? "true" : "false")
        };

        var identidade = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identidade);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
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
            CookieAuthenticationDefaults.AuthenticationScheme
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
            ),

            tipoConta = User.FindFirstValue("tipoConta"),

            administrador =
                User.FindFirstValue("administrador") == "true"
        });
    }

    /// <summary>
    /// Vazio assume "Ambos" para não quebrar clientes antigos.
    /// </summary>
}
