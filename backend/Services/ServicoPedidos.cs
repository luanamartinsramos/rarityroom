using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Fecha o ciclo de compra. O pedido nasce pendente e só o vendedor conclui;
/// concluir é o que transforma o anúncio em vendido.
/// </summary>
public class ServicoPedidos : IServicoPedidos
{
    private readonly IRepositorioPedido repositorioPedido;

    private readonly IRepositorioProduto repositorioProduto;

    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly IServicoNotificacoes notificacoes;

    public ServicoPedidos(
        IRepositorioPedido repositorioPedido,
        IRepositorioProduto repositorioProduto,
        IRepositorioUsuario repositorioUsuario,
        IServicoNotificacoes notificacoes)
    {
        this.repositorioPedido = repositorioPedido;
        this.repositorioProduto = repositorioProduto;
        this.repositorioUsuario = repositorioUsuario;
        this.notificacoes = notificacoes;
    }

    public List<Pedido> Listar(int usuarioId)
    {
        var compras = repositorioPedido.ObterPorComprador(usuarioId);
        var vendas = repositorioPedido.ObterPorVendedor(usuarioId);

        return compras
            .Concat(vendas)
            .DistinctBy(pedido => pedido.Id)
            .OrderByDescending(pedido => pedido.CriadoEm)
            .ToList();
    }

    public Pedido ObterPorId(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        bool participa = pedido.CompradorId == solicitanteId
            || pedido.VendedorId == solicitanteId;

        if (!participa)
        {
            throw new AcessoNegadoException(
                "Você não participa deste pedido.");
        }

        return pedido;
    }

    public Pedido Criar(
        int produtoId,
        DadosEntrega entrega,
        FormaPagamento formaPagamento,
        int compradorId)
    {
        var comprador = repositorioUsuario.ObterPorId(compradorId);

        if (comprador == null)
        {
            throw new NaoEncontradoException("Usuário não encontrado.");
        }

        var produto = ObterProduto(produtoId);

        if (repositorioPedido.ObterPendentePorProduto(produtoId) != null)
        {
            throw new ConflitoException(
                "Este anúncio já possui um pedido em andamento.");
        }

        var pedido = new Pedido(
            produto,
            compradorId,
            entrega,
            formaPagamento);

        var salvo = repositorioPedido.Adicionar(pedido);

        notificacoes.Notificar(
            pedido.VendedorId,
            TipoNotificacao.PedidoCriado,
            $"Novo pedido de \"{pedido.NomeProduto}\". Confira em vendas.");

        return salvo;
    }

    /// <summary>
    /// Pagamento demonstrativo: o resultado vem do cliente e fica registrado.
    /// Quando recusado, o status é persistido antes de a exceção subir.
    /// </summary>
    public Pedido Pagar(int id, bool aprovado, int solicitanteId)
    {
        var pedido = Obter(id);

        pedido.ValidarComprador(solicitanteId);

        try
        {
            pedido.Pagar(aprovado);
        }
        catch (PagamentoRecusadoException)
        {
            repositorioPedido.Atualizar(pedido);

            throw;
        }

        repositorioPedido.Atualizar(pedido);

        notificacoes.Notificar(
            pedido.VendedorId,
            TipoNotificacao.PedidoPago,
            $"O pagamento de \"{pedido.NomeProduto}\" foi aprovado. Prepare o envio.");

        return pedido;
    }

    public Pedido Enviar(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        pedido.ValidarVendedor(solicitanteId);

        pedido.Enviar();

        repositorioPedido.Atualizar(pedido);

        notificacoes.Notificar(
            pedido.CompradorId,
            TipoNotificacao.PedidoEnviado,
            $"Seu pedido de \"{pedido.NomeProduto}\" foi enviado.");

        return pedido;
    }

    public Pedido Entregar(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        pedido.ValidarComprador(solicitanteId);

        pedido.Entregar();

        repositorioPedido.Atualizar(pedido);

        return pedido;
    }

    public Pedido Concluir(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        pedido.ValidarVendedor(solicitanteId);

        pedido.Concluir();

        var produto = ObterProduto(pedido.ProdutoId);

        // O estado e o estoque do anúncio continuam sendo autoridade do
        // próprio anúncio: vender um item já vendido falha aqui.
        produto.Vender();

        repositorioProduto.Atualizar(produto);
        repositorioPedido.Atualizar(pedido);

        notificacoes.Notificar(
            pedido.CompradorId,
            TipoNotificacao.PedidoConcluido,
            $"Seu pedido de \"{pedido.NomeProduto}\" foi concluído.");

        return pedido;
    }

    public Pedido Cancelar(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        bool participa = pedido.CompradorId == solicitanteId
            || pedido.VendedorId == solicitanteId;

        if (!participa)
        {
            throw new AcessoNegadoException(
                "Você não participa deste pedido.");
        }

        pedido.Cancelar();

        repositorioPedido.Atualizar(pedido);

        return pedido;
    }

    private Pedido Obter(int id)
    {
        var pedido = repositorioPedido.ObterPorId(id);

        if (pedido == null)
        {
            throw new NaoEncontradoException("Pedido não encontrado.");
        }

        return pedido;
    }

    private Produto ObterProduto(int produtoId)
    {
        var produto = repositorioProduto.ObterPorId(produtoId);

        if (produto == null)
        {
            throw new NaoEncontradoException("Anúncio não encontrado.");
        }

        return produto;
    }
}
