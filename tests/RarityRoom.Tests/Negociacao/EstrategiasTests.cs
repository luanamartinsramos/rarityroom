using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Negociacao;
using Xunit;

namespace RarityRoom.Tests.Negociacao;

/// <summary>
/// Prova de que o polimorfismo é usado de verdade: mesma oferta, mesmo preço,
/// resultado diferente conforme a estratégia escolhida.
/// </summary>
public class EstrategiasTests
{
    private const decimal Preco = 249.90m;

    private const decimal Oferta = 200m;

    [Fact]
    public void OfertaDe200EmPrecoDe249_90_DeveVariarPorModalidade()
    {
        Assert.False(new EstrategiaPrecoFixo().Aceita(Preco, Oferta));
        Assert.False(new EstrategiaAceitaOferta().Aceita(Preco, Oferta));
        Assert.True(new EstrategiaLiquidacao().Aceita(Preco, Oferta));
    }

    [Fact]
    public void OfertaNoLimiteExato_DeveSerAceitaPorAceitaOferta()
    {
        Assert.False(new EstrategiaAceitaOferta().Aceita(Preco, 212.41m));
        Assert.True(new EstrategiaAceitaOferta().Aceita(Preco, 212.42m));
    }

    [Theory]
    [InlineData(ModalidadeNegociacao.PrecoFixo, 249.90)]
    [InlineData(ModalidadeNegociacao.AceitaOferta, 212.42)]
    [InlineData(ModalidadeNegociacao.Liquidacao, 174.93)]
    public void ValorMinimo_DeveRespeitarCadaModalidade(
        ModalidadeNegociacao modalidade,
        decimal minimoEsperado)
    {
        IEstrategiaNegociacao estrategia = modalidade switch
        {
            ModalidadeNegociacao.PrecoFixo => new EstrategiaPrecoFixo(),
            ModalidadeNegociacao.AceitaOferta => new EstrategiaAceitaOferta(),
            _ => new EstrategiaLiquidacao()
        };

        Assert.Equal(minimoEsperado, estrategia.ValorMinimo(Preco));
        Assert.True(estrategia.Aceita(Preco, minimoEsperado));
        Assert.False(estrategia.Aceita(Preco, minimoEsperado - 0.01m));
    }

    [Fact]
    public void Modalidade_DeveIdentificarCadaEstrategia()
    {
        Assert.Equal(
            ModalidadeNegociacao.PrecoFixo,
            new EstrategiaPrecoFixo().Modalidade);

        Assert.Equal(
            ModalidadeNegociacao.AceitaOferta,
            new EstrategiaAceitaOferta().Modalidade);

        Assert.Equal(
            ModalidadeNegociacao.Liquidacao,
            new EstrategiaLiquidacao().Modalidade);
    }
}
