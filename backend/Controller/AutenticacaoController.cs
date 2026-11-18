using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    private readonly ProtecaoForcaBruta protecao;

    public AutenticacaoController(
        IServicoAutenticacao servicoAutenticacao,
        IServicoRecuperacaoSenha servicoRecuperacaoSenha,
        ProtecaoForcaBruta protecao)
    {
        this.servicoAutenticacao = servicoAutenticacao;
        this.servicoRecuperacaoSenha = servicoRecuperacaoSenha;
        this.protecao = protecao;
    }

    /// <summary>
    /// Gera o token de redefinição e o envia ao dono da conta (e-mail simulado
    /// no log do servidor). A resposta é idêntica exista ou não o e-mail, para
    /// não revelar quem tem conta, e nunca contém o token.
    /// </summary>
    [HttpPost("recuperar-senha")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasLimite.Autenticacao)]
    public IActionResult RecuperarSenha(ModeloRecuperacao modelo)
    {
        servicoRecuperacaoSenha.Solicitar(modelo.Email);

        return Ok(new
        {
            mensagem = "Se este e-mail estiver cadastrado, enviamos as instruções para redefinir a senha."
        });
    }

    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasLimite.Autenticacao)]
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
    [EnableRateLimiting(PoliticasLimite.Autenticacao)]
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

    /// <summary>
    /// Após 5 senhas erradas seguidas a conta fica bloqueada por 15 minutos
    /// (429), e cada IP tem um limite de tentativas por minuto.
    /// </summary>
    [HttpPost("entrar")]
    [EnableRateLimiting(PoliticasLimite.Autenticacao)]
    public async Task<IActionResult> Entrar(ModeloLogin modelo)
    {
        protecao.GarantirLiberado(modelo.Email);

        var usuario = servicoAutenticacao.Entrar(
            modelo.Email,
            modelo.Senha
        );

        if (usuario == null)
        {
            protecao.RegistrarFalha(modelo.Email);

            return Unauthorized(new
            {
                mensagem = "E-mail ou senha inválidos."
            });
        }

        protecao.RegistrarSucesso(modelo.Email);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            SessaoUsuario.CriarPrincipal(usuario)
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

    /// <summary>
    /// A rota sem acento é a oficial; a com acento fica por compatibilidade
    /// (alguns clientes HTTP não aceitam caracteres fora do ASCII na URL).
    /// </summary>
    [Authorize]
    [HttpGet("usuario-atual")]
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
}
