using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

/// <summary>Dublê de teste do repositório de contas.</summary>
public class RepositorioUsuarioMemoria : IRepositorioUsuario
{
    private readonly List<Usuario> usuarios = new();

    private readonly object trava = new();

    private int ultimoId;

    public Usuario? ObterPorId(int id)
    {
        lock (trava)
        {
            return usuarios.FirstOrDefault(usuario => usuario.Id == id);
        }
    }

    public Usuario? ObterPorEmail(string email)
    {
        string procurado = (email ?? string.Empty).Trim();

        lock (trava)
        {
            return usuarios.FirstOrDefault(
                usuario => usuario.Email.Equals(
                    procurado,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    public List<Usuario> ObterTodos()
    {
        lock (trava)
        {
            return new List<Usuario>(usuarios);
        }
    }

    public Usuario Adicionar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        lock (trava)
        {
            usuario.DefinirId(++ultimoId);

            usuarios.Add(usuario);

            return usuario;
        }
    }

    public bool Atualizar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        lock (trava)
        {
            int indice = usuarios.FindIndex(item => item.Id == usuario.Id);

            if (indice < 0)
            {
                return false;
            }

            usuarios[indice] = usuario;

            return true;
        }
    }
}
