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

/// <summary>
/// Checkout a partir do carrinho do usuário autenticado. Não há produto,
/// preço ou quantidade aqui de propósito: tudo é lido do carrinho e dos
/// anúncios no servidor, então o navegador não consegue manipular valores.
/// </summary>
public class ModeloCheckout
{
    public string Cep { get; set; } = string.Empty;

    public string Logradouro { get; set; } = string.Empty;

    public string Numero { get; set; } = string.Empty;

    public string Complemento { get; set; } = string.Empty;

    public string Bairro { get; set; } = string.Empty;

    public string Cidade { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    /// <summary>Pix, CartaoCredito ou Boleto.</summary>
    public string FormaPagamento { get; set; } = string.Empty;

    /// <summary>
    /// Identificador da tentativa gerado pelo cliente. Reenviar a mesma chave
    /// devolve a compra já criada em vez de duplicá-la.
    /// </summary>
    public string ChaveIdempotencia { get; set; } = string.Empty;
}

public class ModeloItemCarrinho
{
    [Required]
    public int ProdutoId { get; set; }

    public int Quantidade { get; set; } = 1;
}

public class ModeloQuantidade
{
    public int Quantidade { get; set; }
}

/// <summary>
/// Dados do cartão da simulação. Trafegam só para validação: nada além dos
/// 4 últimos dígitos e da bandeira é gravado.
/// </summary>
public class ModeloCartao
{
    public string NomeImpresso { get; set; } = string.Empty;

    public string Numero { get; set; } = string.Empty;

    /// <summary>MM/AA.</summary>
    public string Validade { get; set; } = string.Empty;

    public string Cvv { get; set; } = string.Empty;

    public string Cpf { get; set; } = string.Empty;

    public int Parcelas { get; set; } = 1;
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

public class ModeloAvaliacao
{
    [Required]
    public int PedidoId { get; set; }

    [Required]
    public int Nota { get; set; }

    public string Comentario { get; set; } = string.Empty;

    /// <summary>Item do pedido avaliado. Opcional: zero avalia o primeiro.</summary>
    public int ProdutoId { get; set; }
}
