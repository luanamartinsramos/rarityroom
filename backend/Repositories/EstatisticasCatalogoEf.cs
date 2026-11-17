using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;

namespace RarityRoom.Repositorios;

public class EstatisticasCatalogoEf : IEstatisticasCatalogo
{
    private readonly RarityRoomContexto contexto;

    public EstatisticasCatalogoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public Dictionary<int, IndicadoresProduto> ObterIndicadores()
    {
        // Venda confirmada = pagamento aprovado. Pedido cancelado depois do
        // pagamento (estornado) não conta.
        var vendas = contexto.Pedidos
            .AsNoTracking()
            .Where(pedido => pedido.StatusPagamento == StatusPagamento.Pago
                && pedido.Estado != EstadoPedido.Cancelado)
            .SelectMany(pedido => pedido.Itens)
            .GroupBy(item => item.ProdutoId)
            .Select(grupo => new
            {
                ProdutoId = grupo.Key,
                Quantidade = grupo.Sum(item => item.Quantidade)
            })
            .ToDictionary(item => item.ProdutoId, item => item.Quantidade);

        var avaliacoes = contexto.Avaliacoes
            .AsNoTracking()
            .GroupBy(avaliacao => avaliacao.ProdutoId)
            .Select(grupo => new
            {
                ProdutoId = grupo.Key,
                Media = grupo.Average(avaliacao => (double)avaliacao.Nota),
                Total = grupo.Count()
            })
            .ToDictionary(item => item.ProdutoId);

        var resultado = new Dictionary<int, IndicadoresProduto>();

        foreach (int produtoId in vendas.Keys.Union(avaliacoes.Keys))
        {
            vendas.TryGetValue(produtoId, out int vendidos);
            avaliacoes.TryGetValue(produtoId, out var nota);

            resultado[produtoId] = new IndicadoresProduto(
                vendidos,
                nota == null ? 0m : Math.Round((decimal)nota.Media, 2),
                nota?.Total ?? 0);
        }

        return resultado;
    }
}
