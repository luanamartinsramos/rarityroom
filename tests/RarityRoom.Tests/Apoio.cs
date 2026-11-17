using RarityRoom.Modelos;
using RarityRoom.Repositorios;
using RarityRoom.Modelos.Enums;

namespace RarityRoom.Tests;

/// <summary>
/// Fábrica de dados válidos. Cada teste altera só o campo que deseja
/// invalidar, deixando claro o que está sendo exercitado.
/// </summary>
internal static class Apoio
{
    public static DadosAnuncio Dados(
        string nome = "Funko Batman",
        string descricao = "Funko original",
        decimal preco = 149.90m,
        int estoque = 2,
        string categoria = "Funko",
        string marca = "Funko",
        string colecao = "DC",
        string condicao = "Novo",
        string imagemUrl = "https://exemplo.com/funko.jpg",
        ModalidadeNegociacao modalidade = ModalidadeNegociacao.PrecoFixo,
        string franquia = "DC",
        Raridade raridade = Raridade.Comum,
        bool destaque = false)
    {
        return new DadosAnuncio(
            nome,
            descricao,
            preco,
            estoque,
            categoria,
            marca,
            colecao,
            condicao,
            imagemUrl,
            modalidade,
            franquia,
            raridade,
            false,
            destaque);
    }

    /// <summary>
    /// Usuários com perfil "Ambos" (compram e vendem), para os testes que não
    /// estão exercitando o papel da conta.
    /// </summary>
    public static RepositorioUsuarioMemoria Usuarios()
    {
        var usuarios = new RepositorioUsuarioMemoria();

        usuarios.Adicionar(new Usuario("Ana Lima", "ana@teste.com", "sal:hash"));
        usuarios.Adicionar(new Usuario("Bruno Sa", "bruno@teste.com", "sal:hash"));
        usuarios.Adicionar(new Usuario("Carlos Dias", "carlos@teste.com", "sal:hash"));
        usuarios.Adicionar(new Usuario("Dora Lima", "dora@teste.com", "sal:hash"));
        usuarios.Adicionar(new Usuario("Edu Sa", "edu@teste.com", "sal:hash"));

        return usuarios;
    }

    public static DadosEntrega Entrega()
    {
        return DadosEntrega.Criar(
            "01310-100",
            "Avenida Paulista",
            "1000",
            "Apto 12",
            "Bela Vista",
            "São Paulo",
            "SP");
    }

    public static Produto Produto(
        int proprietarioId = 1,
        decimal preco = 149.90m,
        int estoque = 2)
    {
        return new Produto(
            proprietarioId,
            "Funko Batman",
            "Funko original",
            preco,
            estoque,
            "Funko",
            "Funko",
            "DC",
            "Novo",
            string.Empty);
    }
}
