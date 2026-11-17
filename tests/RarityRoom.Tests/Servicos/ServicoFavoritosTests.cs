using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Servicos;

public class ServicoFavoritosTests
{
    private static (
        ServicoFavoritos favoritos,
        ServicoAnuncios anuncios) Criar()
    {
        var produtos = new RepositorioProdutoMemoria();
        var categorias = new RepositorioCategoriaMemoria();

        categorias.Adicionar(new Categoria("Funko"));

        return (
            new ServicoFavoritos(new RepositorioFavoritoMemoria(), produtos),
            new ServicoAnuncios(produtos, categorias, Apoio.Usuarios()));
    }

    [Fact]
    public void Adicionar_NoProprioAnuncio_DeveFalhar()
    {
        var (favoritos, anuncios) = Criar();
        var anuncio = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);

        Assert.Throws<ValidacaoException>(
            () => favoritos.Adicionar(anuncio.Id, usuarioId: 1));
    }

    [Fact]
    public void Adicionar_Duplicado_DeveFalhar()
    {
        var (favoritos, anuncios) = Criar();
        var anuncio = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);

        favoritos.Adicionar(anuncio.Id, usuarioId: 2);

        Assert.Throws<ConflitoException>(
            () => favoritos.Adicionar(anuncio.Id, usuarioId: 2));
    }

    [Fact]
    public void Listar_DeveRetornarAnunciosFavoritados()
    {
        var (favoritos, anuncios) = Criar();
        var primeiro = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);
        anuncios.Criar(Apoio.Dados(nome: "Katana"), proprietarioId: 1);

        favoritos.Adicionar(primeiro.Id, usuarioId: 2);

        var lista = favoritos.Listar(usuarioId: 2);

        Assert.Single(lista);
        Assert.Equal(primeiro.Id, lista[0].Id);
    }

    [Fact]
    public void Remover_NaoFavoritado_DeveFalhar()
    {
        var (favoritos, _) = Criar();

        Assert.Throws<NaoEncontradoException>(
            () => favoritos.Remover(99, usuarioId: 2));
    }

    [Fact]
    public void Remover_DeveEsvaziarLista()
    {
        var (favoritos, anuncios) = Criar();
        var anuncio = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);

        favoritos.Adicionar(anuncio.Id, usuarioId: 2);
        favoritos.Remover(anuncio.Id, usuarioId: 2);

        Assert.Empty(favoritos.Listar(usuarioId: 2));
    }
}
