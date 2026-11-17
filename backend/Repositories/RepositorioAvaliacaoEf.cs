using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioAvaliacaoEf : IRepositorioAvaliacao
{
    private readonly RarityRoomContexto contexto;

    public RepositorioAvaliacaoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public Avaliacao? ObterPorPedido(int pedidoId, int produtoId)
    {
        return contexto.Avaliacoes.FirstOrDefault(
            avaliacao => avaliacao.PedidoId == pedidoId
                && avaliacao.ProdutoId == produtoId);
    }

    public List<Avaliacao> ObterPorVendedor(int vendedorId)
    {
        return contexto.Avaliacoes
            .AsNoTracking()
            .Where(avaliacao => avaliacao.VendedorId == vendedorId)
            .OrderByDescending(avaliacao => avaliacao.CriadaEm)
            .ToList();
    }

    public List<Avaliacao> ObterPorProduto(int produtoId)
    {
        return contexto.Avaliacoes
            .AsNoTracking()
            .Where(avaliacao => avaliacao.ProdutoId == produtoId)
            .OrderByDescending(avaliacao => avaliacao.CriadaEm)
            .ToList();
    }

    public Avaliacao Adicionar(Avaliacao avaliacao)
    {
        ArgumentNullException.ThrowIfNull(avaliacao);

        contexto.Avaliacoes.Add(avaliacao);
        contexto.SaveChanges();

        return avaliacao;
    }
}
