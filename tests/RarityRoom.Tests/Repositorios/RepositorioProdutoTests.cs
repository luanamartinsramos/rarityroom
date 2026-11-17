using RarityRoom.Repositorios;
using Xunit;

namespace RarityRoom.Tests.Repositorios;

public class RepositorioProdutoTests
{
    [Fact]
    public void Adicionar_DeveAtribuirIdsSequenciais()
    {
        var repositorio = new RepositorioProdutoMemoria();

        var primeiro = repositorio.Adicionar(Apoio.Produto());
        var segundo = repositorio.Adicionar(Apoio.Produto());

        Assert.Equal(1, primeiro.Id);
        Assert.Equal(2, segundo.Id);
    }

    /// <summary>
    /// Regressão do defeito de id duplicado: com "Count + 1", excluir um item
    /// fazia o próximo anúncio reaproveitar um id que já existia.
    /// </summary>
    [Fact]
    public void Adicionar_AposExclusao_NaoDeveReutilizarId()
    {
        var repositorio = new RepositorioProdutoMemoria();

        repositorio.Adicionar(Apoio.Produto());
        var segundo = repositorio.Adicionar(Apoio.Produto());
        repositorio.Adicionar(Apoio.Produto());

        repositorio.Remover(segundo.Id);

        var novo = repositorio.Adicionar(Apoio.Produto());

        Assert.Equal(4, novo.Id);
        Assert.Single(repositorio.ObterTodos(), p => p.Id == novo.Id);
        Assert.Equal(3, repositorio.ObterTodos().Count);
    }

    [Fact]
    public void ObterTodos_DeveDevolverCopiaDaLista()
    {
        var repositorio = new RepositorioProdutoMemoria();
        repositorio.Adicionar(Apoio.Produto());

        var lista = repositorio.ObterTodos();
        lista.Clear();

        Assert.Single(repositorio.ObterTodos());
    }

    [Fact]
    public void Remover_Inexistente_DeveRetornarFalso()
    {
        var repositorio = new RepositorioProdutoMemoria();

        Assert.False(repositorio.Remover(99));
    }

    [Fact]
    public void Adicionar_EmParalelo_DeveGerarIdsUnicos()
    {
        var repositorio = new RepositorioProdutoMemoria();

        Parallel.For(0, 200, _ => repositorio.Adicionar(Apoio.Produto()));

        var ids = repositorio.ObterTodos().Select(produto => produto.Id).ToList();

        Assert.Equal(200, ids.Count);
        Assert.Equal(200, ids.Distinct().Count());
    }
}
