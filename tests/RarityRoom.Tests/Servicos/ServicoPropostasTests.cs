using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Modelos.Negociacao;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Servicos;

public class ServicoPropostasTests
{
    private static (
        ServicoPropostas propostas,
        ServicoAnuncios anuncios) Criar()
    {
        var produtos = new RepositorioProdutoMemoria();
        var categorias = new RepositorioCategoriaMemoria();

        categorias.Adicionar(new RarityRoom.Modelos.Categoria("Funko"));

        var anuncios = new ServicoAnuncios(produtos, categorias, Apoio.Usuarios());

        var propostas = new ServicoPropostas(
            new RepositorioPropostaMemoria(),
            produtos,
            new IEstrategiaNegociacao[]
            {
                new EstrategiaPrecoFixo(),
                new EstrategiaAceitaOferta(),
                new EstrategiaLiquidacao()
            },
            new Fakes.ServicoNotificacoesFalso());

        return (propostas, anuncios);
    }

    /// <summary>
    /// Mesma oferta (R$ 200) sobre o mesmo preço (R$ 249,90): recusada no
    /// preço fixo e aceita na liquidação. Se o serviço tivesse um if/else por
    /// modalidade, o teste passaria, mas o polimorfismo não estaria em uso —
    /// por isso ele também verifica o efeito no estado do anúncio.
    /// </summary>
    [Fact]
    public void Aceitar_MesmaOferta_DeveVariarPorModalidade()
    {
        var (propostas, anuncios) = Criar();

        var precoFixo = anuncios.Criar(
            Apoio.Dados(preco: 249.90m, modalidade: ModalidadeNegociacao.PrecoFixo),
            proprietarioId: 1);

        var liquidacao = anuncios.Criar(
            Apoio.Dados(preco: 249.90m, modalidade: ModalidadeNegociacao.Liquidacao),
            proprietarioId: 1);

        var propostaPrecoFixo = propostas.Criar(precoFixo.Id, 200m, string.Empty, 2);
        var propostaLiquidacao = propostas.Criar(liquidacao.Id, 200m, string.Empty, 2);

        Assert.Throws<ConflitoException>(
            () => propostas.Aceitar(propostaPrecoFixo.Id, solicitanteId: 1));

        var aceita = propostas.Aceitar(propostaLiquidacao.Id, solicitanteId: 1);

        Assert.Equal(EstadoProposta.Aceita, aceita.Estado);
        Assert.Equal(EstadoAnuncio.Reservado, anuncios.ObterPorId(liquidacao.Id).Estado);
        Assert.Equal(EstadoAnuncio.Disponivel, anuncios.ObterPorId(precoFixo.Id).Estado);
    }

    [Fact]
    public void Criar_NoProprioAnuncio_DeveFalhar()
    {
        var (propostas, anuncios) = Criar();
        var anuncio = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);

        var excecao = Assert.Throws<ValidacaoException>(
            () => propostas.Criar(anuncio.Id, 100m, string.Empty, proponenteId: 1));

        Assert.Equal("dados-invalidos", excecao.Codigo);
    }

    [Fact]
    public void Criar_EmAnuncioIndisponivel_DeveFalhar()
    {
        var (propostas, anuncios) = Criar();
        var anuncio = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);

        anuncios.AlterarEstado(anuncio.Id, OperacaoAnuncio.Pausar, solicitanteId: 1);

        Assert.Throws<ConflitoException>(
            () => propostas.Criar(anuncio.Id, 100m, string.Empty, proponenteId: 2));
    }

    [Fact]
    public void Aceitar_PorTerceiro_DeveNegar()
    {
        var (propostas, anuncios) = Criar();
        var anuncio = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);
        var proposta = propostas.Criar(anuncio.Id, 149.90m, string.Empty, 2);

        Assert.Throws<AcessoNegadoException>(
            () => propostas.Aceitar(proposta.Id, solicitanteId: 3));
    }

    [Fact]
    public void Aceitar_DeveRecusarAsDemaisPendentes()
    {
        var (propostas, anuncios) = Criar();
        var anuncio = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);

        var primeira = propostas.Criar(anuncio.Id, 149.90m, string.Empty, 2);
        var segunda = propostas.Criar(anuncio.Id, 149.90m, string.Empty, 3);

        propostas.Aceitar(primeira.Id, solicitanteId: 1);

        var restantes = propostas.Listar(usuarioId: 1);

        Assert.Equal(
            EstadoProposta.Aceita,
            restantes.Single(p => p.Id == primeira.Id).Estado);

        Assert.Equal(
            EstadoProposta.Recusada,
            restantes.Single(p => p.Id == segunda.Id).Estado);
    }

    [Fact]
    public void Cancelar_PorQuemNaoEnviou_DeveNegar()
    {
        var (propostas, anuncios) = Criar();
        var anuncio = anuncios.Criar(Apoio.Dados(), proprietarioId: 1);
        var proposta = propostas.Criar(anuncio.Id, 100m, string.Empty, 2);

        Assert.Throws<AcessoNegadoException>(
            () => propostas.Cancelar(proposta.Id, solicitanteId: 3));
    }
}
