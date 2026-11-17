using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoLojas
{
    Loja ObterPorId(int id);

    Loja? ObterDoDono(int donoId);

    Loja Criar(int donoId, string nome, string descricao);

    Loja Atualizar(int donoId, string nome, string descricao);

    Loja DefinirFoto(int donoId, string fotoUrl);
}
