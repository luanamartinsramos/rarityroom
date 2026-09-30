namespace RarityRoom.Modelos;

public class Produto
{
    public int Id { get; private set; }

    public string Nome { get; private set; }

    public string Descricao { get; private set; }

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }

    public string Categoria { get; private set; }

    public string Marca { get; private set; }

    public string Colecao { get; private set; }

    public string Condicao { get; private set; }

    public string ImagemUrl { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public bool Disponivel => Estoque > 0;

    public decimal ValorTotalEmEstoque => Preco * Estoque;

    public Produto(
        int id,
        string nome,
        string descricao,
        decimal preco,
        int estoque,
        string categoria,
        string marca,
        string colecao,
        string condicao,
        string imagemUrl)
    {
        Id = id;
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        Estoque = estoque;
        Categoria = categoria;
        Marca = marca;
        Colecao = colecao;
        Condicao = condicao;
        ImagemUrl = imagemUrl;
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(
        string nome,
        string descricao,
        decimal preco,
        int estoque,
        string categoria,
        string marca,
        string colecao,
        string condicao,
        string imagemUrl)
    {
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        Estoque = estoque;
        Categoria = categoria;
        Marca = marca;
        Colecao = colecao;
        Condicao = condicao;
        ImagemUrl = imagemUrl;
    }
}