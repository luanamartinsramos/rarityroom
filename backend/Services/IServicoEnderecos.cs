using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoEnderecos
{
    List<Endereco> Listar(int usuarioId);

    Endereco Criar(int usuarioId, DadosEntrega dados);

    Endereco Atualizar(int id, int usuarioId, DadosEntrega dados);

    void Remover(int id, int usuarioId);

    Endereco DefinirPrincipal(int id, int usuarioId);
}
