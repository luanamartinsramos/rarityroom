using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Linha do pedido. É um snapshot do anúncio no momento da compra: nome,
/// imagem e preço ficam congelados, mesmo que o vendedor edite o anúncio
/// depois.
/// </summary>
public class ItemPedido
{
    public int Id { get; private set; }

    public int ProdutoId { get; private set; }

    public string NomeProduto { get; private set; } = string.Empty;

    public string ImagemUrl { get; private set; } = string.Empty;

    public decimal PrecoUnitario { get; private set; }

    public int Quantidade { get; private set; }

    public decimal Subtotal => PrecoUnitario * Quantidade;

    private ItemPedido()
    {
    }

    /// <param name="precoUnitario">
    /// Preço decidido pelo servidor (preço do anúncio ou de proposta aceita),
    /// nunca o valor enviado pelo navegador.
    /// </param>
    public ItemPedido(Produto produto, int quantidade, decimal precoUnitario)
    {
        ArgumentNullException.ThrowIfNull(produto);

        if (quantidade <= 0)
        {
            throw new ValidacaoException("A quantidade deve ser maior que zero.");
        }

        if (precoUnitario <= 0)
        {
            throw new ValidacaoException("O preço do item deve ser maior que zero.");
        }

        ProdutoId = produto.Id;
        NomeProduto = produto.Nome;
        ImagemUrl = produto.ImagemUrl;
        PrecoUnitario = Math.Round(precoUnitario, 2);
        Quantidade = quantidade;
    }
}
