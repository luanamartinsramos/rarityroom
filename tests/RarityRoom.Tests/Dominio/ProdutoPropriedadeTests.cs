using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

/// <summary>
/// Regressão do furo de autorização: antes não existia proprietário no
/// anúncio e qualquer usuário autenticado editava o anúncio de terceiros.
/// </summary>
public class ProdutoPropriedadeTests
{
    [Fact]
    public void ValidarPropriedade_QuandoEhDono_NaoDeveFalhar()
    {
        var produto = Apoio.Produto(proprietarioId: 7);

        produto.ValidarPropriedade(7);

        Assert.Equal(7, produto.UsuarioId);
    }

    [Fact]
    public void ValidarPropriedade_QuandoNaoEhDono_DeveNegar()
    {
        var produto = Apoio.Produto(proprietarioId: 7);

        var excecao = Assert.Throws<AcessoNegadoException>(
            () => produto.ValidarPropriedade(8));

        Assert.Equal("acesso-negado", excecao.Codigo);
    }

    [Fact]
    public void ValidarPropriedade_SemSessao_DeveNegar()
    {
        var produto = Apoio.Produto(proprietarioId: 7);

        Assert.Throws<AcessoNegadoException>(
            () => produto.ValidarPropriedade(0));
    }

    [Fact]
    public void ValidarExclusao_AnuncioVendido_DeveFalhar()
    {
        var produto = Apoio.Produto(proprietarioId: 1);
        produto.Vender();

        Assert.Throws<ConflitoException>(
            () => produto.ValidarExclusao(1));
    }

    [Fact]
    public void ValidarExclusao_DeOutroUsuario_DeveNegar()
    {
        var produto = Apoio.Produto(proprietarioId: 1);

        Assert.Throws<AcessoNegadoException>(
            () => produto.ValidarExclusao(2));
    }
}
