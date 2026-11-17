using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Repositorios;
using Xunit;

namespace RarityRoom.Tests.Repositorios;

/// <summary>
/// Persistência real (EF Core + SQLite em memória), com o mesmo mapeamento da
/// produção. Os dublês em memória não cobrem esta camada.
/// </summary>
public class RepositorioEfTests : IDisposable
{
    private readonly SqliteConnection conexao;

    private readonly RarityRoomContexto contexto;

    public RepositorioEfTests()
    {
        conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();

        var opcoes = new DbContextOptionsBuilder<RarityRoomContexto>()
            .UseSqlite(conexao)
            .Options;

        contexto = new RarityRoomContexto(opcoes);
        contexto.Database.EnsureCreated();
    }

    public void Dispose()
    {
        contexto.Dispose();
        conexao.Dispose();
    }

    [Fact]
    public void Adicionar_DevePersistirEAtribuirId()
    {
        var repositorio = new RepositorioProdutoEf(contexto);

        var produto = repositorio.Adicionar(Apoio.Produto());

        Assert.True(produto.Id > 0);
        Assert.NotNull(repositorio.ObterPorId(produto.Id));
    }

    /// <summary>
    /// Regressão do defeito original: excluir o último anúncio e criar outro
    /// não pode reaproveitar o identificador excluído.
    /// </summary>
    [Fact]
    public void Adicionar_AposExcluirUltimo_NaoDeveReutilizarId()
    {
        var repositorio = new RepositorioProdutoEf(contexto);

        repositorio.Adicionar(Apoio.Produto());
        repositorio.Adicionar(Apoio.Produto());
        var terceiro = repositorio.Adicionar(Apoio.Produto());

        repositorio.Remover(terceiro.Id);

        var novo = repositorio.Adicionar(Apoio.Produto());

        Assert.Equal(4, novo.Id);
        Assert.Equal(3, repositorio.ObterTodos().Count);
    }

    [Fact]
    public void ObterTodos_DeveDevolverListaIndependente()
    {
        var repositorio = new RepositorioProdutoEf(contexto);
        repositorio.Adicionar(Apoio.Produto());

        var lista = repositorio.ObterTodos();
        lista.Clear();

        Assert.Single(repositorio.ObterTodos());
    }

    [Fact]
    public void Atualizar_DevePersistirAlteracao()
    {
        var repositorio = new RepositorioProdutoEf(contexto);
        var produto = repositorio.Adicionar(Apoio.Produto());

        produto.Atualizar(
            "Funko Robin", "novo", 99.90m, 1, "Funko",
            string.Empty, string.Empty, string.Empty, string.Empty);

        Assert.True(repositorio.Atualizar(produto));

        contexto.ChangeTracker.Clear();

        var recarregado = repositorio.ObterPorId(produto.Id);

        Assert.Equal("Funko Robin", recarregado!.Nome);
        Assert.Equal(99.90m, recarregado.Preco);
    }

    [Fact]
    public void Remover_Inexistente_DeveRetornarFalso()
    {
        var repositorio = new RepositorioProdutoEf(contexto);

        Assert.False(repositorio.Remover(99));
    }

    [Fact]
    public void Usuario_DeveSerBuscadoIgnorandoCaixa()
    {
        var repositorio = new RepositorioUsuarioEf(contexto);
        repositorio.Adicionar(new Usuario("Ana Lima", "ana@teste.com", "sal:hash"));

        var encontrado = repositorio.ObterPorEmail("ANA@Teste.COM");

        Assert.NotNull(encontrado);
    }

    [Fact]
    public void Categoria_DeveSerBuscadaIgnorandoAcentoECaixa()
    {
        var repositorio = new RepositorioCategoriaEf(contexto);
        repositorio.Adicionar(new Categoria("Colecionáveis"));

        Assert.NotNull(repositorio.ObterPorNome("colecionaveis"));
    }

    [Fact]
    public void Favorito_NaoDevePermitirDuplicataNoBanco()
    {
        var produtos = new RepositorioProdutoEf(contexto);
        var favoritos = new RepositorioFavoritoEf(contexto);

        var produto = produtos.Adicionar(Apoio.Produto());

        favoritos.Adicionar(new Favorito(2, produto.Id, produto.UsuarioId));

        Assert.Throws<DbUpdateException>(
            () => favoritos.Adicionar(new Favorito(2, produto.Id, produto.UsuarioId)));
    }

    [Fact]
    public void Proposta_DevePersistirEstadoAceito()
    {
        var repositorio = new RepositorioPropostaEf(contexto);
        var proposta = repositorio.Adicionar(new Proposta(1, 2, 100m, "oi"));

        proposta.Aceitar();
        repositorio.Atualizar(proposta);

        contexto.ChangeTracker.Clear();

        Assert.Equal(
            EstadoProposta.Aceita,
            repositorio.ObterPorId(proposta.Id)!.Estado);
    }
}
