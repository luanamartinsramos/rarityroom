using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioUsuarioMemoria : IRepositorioUsuario
{
    private readonly List<Usuario> usuarios = new();

    private int proximoId = 1;

    public Usuario? ObterPorId(int id)
    {
        return usuarios.FirstOrDefault(usuario => usuario.Id == id);
    }

    public Usuario? ObterPorEmail(string email)
    {
        return usuarios.FirstOrDefault(
            usuario =>
                usuario.Email.Equals(
                    email,
                    StringComparison.OrdinalIgnoreCase));
    }

    public List<Usuario> ObterTodos()
    {
        return usuarios;
    }

    public Usuario Adicionar(Usuario usuario)
    {
        usuarios.Add(usuario);

        proximoId++;

        return usuario;
    }

    public int ObterProximoId()
    {
        return proximoId;
    }
}