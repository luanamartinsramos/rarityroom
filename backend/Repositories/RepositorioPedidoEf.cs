using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;

namespace RarityRoom.Repositorios;

public class RepositorioPedidoEf : IRepositorioPedido
{
    private readonly RarityRoomContexto contexto;

    public RepositorioPedidoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public List<Pedido> ObterTodos()
    {
        return contexto.Pedidos
            .AsNoTracking()
            .OrderByDescending(pedido => pedido.CriadoEm)
            .ToList();
    }

    public Pedido? ObterPorId(int id)
    {
        return contexto.Pedidos.FirstOrDefault(pedido => pedido.Id == id);
    }

    public List<Pedido> ObterPorComprador(int compradorId)
    {
        return contexto.Pedidos
            .AsNoTracking()
            .Where(pedido => pedido.CompradorId == compradorId)
            .OrderByDescending(pedido => pedido.CriadoEm)
            .ToList();
    }

    public List<Pedido> ObterPorVendedor(int vendedorId)
    {
        return contexto.Pedidos
            .AsNoTracking()
            .Where(pedido => pedido.VendedorId == vendedorId)
            .OrderByDescending(pedido => pedido.CriadoEm)
            .ToList();
    }

    /// <summary>
    /// Um anúncio só pode ter um pedido em andamento por vez — é esta consulta
    /// que impede duas pessoas comprando o mesmo item.
    /// </summary>
    public Pedido? ObterPendentePorProduto(int produtoId)
    {
        return contexto.Pedidos.FirstOrDefault(
            pedido => pedido.ProdutoId == produtoId
                && pedido.Estado == EstadoPedido.Pendente);
    }

    public Pedido Adicionar(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        contexto.Pedidos.Add(pedido);
        contexto.SaveChanges();

        return pedido;
    }

    public bool Atualizar(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        bool existe = contexto.Pedidos.Any(item => item.Id == pedido.Id);

        if (!existe)
        {
            return false;
        }

        contexto.Pedidos.Update(pedido);
        contexto.SaveChanges();

        return true;
    }
}
