using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioPropostaEf : IRepositorioProposta
{
    private readonly RarityRoomContexto contexto;

    public RepositorioPropostaEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public List<Proposta> ObterTodas()
    {
        return contexto.Propostas
            .AsNoTracking()
            .OrderByDescending(proposta => proposta.CriadaEm)
            .ToList();
    }

    /// <summary>
    /// Rastreado de propósito: o aceite recusa as demais propostas pendentes
    /// no mesmo ciclo, e as alterações precisam ser persistidas.
    /// </summary>
    public List<Proposta> ObterPorAnuncio(int produtoId)
    {
        return contexto.Propostas
            .Where(proposta => proposta.ProdutoId == produtoId)
            .ToList();
    }

    public Proposta? ObterPorId(int id)
    {
        return contexto.Propostas.FirstOrDefault(proposta => proposta.Id == id);
    }

    public Proposta Adicionar(Proposta proposta)
    {
        ArgumentNullException.ThrowIfNull(proposta);

        contexto.Propostas.Add(proposta);
        contexto.SaveChanges();

        return proposta;
    }

    public bool Atualizar(Proposta proposta)
    {
        ArgumentNullException.ThrowIfNull(proposta);

        bool existe = contexto.Propostas.Any(item => item.Id == proposta.Id);

        if (!existe)
        {
            return false;
        }

        contexto.Propostas.Update(proposta);
        contexto.SaveChanges();

        return true;
    }
}
