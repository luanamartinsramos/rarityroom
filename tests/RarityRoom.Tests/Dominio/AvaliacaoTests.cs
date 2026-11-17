using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

public class AvaliacaoTests
{
    private static Pedido PedidoConcluido(int vendedorId = 1, int compradorId = 2)
    {
        var produto = Apoio.Produto(proprietarioId: vendedorId);
        produto.DefinirId(10);

        var pedido = NovoPedido(produto, compradorId);
        pedido.DefinirId(20);
        pedido.ConfirmarPagamento(DateTime.UtcNow);
        pedido.Concluir();

        return pedido;
    }

    [Fact]
    public void Construtor_DeveVincularPedidoEAvaliado()
    {
        var avaliacao = new Avaliacao(
            PedidoConcluido(vendedorId: 1, compradorId: 2),
            autorId: 2,
            nota: 5,
            comentario: "  Excelente  ");

        Assert.Equal(1, avaliacao.VendedorId);
        Assert.Equal(2, avaliacao.AutorId);
        Assert.Equal("Excelente", avaliacao.Comentario);
    }

    [Fact]
    public void Construtor_DeveVincularOProdutoAvaliado()
    {
        var avaliacao = new Avaliacao(
            PedidoConcluido(),
            autorId: 2,
            nota: 4,
            comentario: string.Empty);

        Assert.Equal(10, avaliacao.ProdutoId);
    }

    [Fact]
    public void Construtor_PedidoPendente_DeveFalhar()
    {
        var produto = Apoio.Produto(proprietarioId: 1);
        produto.DefinirId(10);
        var pedido = NovoPedido(produto, compradorId: 2);
        pedido.DefinirId(20);

        Assert.Throws<ConflitoException>(
            () => new Avaliacao(pedido, autorId: 2, nota: 5, comentario: string.Empty));
    }

    [Fact]
    public void Construtor_ProdutoForaDoPedido_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => new Avaliacao(PedidoConcluido(), 2, 5, string.Empty, produtoId: 999));
    }

    [Fact]
    public void Construtor_AutorQueNaoComprou_DeveNegar()
    {
        Assert.Throws<AcessoNegadoException>(
            () => new Avaliacao(
                PedidoConcluido(compradorId: 2),
                autorId: 3,
                nota: 5,
                comentario: string.Empty));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Construtor_NotaForaDaFaixa_DeveFalhar(int nota)
    {
        Assert.Throws<ValidacaoException>(
            () => new Avaliacao(
                PedidoConcluido(),
                autorId: 2,
                nota: nota,
                comentario: string.Empty));
    }

    private static Pedido NovoPedido(Produto produto, int compradorId)
    {
        return new Pedido(
            compradorId,
            produto.UsuarioId,
            new[] { new ItemPedido(produto, 1, produto.Preco) },
            Apoio.Entrega(),
            FormaPagamento.Pix);
    }
}
