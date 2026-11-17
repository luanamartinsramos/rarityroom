using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

/// <summary>
/// RN-REAB (docs/av2/especificacao-dscode.md): repor estoque de um anúncio.
/// </summary>
public class ProdutoReabastecerTests
{
    private static Produto Esgotado()
    {
        var produto = Apoio.Produto(estoque: 1);
        produto.BaixarEstoque(1);

        return produto;
    }

    [Fact]
    public void R1_AnuncioEsgotado_VoltaADisponivelComONovoEstoque()
    {
        var produto = Esgotado();

        produto.Reabastecer(5);

        Assert.Equal(5, produto.Estoque);
        Assert.Equal(EstadoAnuncio.Disponivel, produto.Estado);
        Assert.True(produto.Disponivel);
    }

    [Fact]
    public void R2_AnuncioPausado_SomaEstoqueSemMudarEstado()
    {
        var produto = Apoio.Produto(estoque: 1);
        produto.Pausar();

        produto.Reabastecer(2);

        Assert.Equal(3, produto.Estoque);
        Assert.Equal(EstadoAnuncio.Pausado, produto.Estado);
    }

    [Fact]
    public void R3_AnuncioReservado_EhRecusadoSemAlterarNada()
    {
        var produto = Apoio.Produto(estoque: 2);
        produto.Reservar();

        Assert.Throws<TransicaoInvalidaException>(() => produto.Reabastecer(1));
        Assert.Equal(2, produto.Estoque);
        Assert.Equal(EstadoAnuncio.Reservado, produto.Estado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void R4_QuantidadeForaDaFaixa_EhRecusadaSemAlterarNada(int quantidade)
    {
        var produto = Esgotado();

        Assert.Throws<ValidacaoException>(() => produto.Reabastecer(quantidade));
        Assert.Equal(0, produto.Estoque);
        Assert.Equal(EstadoAnuncio.Vendido, produto.Estado);
    }
}
