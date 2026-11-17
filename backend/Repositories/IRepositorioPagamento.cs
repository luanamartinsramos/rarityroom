using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioPagamento
{
    Pagamento? ObterPorId(int id);

    Pagamento? ObterPorChave(int compradorId, string chaveIdempotencia);

    List<Pagamento> ObterPendentesDoComprador(int compradorId);

    Pagamento Adicionar(Pagamento pagamento);

    void Atualizar(Pagamento pagamento);
}
