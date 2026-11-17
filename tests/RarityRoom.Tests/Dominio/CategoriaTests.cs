using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Dominio;

public class CategoriaTests
{
    [Theory]
    [InlineData("Colecionáveis", "colecionaveis")]
    [InlineData("Action Figure", "action-figure")]
    [InlineData("  Funko  ", "funko")]
    [InlineData("DC Comics!", "dc-comics")]
    public void GerarSlug_DeveNormalizar(string entrada, string esperado)
    {
        Assert.Equal(esperado, Categoria.GerarSlug(entrada));
    }

    [Fact]
    public void GerarSlug_DeveIgnorarCaixaEAcento()
    {
        Assert.Equal(
            Categoria.GerarSlug("ANIME"),
            Categoria.GerarSlug("ánimé"));
    }

    [Fact]
    public void Construtor_NomeCurto_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(() => new Categoria("a"));
    }

    [Fact]
    public void Construtor_SemLetrasNemNumeros_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(() => new Categoria("@@@"));
    }

    [Fact]
    public void Servico_NaoDevePermitirCategoriaDuplicada()
    {
        var servico = new ServicoCategorias(new RepositorioCategoriaMemoria());

        servico.Criar("Funko");

        Assert.Throws<ConflitoException>(() => servico.Criar("FUNKO"));
    }

    [Fact]
    public void Servico_DeveCriarCategoriaComSlug()
    {
        var servico = new ServicoCategorias(new RepositorioCategoriaMemoria());

        var categoria = servico.Criar("Colecionáveis");

        Assert.Equal("colecionaveis", categoria.Slug);
        Assert.Single(servico.Listar());
    }
}
