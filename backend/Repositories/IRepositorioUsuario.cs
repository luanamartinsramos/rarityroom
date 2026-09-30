using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioUsuario
{
    Usuario? ObterPorId(int id);

    Usuario? ObterPorEmail(string email);

    List<Usuario> ObterTodos();

    Usuario Adicionar(Usuario usuario);
}