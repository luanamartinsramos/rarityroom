using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioEndereco
{
    List<Endereco> ObterPorUsuario(int usuarioId);

    Endereco? ObterPorId(int id);

    int ContarPorUsuario(int usuarioId);

    Endereco Adicionar(Endereco endereco);

    bool Atualizar(Endereco endereco);

    bool Remover(int id);
}
