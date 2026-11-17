using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

public class PedidoTests
{
    private static Produto Anuncio(int proprietarioId = 1, decimal preco = 249.90m)
    {
        var produto = Apoio.Produto(proprietarioId: proprietarioId, preco: preco);
        produto.DefinirId(10);

        return produto;
    }

    [Fact]
    public void Construtor_DeveGuardarSnapshotDoAnuncio()
    {
        var anuncio = Anuncio(preco: 249.90m);

        var pedido = NovoPedido(anuncio, compradorId: 2);

        Assert.Equal(249.90m, pedido.PrecoUnitario);
        Assert.Equal("Funko Batman", pedido.NomeProduto);
        Assert.Equal(1, pedido.VendedorId);
        Assert.Equal(EstadoPedido.Pendente, pedido.Estado);
    }

    [Fact]
    public void Snapshot_NaoMudaQuandoOAnuncioMuda()
    {
        var anuncio = Anuncio(preco: 249.90m);
        var pedido = NovoPedido(anuncio, compradorId: 2);

        anuncio.Atualizar(
            "Funko Batman", "editado", 999.90m, 1, "Funko",
            string.Empty, string.Empty, string.Empty, string.Empty);

        Assert.Equal(249.90m, pedido.PrecoUnitario);
    }

    [Fact]
    public void Construtor_NoProprioAnuncio_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => NovoPedido(Anuncio(proprietarioId: 1), compradorId: 1));
    }

    [Fact]
    public void Construtor_AnuncioIndisponivel_DeveFalhar()
    {
        var anuncio = Anuncio();
        anuncio.Pausar();

        Assert.Throws<ConflitoException>(
            () => NovoPedido(anuncio, compradorId: 2));
    }

    [Fact]
    public void Concluir_DeveAlterarEstado()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        pedido.Concluir();

        Assert.Equal(EstadoPedido.Concluido, pedido.Estado);
        Assert.NotNull(pedido.ConcluidoEm);
    }

    [Fact]
    public void Concluir_JaConcluido_DeveFalhar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);
        pedido.Concluir();

        var excecao = Assert.Throws<TransicaoInvalidaException>(
            () => pedido.Concluir());

        Assert.Equal("transicao-invalida", excecao.Codigo);
    }

    [Fact]
    public void ValidarVendedor_PeloComprador_DeveNegar()
    {
        var pedido = NovoPedido(Anuncio(proprietarioId: 1), compradorId: 2);

        Assert.Throws<AcessoNegadoException>(
            () => pedido.ValidarVendedor(2));
    }

    private static Pedido NovoPedido(Produto produto, int compradorId)
    {
        return new Pedido(
            produto,
            compradorId,
            Apoio.Entrega(),
            FormaPagamento.Pix);
    }

    [Fact]
    public void Pagar_Aprovado_DeveMarcarPagoERegistrarStatus()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        pedido.Pagar(aprovado: true);

        Assert.Equal(EstadoPedido.Pago, pedido.Estado);
        Assert.Equal(StatusPagamento.Pago, pedido.StatusPagamento);
    }

    [Fact]
    public void Pagar_Recusado_DeveRegistrarRecusaEInterromper()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        var excecao = Assert.Throws<PagamentoRecusadoException>(
            () => pedido.Pagar(aprovado: false));

        Assert.Equal("pagamento-recusado", excecao.Codigo);
        Assert.Equal(StatusPagamento.Recusado, pedido.StatusPagamento);
        Assert.Equal(EstadoPedido.Pendente, pedido.Estado);
    }

    [Fact]
    public void Enviar_AntesDoPagamento_DeveFalhar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        Assert.Throws<TransicaoInvalidaException>(() => pedido.Enviar());
    }

    [Fact]
    public void CicloCompleto_PagarEnviarEntregar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        pedido.Pagar(aprovado: true);
        pedido.Enviar();
        pedido.Entregar();

        Assert.Equal(EstadoPedido.Entregue, pedido.Estado);
    }

    [Fact]
    public void Entregar_SemEnvio_DeveFalhar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);
        pedido.Pagar(aprovado: true);

        Assert.Throws<TransicaoInvalidaException>(() => pedido.Entregar());
    }
}
