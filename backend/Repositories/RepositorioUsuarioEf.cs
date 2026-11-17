using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioUsuarioEf : IRepositorioUsuario
{
    private readonly RarityRoomContexto contexto;

    public RepositorioUsuarioEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public Usuario? ObterPorId(int id)
    {
        return contexto.Usuarios.FirstOrDefault(usuario => usuario.Id == id);
    }

    /// <summary>
    /// E-mail é comparado sem diferenciar caixa (RN13). A tradução para SQL
    /// usa LOWER, que o SQLite suporta nativamente.
    /// </summary>
    public Usuario? ObterPorEmail(string email)
    {
        string procurado = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (procurado.Length == 0)
        {
            return null;
        }

        return contexto.Usuarios.FirstOrDefault(
            usuario => usuario.Email.ToLower() == procurado);
    }

    public List<Usuario> ObterTodos()
    {
        return contexto.Usuarios
            .AsNoTracking()
            .OrderBy(usuario => usuario.Id)
            .ToList();
    }

    public bool Atualizar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        bool existe = contexto.Usuarios.Any(item => item.Id == usuario.Id);

        if (!existe)
        {
            return false;
        }

        contexto.Usuarios.Update(usuario);
        contexto.SaveChanges();

        return true;
    }

    public Usuario Adicionar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        contexto.Usuarios.Add(usuario);
        contexto.SaveChanges();

        return usuario;
    }
}
