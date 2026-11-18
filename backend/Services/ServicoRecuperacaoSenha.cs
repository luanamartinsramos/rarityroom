using System.Security.Cryptography;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Redefinição de senha. O token é entregue pelo <see cref="ICanalRecuperacaoSenha"/>
/// (no projeto, um e-mail simulado no log do servidor) e nunca volta na
/// resposta HTTP — senão qualquer pessoa que soubesse o e-mail de alguém
/// conseguiria trocar a senha dessa conta.
/// </summary>
public class ServicoRecuperacaoSenha : IServicoRecuperacaoSenha
{
    private static readonly TimeSpan Validade = TimeSpan.FromMinutes(30);

    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly IRepositorioRecuperacaoSenha repositorioRecuperacao;

    private readonly IHashSenha hash;

    private readonly ICanalRecuperacaoSenha canal;

    public ServicoRecuperacaoSenha(
        IRepositorioUsuario repositorioUsuario,
        IRepositorioRecuperacaoSenha repositorioRecuperacao,
        IHashSenha hash,
        ICanalRecuperacaoSenha canal)
    {
        this.repositorioUsuario = repositorioUsuario;
        this.repositorioRecuperacao = repositorioRecuperacao;
        this.hash = hash;
        this.canal = canal;
    }

    public void Solicitar(string email)
    {
        var usuario = repositorioUsuario.ObterPorEmail(email ?? string.Empty);

        if (usuario == null || !usuario.Ativo)
        {
            return;
        }

        repositorioRecuperacao.InvalidarDoUsuario(usuario.Id);

        // 256 bits de um gerador criptográfico: impossível de adivinhar.
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        repositorioRecuperacao.Adicionar(new RecuperacaoSenha(
            usuario.Id,
            token,
            DateTime.UtcNow.Add(Validade)));

        canal.Enviar(usuario.Email, usuario.Nome, token);
    }

    public void Redefinir(string email, string token, string novaSenha)
    {
        var recuperacao = repositorioRecuperacao.ObterPorToken(token);

        if (recuperacao == null || !recuperacao.EstaValido(DateTime.UtcNow))
        {
            throw new ValidacaoException(
                "Token inválido ou expirado. Solicite um novo.");
        }

        var usuario = repositorioUsuario.ObterPorEmail(email ?? string.Empty);

        if (usuario == null || usuario.Id != recuperacao.UsuarioId)
        {
            throw new ValidacaoException(
                "Token inválido ou expirado. Solicite um novo.");
        }

        Usuario.ValidarSenha(novaSenha);

        usuario.AlterarSenha(hash.CriarHash(novaSenha));

        repositorioUsuario.Atualizar(usuario);

        recuperacao.MarcarComoUsada();

        repositorioRecuperacao.Atualizar(recuperacao);
    }
}
