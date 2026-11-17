using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioProposta
{
    List<Proposta> ObterTodas();

    List<Proposta> ObterPorAnuncio(int produtoId);

    Proposta? ObterPorId(int id);

    Proposta Adicionar(Proposta proposta);

    bool Atualizar(Proposta proposta);
}
