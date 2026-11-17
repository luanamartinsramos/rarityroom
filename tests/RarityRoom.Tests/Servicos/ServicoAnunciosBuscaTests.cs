using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Servicos;

/// <summary>
/// Pesquisa do catálogo. Antes o filtro só existia no navegador e comparava
/// texto cru: "colecionaveis" não encontrava "Colecionáveis", e as categorias
/// da tela não existiam no catálogo.
/// </summary>
public class ServicoAnunciosBuscaTests
{
    private static ServicoAnuncios Criar()
    {
        var produtos = new RepositorioProdutoMemoria();
        var categorias = new RepositorioCategoriaMemoria();

        categorias.Adicionar(new Categoria("Funko"));
        categorias.Adicionar(new Categoria("Colecionáveis"));
        categorias.Adicionar(new Categoria("Anime"));

        var servico = new ServicoAnuncios(produtos, categorias, Apoio.Usuarios());

        servico.Criar(
            Apoio.Dados(
                nome: "Funko Batman",
                categoria: "Funko",
                marca: "Funko",
                colecao: "DC",
                franquia: "DC"),
            1);

        servico.Criar(
            Apoio.Dados(
                nome: "Copo Colecionável",
                categoria: "Colecionáveis",
                marca: "Vidro",
                colecao: "Geek",
                franquia: "Geek"),
            1);

        servico.Criar(
            Apoio.Dados(
                nome: "Katana Demon Slayer",
                categoria: "Anime",
                marca: "Réplica",
                colecao: "Kimetsu",
                franquia: "Kimetsu"),
            2);

        return servico;
    }

    [Fact]
    public void Listar_SemFiltro_DeveRetornarTudo()
    {
        Assert.Equal(3, Criar().Listar().Count);
    }

    [Fact]
    public void Listar_PorTermoSemAcento_DeveAcharPalavraAcentuada()
    {
        var encontrados = Criar().Listar(new FiltroAnuncios(Termo: "colecionavel"));

        Assert.Single(encontrados);
        Assert.Equal("Copo Colecionável", encontrados[0].Nome);
    }

    [Fact]
    public void Listar_PorTermoComAcento_DeveAchar()
    {
        Assert.Single(Criar().Listar(new FiltroAnuncios(Termo: "Colecionável")));
    }

    [Fact]
    public void Listar_PorTermo_DeveBuscarEmMarcaEColecao()
    {
        Assert.Single(Criar().Listar(new FiltroAnuncios(Termo: "kimetsu")));
        Assert.Single(Criar().Listar(new FiltroAnuncios(Termo: "vidro")));
    }

    [Fact]
    public void Listar_PorCategoria_DeveIgnorarCaixaEAcento()
    {
        Assert.Single(Criar().Listar(new FiltroAnuncios(Categoria: "colecionaveis")));
        Assert.Single(Criar().Listar(new FiltroAnuncios(Categoria: "FUNKO")));
    }

    [Fact]
    public void Listar_PorEstado_DeveFiltrar()
    {
        var servico = Criar();
        var produtos = servico.Listar();
        produtos[0].Vender();

        var disponiveis = servico.Listar(
            new FiltroAnuncios(Estado: nameof(EstadoAnuncio.Disponivel)));

        Assert.Equal(2, disponiveis.Count);
    }

    [Fact]
    public void Listar_PorFranquia_DeveFiltrar()
    {
        var encontrados = Criar().Listar(new FiltroAnuncios(Franquia: "dc"));

        Assert.Single(encontrados);
        Assert.Equal("Funko Batman", encontrados[0].Nome);
    }

    [Fact]
    public void Listar_PorFaixaDePreco_DeveFiltrar()
    {
        var servico = Criar();

        var baratos = servico.Listar(
            new FiltroAnuncios(PrecoMaximo: 200m));

        Assert.All(baratos, produto => Assert.True(produto.Preco <= 200m));
    }

    [Fact]
    public void Listar_OrdenadoPorMenorPreco_DeveVirCrescente()
    {
        var precos = Criar()
            .Listar(new FiltroAnuncios(Ordem: "MenorPreco"))
            .Select(produto => produto.Preco)
            .ToList();

        Assert.Equal(precos.OrderBy(preco => preco), precos);
    }

    [Fact]
    public void Listar_OrdenadoPorMaiorPreco_DeveVirDecrescente()
    {
        var precos = Criar()
            .Listar(new FiltroAnuncios(Ordem: "MaiorPreco"))
            .Select(produto => produto.Preco)
            .ToList();

        Assert.Equal(precos.OrderByDescending(preco => preco), precos);
    }

    [Fact]
    public void Listar_SomenteDestaque_DeveTrazerApenasDestacados()
    {
        var produtos = new RepositorioProdutoMemoria();
        var categorias = new RepositorioCategoriaMemoria();
        categorias.Adicionar(new Categoria("Funko"));

        var servico = new ServicoAnuncios(produtos, categorias, Apoio.Usuarios());

        servico.Criar(Apoio.Dados(nome: "Comum", destaque: false), 1);
        servico.Criar(Apoio.Dados(nome: "Destaque", destaque: true), 1);

        var destacados = servico.Listar(new FiltroAnuncios(SomenteDestaque: true));

        Assert.Single(destacados);
        Assert.Equal("Destaque", destacados[0].Nome);
    }

    [Fact]
    public void Listar_TermoInexistente_DeveRetornarVazio()
    {
        Assert.Empty(Criar().Listar(new FiltroAnuncios(Termo: "dragon ball")));
    }
}
