using RarityRoom.Modelos.Enums;

namespace RarityRoom.Modelos;

/// <summary>Linha do carrinho com os dados atuais do anúncio.</summary>
public record ItemCarrinhoResumo(
    int ProdutoId,
    string Nome,
    string ImagemUrl,
    string Categoria,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Subtotal,
    int EstoqueDisponivel,
    int VendedorId,
    string VendedorNome,
    bool Disponivel,
    bool PrecoDeProposta,
    string? Aviso);

/// <summary>
/// Carrinho calculado no servidor. "PodeFinalizar" só é verdadeiro quando
/// todos os itens existem, estão disponíveis e cabem no estoque.
/// </summary>
public record CarrinhoResumo(
    List<ItemCarrinhoResumo> Itens,
    int QuantidadeItens,
    decimal Total,
    bool PodeFinalizar);

/// <summary>Compra (checkout) com seu pagamento simulado e seus pedidos.</summary>
public record CompraResumo(
    int Id,
    string Codigo,
    FormaPagamento Forma,
    StatusPagamento Status,
    decimal Valor,
    int Parcelas,
    decimal ValorParcela,
    DateTime CriadoEm,
    DateTime ExpiraEm,
    DateTime? ProcessadoEm,
    string CodigoPix,
    string LinhaDigitavel,
    string CodigoBarras,
    string CartaoFinal,
    string CartaoBandeira,
    string Motivo,
    bool Simulacao,
    List<Pedido> Pedidos)
{
    public static CompraResumo De(Pagamento pagamento, List<Pedido> pedidos)
    {
        return new CompraResumo(
            pagamento.Id,
            pagamento.Codigo,
            pagamento.Forma,
            pagamento.Status,
            pagamento.Valor,
            pagamento.Parcelas,
            pagamento.ValorParcela,
            pagamento.CriadoEm,
            pagamento.ExpiraEm,
            pagamento.ProcessadoEm,
            pagamento.CodigoPix,
            pagamento.LinhaDigitavel,
            new string(pagamento.LinhaDigitavel.Where(char.IsDigit).ToArray()),
            pagamento.CartaoFinal,
            pagamento.CartaoBandeira,
            pagamento.Motivo,
            Simulacao: true,
            pedidos);
    }
}

/// <summary>
/// Anúncio do catálogo com indicadores reais calculados a partir dos pedidos
/// pagos e das avaliações. Sem histórico, os números são zero — nada é
/// inventado.
/// </summary>
public record ProdutoCatalogo(
    int Id,
    int UsuarioId,
    string Nome,
    string Descricao,
    decimal Preco,
    int Estoque,
    string Categoria,
    string Marca,
    string Colecao,
    string Condicao,
    string ImagemUrl,
    string Franquia,
    Raridade Raridade,
    bool EdicaoLimitada,
    bool Destaque,
    EstadoAnuncio Estado,
    ModalidadeNegociacao Modalidade,
    bool Disponivel,
    DateTime CriadoEm,
    string VendedorNome,
    int Vendas,
    decimal MediaAvaliacoes,
    int TotalAvaliacoes)
{
    public static ProdutoCatalogo De(
        Produto produto,
        string vendedorNome,
        int vendas,
        decimal media,
        int totalAvaliacoes)
    {
        return new ProdutoCatalogo(
            produto.Id,
            produto.UsuarioId,
            produto.Nome,
            produto.Descricao,
            produto.Preco,
            produto.Estoque,
            produto.Categoria,
            produto.Marca,
            produto.Colecao,
            produto.Condicao,
            produto.ImagemUrl,
            produto.Franquia,
            produto.Raridade,
            produto.EdicaoLimitada,
            produto.Destaque,
            produto.Estado,
            produto.Modalidade,
            produto.Disponivel,
            produto.CriadoEm,
            vendedorNome,
            vendas,
            media,
            totalAvaliacoes);
    }
}

/// <summary>Vendas e avaliações reais por anúncio.</summary>
public record IndicadoresProduto(int Vendas, decimal MediaAvaliacoes, int TotalAvaliacoes)
{
    public static readonly IndicadoresProduto Vazio = new(0, 0m, 0);
}
