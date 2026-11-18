using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

public class ServicoPerfil : IServicoPerfil
{
    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly IHashSenha hashSenha;

    public ServicoPerfil(IRepositorioUsuario repositorioUsuario, IHashSenha hashSenha)
    {
        this.repositorioUsuario = repositorioUsuario;
        this.hashSenha = hashSenha;
    }

    public PerfilUsuario Obter(int usuarioId)
    {
        return ParaPerfil(ObterUsuario(usuarioId));
    }

    public PerfilUsuario AtualizarNome(int usuarioId, string nome)
    {
        var usuario = ObterUsuario(usuarioId);

        usuario.AlterarNome(nome);

        repositorioUsuario.Atualizar(usuario);

        return ParaPerfil(usuario);
    }

    /// <summary>
    /// O e-mail é o login e o destino da recuperação de senha: trocá-lo exige
    /// a senha atual, senão uma sessão aberta num computador emprestado
    /// bastaria para tomar a conta.
    /// </summary>
    public PerfilUsuario AtualizarEmail(int usuarioId, string email, string senhaAtual)
    {
        var usuario = ObterUsuario(usuarioId);

        if (string.IsNullOrEmpty(senhaAtual)
            || !hashSenha.VerificarSenha(senhaAtual, usuario.SenhaHash))
        {
            throw new ValidacaoException("Senha atual incorreta.");
        }

        email ??= string.Empty;

        var existente = repositorioUsuario.ObterPorEmail(email);

        if (existente != null && existente.Id != usuarioId)
        {
            throw new ConflitoException("Este e-mail já está em uso.");
        }

        usuario.AlterarEmail(email);

        repositorioUsuario.Atualizar(usuario);

        return ParaPerfil(usuario);
    }

    public PerfilUsuario DefinirFoto(int usuarioId, string fotoUrl)
    {
        var usuario = ObterUsuario(usuarioId);

        usuario.DefinirFoto(fotoUrl);

        repositorioUsuario.Atualizar(usuario);

        return ParaPerfil(usuario);
    }

    private Usuario ObterUsuario(int usuarioId)
    {
        var usuario = repositorioUsuario.ObterPorId(usuarioId);

        if (usuario == null)
        {
            throw new NaoEncontradoException("Usuário não encontrado.");
        }

        return usuario;
    }

    private static PerfilUsuario ParaPerfil(Usuario usuario)
    {
        return new PerfilUsuario(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.TipoConta.ToString(),
            usuario.Administrador,
            usuario.FotoUrl,
            usuario.CriadoEm);
    }
}
