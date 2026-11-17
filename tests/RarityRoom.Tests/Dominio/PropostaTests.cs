using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

public class PropostaTests
{
    [Fact]
    public void Construtor_DeveIniciarPendente()
    {
        var proposta = new Proposta(1, 2, 100m, "  tenho interesse  ");

        Assert.Equal(EstadoProposta.Pendente, proposta.Estado);
        Assert.Equal("tenho interesse", proposta.Mensagem);
        Assert.True(proposta.EstaPendente);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Construtor_ValorNaoPositivo_DeveFalhar(decimal valor)
    {
        Assert.Throws<ValidacaoException>(
            () => new Proposta(1, 2, valor, string.Empty));
    }

    [Fact]
    public void Aceitar_DeveAlterarEstado()
    {
        var proposta = new Proposta(1, 2, 100m, string.Empty);

        proposta.Aceitar();

        Assert.Equal(EstadoProposta.Aceita, proposta.Estado);
    }

    [Fact]
    public void Aceitar_PropostaJaAceita_DeveFalhar()
    {
        var proposta = new Proposta(1, 2, 100m, string.Empty);
        proposta.Aceitar();

        var excecao = Assert.Throws<TransicaoInvalidaException>(
            () => proposta.Aceitar());

        Assert.Equal("transicao-invalida", excecao.Codigo);
    }

    [Fact]
    public void Cancelar_DeOutroUsuario_DeveNegar()
    {
        var proposta = new Proposta(1, proponenteId: 2, 100m, string.Empty);

        Assert.Throws<AcessoNegadoException>(
            () => proposta.ValidarProponente(3));
    }

    [Fact]
    public void Cancelar_PeloProponente_DevePermitir()
    {
        var proposta = new Proposta(1, proponenteId: 2, 100m, string.Empty);

        proposta.ValidarProponente(2);
        proposta.Cancelar();

        Assert.Equal(EstadoProposta.Cancelada, proposta.Estado);
    }
}
