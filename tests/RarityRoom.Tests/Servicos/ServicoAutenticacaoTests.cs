using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Servicos;

public class ServicoAutenticacaoTests
{
    private static ServicoAutenticacao Criar()
    {
        return new ServicoAutenticacao(
            new RepositorioUsuarioMemoria(),
            new HashSenha());
    }

    [Fact]
    public void Cadastrar_DeveGerarIdsSequenciais()
    {
        var servico = Criar();

        var primeira = servico.Cadastrar("Ana Lima", "ana@teste.com", "senha123");
        var segundo = servico.Cadastrar("Bruno Sa", "bruno@teste.com", "senha123");

        Assert.Equal(1, primeira.Id);
        Assert.Equal(2, segundo.Id);
    }

    [Fact]
    public void Cadastrar_EmailDuplicado_DeveConflitar()
    {
        var servico = Criar();
        servico.Cadastrar("Ana Lima", "ana@teste.com", "senha123");

        var excecao = Assert.Throws<ConflitoException>(
            () => servico.Cadastrar("Outra Ana", "ana@teste.com", "senha123"));

        Assert.Equal("conflito", excecao.Codigo);
    }

    [Fact]
    public void Cadastrar_EmailDuplicadoComCaixaDiferente_DeveConflitar()
    {
        var servico = Criar();
        servico.Cadastrar("Ana Lima", "ana@teste.com", "senha123");

        Assert.Throws<ConflitoException>(
            () => servico.Cadastrar("Outra Ana", "ANA@Teste.com", "senha123"));
    }

    [Fact]
    public void Cadastrar_SenhaCurta_DeveFalhar()
    {
        var servico = Criar();

        Assert.Throws<ValidacaoException>(
            () => servico.Cadastrar("Ana Lima", "ana@teste.com", "123"));
    }

    [Fact]
    public void Cadastrar_NaoDeveGuardarSenhaEmTextoPuro()
    {
        var servico = Criar();

        var usuario = servico.Cadastrar("Ana Lima", "ana@teste.com", "senha123");

        Assert.NotEqual("senha123", usuario.SenhaHash);
        Assert.Contains(':', usuario.SenhaHash);
    }

    [Fact]
    public void Entrar_ComSenhaCorreta_DeveRetornarUsuario()
    {
        var servico = Criar();
        servico.Cadastrar("Ana Lima", "ana@teste.com", "senha123");

        var usuario = servico.Entrar("ana@teste.com", "senha123");

        Assert.NotNull(usuario);
        Assert.Equal("Ana Lima", usuario!.Nome);
    }

    [Fact]
    public void Entrar_ComSenhaErrada_DeveRetornarNulo()
    {
        var servico = Criar();
        servico.Cadastrar("Ana Lima", "ana@teste.com", "senha123");

        Assert.Null(servico.Entrar("ana@teste.com", "senhaerrada"));
    }

    [Fact]
    public void Entrar_EmailInexistente_DeveRetornarNulo()
    {
        var servico = Criar();

        Assert.Null(servico.Entrar("ninguem@teste.com", "senha123"));
    }

    [Fact]
    public void Entrar_UsuarioDesativado_DeveRetornarNulo()
    {
        var servico = Criar();
        var usuario = servico.Cadastrar("Ana Lima", "ana@teste.com", "senha123");
        usuario.Desativar();

        Assert.Null(servico.Entrar("ana@teste.com", "senha123"));
    }
}
