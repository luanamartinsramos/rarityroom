using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Servicos;

/// <summary>
/// Regra de catálogo: o anúncio só usa categoria existente e grava o nome
/// canônico, não o texto digitado.
/// </summary>
public class ServicoAnunciosCategoriaTests
{
    private static ServicoAnuncios Criar()
    {
        var categorias = new RepositorioCategoriaMemoria();

        categorias.Adicionar(new Categoria("Funko"));

        return new ServicoAnuncios(
            new RepositorioProdutoMemoria(),
            categorias,
            Apoio.Usuarios());
    }

    [Fact]
    public void Criar_ComCategoriaNaoCadastrada_DeveFalhar()
    {
        var servico = Criar();

        var excecao = Assert.Throws<ValidacaoException>(
            () => servico.Criar(Apoio.Dados(categoria: "Invocação"), proprietarioId: 1));

        Assert.Equal("dados-invalidos", excecao.Codigo);
    }

    [Fact]
    public void Criar_DeveGravarNomeCanonicoDaCategoria()
    {
        var servico = Criar();

        var produto = servico.Criar(
            Apoio.Dados(categoria: "  funko  "),
            proprietarioId: 1);

        Assert.Equal("Funko", produto.Categoria);
    }

    [Fact]
    public void Atualizar_ParaCategoriaInexistente_DeveFalhar()
    {
        var servico = Criar();
        var produto = servico.Criar(Apoio.Dados(), proprietarioId: 1);

        Assert.Throws<ValidacaoException>(
            () => servico.Atualizar(
                produto.Id,
                Apoio.Dados(categoria: "Categoria Fantasma"),
                solicitanteId: 1));

        Assert.Equal("Funko", produto.Categoria);
    }
}
