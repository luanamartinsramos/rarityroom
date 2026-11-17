using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using Xunit;

namespace RarityRoom.Tests.Dominio;

public class UsuarioTests
{
    private const string HashValido = "c2FsdA==:aGFzaA==";

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void Construtor_NomeInvalido_DeveFalhar(string nome)
    {
        Assert.Throws<ValidacaoException>(
            () => new Usuario(nome, "ana@teste.com", HashValido));
    }

    [Theory]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("ana@teste")]
    [InlineData("ana teste@teste.com")]
    [InlineData("ana@@teste.com")]
    public void Construtor_EmailInvalido_DeveFalhar(string email)
    {
        Assert.Throws<ValidacaoException>(
            () => new Usuario("Ana Lima", email, HashValido));
    }

    [Fact]
    public void Construtor_HashSemSeparador_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => new Usuario("Ana Lima", "ana@teste.com", "hashsolto"));
    }

    [Fact]
    public void AlterarNome_Invalido_DeveFalhar()
    {
        var usuario = new Usuario("Ana Lima", "ana@teste.com", HashValido);

        Assert.Throws<ValidacaoException>(() => usuario.AlterarNome("ab"));
        Assert.Equal("Ana Lima", usuario.Nome);
    }

    [Fact]
    public void EhProprietario_DeveCompararIdentificador()
    {
        var usuario = new Usuario("Ana Lima", "ana@teste.com", HashValido);

        Assert.False(usuario.EhProprietario(0));
        Assert.False(usuario.EhProprietario(99));
    }

    [Fact]
    public void ValidarSenha_CurtaDeveFalhar()
    {
        Assert.Throws<ValidacaoException>(() => Usuario.ValidarSenha("123"));
    }

    [Fact]
    public void ValidarSenha_ValidaNaoDeveFalhar()
    {
        Usuario.ValidarSenha("senha123");
    }
}
