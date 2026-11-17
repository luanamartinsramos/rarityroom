using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

/// <summary>
/// Regras de formação do anúncio. Estas regras vivem na entidade: chamar a
/// entidade diretamente precisa ser suficiente para barrar dados inválidos.
/// </summary>
public class ProdutoValidacaoTests
{
    [Fact]
    public void Construtor_NomeVazio_DeveFalhar()
    {
        var excecao = Assert.Throws<ValidacaoException>(
            () => new Produto(
                1, "   ", string.Empty, 10m, 1, "Funko",
                string.Empty, string.Empty, string.Empty, string.Empty));

        Assert.Equal("dados-invalidos", excecao.Codigo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Construtor_PrecoNaoPositivo_DeveFalhar(decimal preco)
    {
        Assert.Throws<ValidacaoException>(
            () => new Produto(
                1, "Funko Batman", "", preco, 1, "Funko",
                "", "", "", string.Empty));
    }

    [Fact]
    public void Construtor_EstoqueNegativo_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => Apoio.Produto(estoque: -1));
    }

    [Fact]
    public void Construtor_CategoriaVazia_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => new Produto(
                1, "Funko Batman", "", 10m, 1, "  ",
                "", "", "", string.Empty));
    }

    [Fact]
    public void Construtor_ImagemUrlInvalida_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => new Produto(
                1, "Funko Batman", "", 10m, 1, "Funko",
                "", "", "", "nao-e-url"));
    }

    [Fact]
    public void Construtor_SemProprietario_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => Apoio.Produto(proprietarioId: 0));
    }

    [Fact]
    public void Construtor_DeveRemoverEspacosDasExtremidades()
    {
        var produto = new Produto(
            1, "  Funko Batman  ", "  descricao  ", 10m, 1, " Funko ",
            " Funko ", " DC ", " Novo ", "  ");

        Assert.Equal("Funko Batman", produto.Nome);
        Assert.Equal("descricao", produto.Descricao);
        Assert.Equal("Funko", produto.Categoria);
        Assert.Equal(string.Empty, produto.ImagemUrl);
    }

    [Fact]
    public void Construtor_DeveIniciarDisponivel()
    {
        var produto = Apoio.Produto();

        Assert.Equal(0, produto.Id);
        Assert.True(produto.Disponivel);
        Assert.Equal(149.90m * 2, produto.ValorTotalEmEstoque);
    }
}
