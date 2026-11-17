using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

public class ServicoAutenticacao : IServicoAutenticacao
{
    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly IHashSenha hashSenha;

    public ServicoAutenticacao(
        IRepositorioUsuario repositorioUsuario,
        IHashSenha hashSenha)
    {
        this.repositorioUsuario = repositorioUsuario;
        this.hashSenha = hashSenha;
    }

    public bool EmailExiste(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return repositorioUsuario.ObterPorEmail(email) != null;
    }

    public Usuario Cadastrar(
        string nome,
        string email,
        string senha)
    {
        Usuario.ValidarSenha(senha);

        if (EmailExiste(email))
        {
            throw new ConflitoException(
                "Este e-mail já está cadastrado.");
        }

        // Toda conta nasce compradora e vendedora (escopo do marketplace).
        var usuario = new Usuario(
            nome,
            email,
            hashSenha.CriarHash(senha),
            TipoConta.Ambos
        );

        return repositorioUsuario.Adicionar(usuario);
    }

    public Usuario? Entrar(
        string email,
        string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(senha))
        {
            return null;
        }

        var usuario = repositorioUsuario.ObterPorEmail(email);

        if (usuario == null || !usuario.Ativo)
        {
            return null;
        }

        if (!hashSenha.VerificarSenha(senha, usuario.SenhaHash))
        {
            return null;
        }

        return usuario;
    }
}
