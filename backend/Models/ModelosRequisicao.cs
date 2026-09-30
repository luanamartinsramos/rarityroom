using System.ComponentModel.DataAnnotations;

namespace RarityRoom.Modelos;

public class ModeloCadastro
{
    [Required]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Senha { get; set; } = string.Empty;
}

public class ModeloLogin
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}

public class ModeloProduto
{
    [Required]
    public string Nome { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Preco { get; set; }

    [Range(0, int.MaxValue)]
    public int Estoque { get; set; }

    [Required]
    public string Categoria { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Colecao { get; set; } = string.Empty;

    public string Condicao { get; set; } = string.Empty;

    public string ImagemUrl { get; set; } = string.Empty;
}