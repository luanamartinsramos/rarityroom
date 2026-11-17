using System.ComponentModel.DataAnnotations;

namespace RarityRoom.Modelos;

/// <summary>
/// Modelos de entrada da API. Aqui só existe o mínimo para o binding não
/// receber nulo: toda regra de negócio (tamanho, faixa, formato) é aplicada
/// pelo domínio, que é a única autoridade. Duplicar as regras aqui faria as
/// duas cópias divergirem com o tempo.
/// </summary>
public class ModeloCadastro
{
    [Required]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;

}

public class ModeloLogin
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}

public class ModeloProduto
{
    [Required]
    public string Nome { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int Estoque { get; set; }

    [Required]
    public string Categoria { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Colecao { get; set; } = string.Empty;

    public string Condicao { get; set; } = string.Empty;

    public string ImagemUrl { get; set; } = string.Empty;

    /// <summary>
    /// PrecoFixo, AceitaOferta ou Liquidacao. Vazio assume PrecoFixo.
    /// </summary>
    public string Modalidade { get; set; } = string.Empty;

    public string Franquia { get; set; } = string.Empty;

    /// <summary>Comum, Raro, Epico ou Lendario.</summary>
    public string Raridade { get; set; } = string.Empty;

    public bool EdicaoLimitada { get; set; }

    public bool Destaque { get; set; }
}

public class ModeloEstadoAnuncio
{
    [Required]
    public string Operacao { get; set; } = string.Empty;
}

public class ModeloCategoria
{
    [Required]
    public string Nome { get; set; } = string.Empty;
}

public class ModeloProposta
{
    [Required]
    public int ProdutoId { get; set; }

    [Required]
    public decimal Valor { get; set; }

    public string Mensagem { get; set; } = string.Empty;
}

public class ModeloPedido
{
    [Required]
    public int ProdutoId { get; set; }

    public string Cep { get; set; } = string.Empty;

    public string Logradouro { get; set; } = string.Empty;

    public string Numero { get; set; } = string.Empty;

    public string Complemento { get; set; } = string.Empty;

    public string Bairro { get; set; } = string.Empty;

    public string Cidade { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    /// <summary>Pix, CartaoCredito ou Boleto.</summary>
    public string FormaPagamento { get; set; } = string.Empty;
}

public class ModeloNome
{
    [Required]
    public string Nome { get; set; } = string.Empty;
}

public class ModeloEmail
{
    [Required]
    public string Email { get; set; } = string.Empty;
}

public class ModeloEndereco
{
    [Required]
    public string Cep { get; set; } = string.Empty;

    [Required]
    public string Logradouro { get; set; } = string.Empty;

    [Required]
    public string Numero { get; set; } = string.Empty;

    public string Complemento { get; set; } = string.Empty;

    [Required]
    public string Bairro { get; set; } = string.Empty;

    [Required]
    public string Cidade { get; set; } = string.Empty;

    [Required]
    public string Estado { get; set; } = string.Empty;

    /// <summary>Valida com a mesma regra usada no checkout.</summary>
    public DadosEntrega ParaEntrega()
    {
        return DadosEntrega.Criar(
            Cep,
            Logradouro,
            Numero,
            Complemento,
            Bairro,
            Cidade,
            Estado);
    }
}

public class ModeloRecuperacao
{
    [Required]
    public string Email { get; set; } = string.Empty;
}

public class ModeloRedefinicao
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string NovaSenha { get; set; } = string.Empty;
}

public class ModeloLoja
{
    [Required]
    public string Nome { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;
}

public class ModeloPagamento
{
    /// <summary>Resultado da simulação de pagamento.</summary>
    public bool Aprovado { get; set; } = true;
}

public class ModeloAvaliacao
{
    [Required]
    public int PedidoId { get; set; }

    [Required]
    public int Nota { get; set; }

    public string Comentario { get; set; } = string.Empty;
}
