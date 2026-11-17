using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

/// <summary>Dublê de teste do repositório de propostas.</summary>
public class RepositorioPropostaMemoria : IRepositorioProposta
{
    private readonly List<Proposta> propostas = new();

    private readonly object trava = new();

    private int ultimoId;

    public List<Proposta> ObterTodas()
    {
        lock (trava)
        {
            return new List<Proposta>(propostas);
        }
    }

    public List<Proposta> ObterPorAnuncio(int produtoId)
    {
        lock (trava)
        {
            return propostas
                .Where(proposta => proposta.ProdutoId == produtoId)
                .ToList();
        }
    }

    public Proposta? ObterPorId(int id)
    {
        lock (trava)
        {
            return propostas.FirstOrDefault(proposta => proposta.Id == id);
        }
    }

    public Proposta Adicionar(Proposta proposta)
    {
        ArgumentNullException.ThrowIfNull(proposta);

        lock (trava)
        {
            proposta.DefinirId(++ultimoId);

            propostas.Add(proposta);

            return proposta;
        }
    }

    public bool Atualizar(Proposta proposta)
    {
        ArgumentNullException.ThrowIfNull(proposta);

        lock (trava)
        {
            int indice = propostas.FindIndex(item => item.Id == proposta.Id);

            if (indice < 0)
            {
                return false;
            }

            propostas[indice] = proposta;

            return true;
        }
    }
}
