using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;

namespace RarityRoom.Repositorios;

public class UnidadeDeTrabalhoEf : IUnidadeDeTrabalho
{
    private readonly RarityRoomContexto contexto;

    public UnidadeDeTrabalhoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public T Executar<T>(Func<T> operacao)
    {
        // Transação já aberta (chamada aninhada): participa dela.
        if (contexto.Database.CurrentTransaction != null)
        {
            return operacao();
        }

        using var transacao = contexto.Database.BeginTransaction();

        try
        {
            T resultado = operacao();

            transacao.Commit();

            return resultado;
        }
        catch
        {
            transacao.Rollback();

            // Entidades alteradas e não gravadas não podem vazar para a
            // próxima operação do mesmo contexto.
            contexto.ChangeTracker.Clear();

            throw;
        }
    }
}
