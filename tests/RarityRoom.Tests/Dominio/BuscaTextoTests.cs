using RarityRoom.Modelos;
using Xunit;

namespace RarityRoom.Tests.Dominio;

public class BuscaTextoTests
{
    [Theory]
    [InlineData("Colecionáveis", "colecionaveis")]
    [InlineData("  FUNKO  ", "funko")]
    [InlineData("Ação", "acao")]
    [InlineData("ÁÉÍÓÚ", "aeiou")]
    public void Normalizar_DeveRemoverAcentoECaixa(string entrada, string esperado)
    {
        Assert.Equal(esperado, BuscaTexto.Normalizar(entrada));
    }

    [Fact]
    public void Contem_DeveIgnorarAcento()
    {
        Assert.True(BuscaTexto.Contem("Colecionáveis", "colecionaveis"));
        Assert.True(BuscaTexto.Contem("Funko Batman", "batman"));
        Assert.False(BuscaTexto.Contem("Funko Batman", "robin"));
    }

    [Fact]
    public void Igual_DeveIgnorarCaixaAcentoEEspacos()
    {
        Assert.True(BuscaTexto.Igual(" Colecionáveis ", "colecionaveis"));
        Assert.False(BuscaTexto.Igual("Funko", "colecionaveis"));
    }
}
