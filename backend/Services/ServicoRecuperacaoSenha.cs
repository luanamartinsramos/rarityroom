using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Redefinição de senha sem provedor de e-mail. O token é devolvido na própria
/// resposta e marcado como demonstração — em produção ele iria por e-mail.
/// </summary>
public class ServicoRecuperacaoSenha : IServicoRecuperacaoSenha
{
    private static readonly TimeSpan Validade = TimeSpan.FromMinutes(30);

    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly IRepositorioRecuperacaoSenha repositorioRecuperacao;

    private readonly IHashSenha hash;

    public ServicoRecuperacaoSenha(
        IRepositorioUsuario repositorioUsuario,
        IRepositorioRecuperacaoSenha repositorioRecuperacao,
        IHashSenha hash)
    {
        this.repositorioUsuario = repositorioUsuario;
        this.repositorioRecuperacao = repositorioRecuperacao;
        this.hash = hash;
    }

    public string? Solicitar(string email)
    {
        var usuario = repositorioUsuario.ObterPorEmail(email ?? string.Empty);

        if (usuario == null || !usuario.Ativo)
        {
            return null;
        }

        repositorioRecuperacao.InvalidarDoUsuario(usuario.Id);

        string token = Guid.NewGuid().ToString("N");

        repositorioRecuperacao.Adicionar(new RecuperacaoSenha(
            usuario.Id,
            token,
            DateTime.UtcNow.Add(Validade)));

        return token;
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
