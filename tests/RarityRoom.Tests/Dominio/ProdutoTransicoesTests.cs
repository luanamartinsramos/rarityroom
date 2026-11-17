using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

/// <summary>
/// Ciclo de vida do anúncio. A transição é decidida pelo par
/// (estado de origem, operação): operações diferentes que chegam ao mesmo
/// destino não podem ser confundidas.
/// </summary>
public class ProdutoTransicoesTests
{
    [Fact]
    public void Reservar_DeDisponivel_DeveReservar()
    {
        var produto = Apoio.Produto();

        produto.Reservar();

        Assert.Equal(EstadoAnuncio.Reservado, produto.Estado);
    }

    [Fact]
    public void Vender_DeDisponivel_DeveVender()
    {
        var produto = Apoio.Produto();

        produto.Vender();

        Assert.Equal(EstadoAnuncio.Vendido, produto.Estado);
    }

    [Fact]
    public void Pausar_DeDisponivel_DevePausar()
    {
        var produto = Apoio.Produto();

        produto.Pausar();

        Assert.Equal(EstadoAnuncio.Pausado, produto.Estado);
    }

    [Fact]
    public void Reativar_DePausado_DeveVoltarParaDisponivel()
    {
        var produto = Apoio.Produto();
        produto.Pausar();

        produto.Reativar();

        Assert.Equal(EstadoAnuncio.Disponivel, produto.Estado);
    }

    [Fact]
    public void CancelarReserva_DeReservado_DeveVoltarParaDisponivel()
    {
        var produto = Apoio.Produto();
        produto.Reservar();

        produto.CancelarReserva();

        Assert.Equal(EstadoAnuncio.Disponivel, produto.Estado);
    }

    [Fact]
    public void Vender_DeReservado_DeveVender()
    {
        var produto = Apoio.Produto();
        produto.Reservar();

        produto.Vender();

        Assert.Equal(EstadoAnuncio.Vendido, produto.Estado);
    }

    /// <summary>
    /// Regressão: quando a tabela era indexada apenas pelo destino,
    /// "Reativar" encontrava a transição Reservado -> Disponivel e cancelava
    /// a reserva em silêncio.
    /// </summary>
    [Fact]
    public void Reativar_DeReservado_NaoDeveCancelarReserva()
    {
        var produto = Apoio.Produto();
        produto.Reservar();

        var excecao = Assert.Throws<TransicaoInvalidaException>(
            () => produto.Reativar());

        Assert.Equal("transicao-invalida", excecao.Codigo);
        Assert.Equal(EstadoAnuncio.Reservado, produto.Estado);
    }

    [Fact]
    public void CancelarReserva_DePausado_DeveFalhar()
    {
        var produto = Apoio.Produto();
        produto.Pausar();

        Assert.Throws<TransicaoInvalidaException>(
            () => produto.CancelarReserva());

        Assert.Equal(EstadoAnuncio.Pausado, produto.Estado);
    }

    [Fact]
    public void Reservar_DeVendido_DeveFalhar()
    {
        var produto = Apoio.Produto();
        produto.Vender();

        Assert.Throws<TransicaoInvalidaException>(
            () => produto.Reservar());
    }

    [Fact]
    public void Vender_SemEstoque_DeveFalhar()
    {
        var produto = Apoio.Produto(estoque: 0);

        Assert.Throws<ConflitoException>(() => produto.Vender());

        Assert.Equal(EstadoAnuncio.Disponivel, produto.Estado);
    }

    [Fact]
    public void Atualizar_AnuncioVendido_DeveFalhar()
    {
        var produto = Apoio.Produto();
        produto.Vender();

        Assert.Throws<ConflitoException>(
            () => produto.Atualizar(
                "Funko Robin", "", 100m, 1, "Funko",
                "", "", "", string.Empty));
    }

    [Fact]
    public void AnuncioPausado_NaoDeveEstarDisponivel()
    {
        var produto = Apoio.Produto();
        produto.Pausar();

        Assert.False(produto.Disponivel);
    }
}
