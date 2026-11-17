using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

public class FavoritoTests
{
    [Fact]
    public void Construtor_QuandoAnuncioProprio_DeveFalhar()
    {
        var excecao = Assert.Throws<ValidacaoException>(
            () => new Favorito(usuarioId: 3, produtoId: 10, proprietarioProdutoId: 3));

        Assert.Equal("dados-invalidos", excecao.Codigo);
    }

    [Fact]
    public void Construtor_QuandoAnuncioDeOutro_DeveCriar()
    {
        var favorito = new Favorito(
            usuarioId: 3,
            produtoId: 10,
            proprietarioProdutoId: 7);

        Assert.Equal(3, favorito.UsuarioId);
        Assert.Equal(10, favorito.ProdutoId);
    }

    [Fact]
    public void Construtor_SemUsuario_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => new Favorito(0, 10, 7));
    }
}
