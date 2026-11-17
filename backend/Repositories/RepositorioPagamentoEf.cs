using RarityRoom.Dados;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;

namespace RarityRoom.Repositorios;

public class RepositorioPagamentoEf : IRepositorioPagamento
{
    private readonly RarityRoomContexto contexto;

    public RepositorioPagamentoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public Pagamento? ObterPorId(int id)
    {
        return contexto.Pagamentos.FirstOrDefault(pagamento => pagamento.Id == id);
    }

    public Pagamento? ObterPorChave(int compradorId, string chaveIdempotencia)
    {
        return contexto.Pagamentos.FirstOrDefault(
            pagamento => pagamento.CompradorId == compradorId
                && pagamento.ChaveIdempotencia == chaveIdempotencia);
    }

    public List<Pagamento> ObterPendentesDoComprador(int compradorId)
    {
        return contexto.Pagamentos
            .Where(pagamento => pagamento.CompradorId == compradorId
                && pagamento.Status == StatusPagamento.Pendente)
            .ToList();
    }

    public Pagamento Adicionar(Pagamento pagamento)
    {
        contexto.Pagamentos.Add(pagamento);
        contexto.SaveChanges();

        return pagamento;
    }

    public void Atualizar(Pagamento pagamento)
    {
        contexto.Pagamentos.Update(pagamento);
        contexto.SaveChanges();
    }
}
