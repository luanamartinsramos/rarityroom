using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Servicos;

public class ServicoAnunciosTests
{
    private static ServicoAnuncios Criar(out RepositorioProdutoMemoria repositorio)
    {
        repositorio = new RepositorioProdutoMemoria();

        return new ServicoAnuncios(repositorio, CriarCategorias(), Apoio.Usuarios());
    }

    private static RepositorioCategoriaMemoria CriarCategorias()
    {
        var categorias = new RepositorioCategoriaMemoria();

        categorias.Adicionar(new Categoria("Funko"));

        return categorias;
    }

    [Fact]
    public void Criar_DeveVincularProprietario()
    {
        var servico = Criar(out _);

        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 5);

        Assert.Equal(5, produto.UsuarioId);
        Assert.Equal(1, produto.Id);
    }

    [Fact]
    public void Criar_ComDadosInvalidos_NaoDevePersistir()
    {
        var servico = Criar(out var repositorio);

        Assert.Throws<ValidacaoException>(
            () => servico.Criar(Apoio.Dados(preco: 0m), proprietarioId: 1));

        Assert.Empty(repositorio.ObterTodos());
    }

    [Fact]
    public void Atualizar_PorOutroUsuario_DeveNegar()
    {
        var servico = Criar(out _);
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        Assert.Throws<AcessoNegadoException>(
            () => servico.Atualizar(
                produto.Id,
                Apoio.Dados(nome: "Invadido"),
                solicitanteId: 2));
    }

    [Fact]
    public void Atualizar_PorOutroUsuario_NaoDeveAlterarDados()
    {
        var servico = Criar(out _);
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        Assert.Throws<AcessoNegadoException>(
            () => servico.Atualizar(
                produto.Id,
                Apoio.Dados(nome: "Invadido"),
                solicitanteId: 2));

        Assert.Equal("Funko Batman", produto.Nome);
    }

    [Fact]
    public void Atualizar_PeloDono_DeveAlterar()
    {
        var servico = Criar(out _);
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        var atualizado = servico.Atualizar(
            produto.Id,
            Apoio.Dados(nome: "Funko Robin", preco: 99.50m),
            solicitanteId: 1);

        Assert.Equal("Funko Robin", atualizado.Nome);
        Assert.Equal(99.50m, atualizado.Preco);
    }

    [Fact]
    public void Remover_PorOutroUsuario_DeveNegar()
    {
        var servico = Criar(out var repositorio);
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        Assert.Throws<AcessoNegadoException>(
            () => servico.Remover(produto.Id, solicitanteId: 2));

        Assert.Single(repositorio.ObterTodos());
    }

    [Fact]
    public void Remover_PeloDono_DeveExcluir()
    {
        var servico = Criar(out var repositorio);
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        servico.Remover(produto.Id, solicitanteId: 1);

        Assert.Empty(repositorio.ObterTodos());
    }

    [Fact]
    public void ObterPorId_Inexistente_DeveLancarNaoEncontrado()
    {
        var servico = Criar(out _);

        var excecao = Assert.Throws<NaoEncontradoException>(
            () => servico.ObterPorId(42));

        Assert.Equal("nao-encontrado", excecao.Codigo);
    }

    [Fact]
    public void AlterarEstado_PorOutroUsuario_DeveNegar()
    {
        var servico = Criar(out _);
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        Assert.Throws<AcessoNegadoException>(
            () => servico.AlterarEstado(
                produto.Id,
                OperacaoAnuncio.Reservar,
                solicitanteId: 2));
    }

    [Fact]
    public void AlterarEstado_VenderAnuncioPausado_DeveFalhar()
    {
        var servico = Criar(out _);
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        servico.AlterarEstado(produto.Id, OperacaoAnuncio.Pausar, 1);

        Assert.Throws<TransicaoInvalidaException>(
            () => servico.AlterarEstado(produto.Id, OperacaoAnuncio.Vender, 1));
    }

    [Fact]
    public void AlterarEstado_Reservar_DeveAtualizarEstadoPersistido()
    {
        var servico = Criar(out _);
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        servico.AlterarEstado(produto.Id, OperacaoAnuncio.Reservar, 1);

        Assert.Equal(
            EstadoAnuncio.Reservado,
            servico.ObterPorId(produto.Id).Estado);
    }
}
