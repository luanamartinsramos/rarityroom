using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

public class ServicoAvaliacoes : IServicoAvaliacoes
{
    private readonly IRepositorioAvaliacao repositorioAvaliacao;

    private readonly IRepositorioPedido repositorioPedido;

    public ServicoAvaliacoes(
        IRepositorioAvaliacao repositorioAvaliacao,
        IRepositorioPedido repositorioPedido)
    {
        this.repositorioAvaliacao = repositorioAvaliacao;
        this.repositorioPedido = repositorioPedido;
    }

    public Avaliacao Avaliar(
        int pedidoId,
        int autorId,
        int nota,
        string comentario)
    {
        var pedido = repositorioPedido.ObterPorId(pedidoId);

        if (pedido == null)
        {
            throw new NaoEncontradoException("Pedido não encontrado.");
        }

        if (repositorioAvaliacao.ObterPorPedido(pedidoId) != null)
        {
            throw new ConflitoException("Este pedido já foi avaliado.");
        }

        var avaliacao = new Avaliacao(pedido, autorId, nota, comentario);

        return repositorioAvaliacao.Adicionar(avaliacao);
    }

    public ReputacaoVendedor ObterReputacao(int vendedorId)
    {
        var avaliacoes = repositorioAvaliacao.ObterPorVendedor(vendedorId);

        if (avaliacoes.Count == 0)
        {
            return new ReputacaoVendedor(0m, 0);
        }

        decimal media = Math.Round(
            (decimal)avaliacoes.Average(avaliacao => avaliacao.Nota),
            2);

        return new ReputacaoVendedor(media, avaliacoes.Count);
    }

    public ResumoAvaliacoes ObterResumoProduto(int produtoId)
    {
        var avaliacoes = repositorioAvaliacao.ObterPorProduto(produtoId);

        decimal media = avaliacoes.Count == 0
            ? 0m
            : Math.Round(
                (decimal)avaliacoes.Average(avaliacao => avaliacao.Nota),
                2);

        return new ResumoAvaliacoes(
            produtoId,
            media,
            avaliacoes.Count,
            avaliacoes);
    }

    public List<Avaliacao> ListarDoVendedor(int vendedorId)
    {
        return repositorioAvaliacao.ObterPorVendedor(vendedorId);
    }
}
