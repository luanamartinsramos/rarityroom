using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Item do carrinho de um usuário autenticado. Guarda apenas o anúncio e a
/// quantidade: preço, nome e vendedor são sempre lidos do anúncio atual, então
/// o carrinho não tem como alterar (nem "congelar") dados de terceiros.
/// </summary>
public class ItemCarrinho
{
    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public int ProdutoId { get; private set; }

    public int Quantidade { get; private set; }

    public DateTime AdicionadoEm { get; private set; }

    private ItemCarrinho()
    {
    }

    public ItemCarrinho(int usuarioId, int produtoId, int quantidade)
    {
        if (usuarioId <= 0)
        {
            throw new AcessoNegadoException(
                "É necessário estar autenticado para usar o carrinho.");
        }

        if (produtoId <= 0)
        {
            throw new ValidacaoException("Anúncio inválido.");
        }

        UsuarioId = usuarioId;
        ProdutoId = produtoId;
        AdicionadoEm = DateTime.UtcNow;

        DefinirQuantidade(quantidade);
    }

    public void DefinirQuantidade(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ValidacaoException(
                "A quantidade deve ser maior que zero. Para tirar o item, remova-o do carrinho.");
        }

        Quantidade = quantidade;
    }
}
