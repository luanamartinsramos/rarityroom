using RarityRoom.Modelos;
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
        return repositorioUsuario.ObterPorEmail(email) != null;
    }

    public Usuario Cadastrar(
        string nome,
        string email,
        string senha)
    {
        if (EmailExiste(email))
        {
            throw new InvalidOperationException(
                "Este e-mail já está cadastrado.");
        }

        var usuario = new Usuario(
            id: repositorioUsuario.ObterTodos().Count + 1,
            nome: nome,
            email: email,
            senhaHash: hashSenha.CriarHash(senha)
        );

        return repositorioUsuario.Adicionar(usuario);
    }

    public Usuario? Entrar(
        string email,
        string senha)
    {
        var usuario = repositorioUsuario.ObterPorEmail(email);

        if (usuario == null)
        {
            return null;
        }

        if (!usuario.Ativo)
        {
            return null;
        }

        bool senhaValida = hashSenha.VerificarSenha(
            senha,
            usuario.SenhaHash
        );

        if (!senhaValida)
        {
            return null;
        }

        return usuario;
    }
}