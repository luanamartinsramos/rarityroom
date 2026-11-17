using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioCarrinho
{
    List<ItemCarrinho> ObterPorUsuario(int usuarioId);

    ItemCarrinho? Obter(int usuarioId, int produtoId);

    ItemCarrinho Adicionar(ItemCarrinho item);

    void Atualizar(ItemCarrinho item);

    void Remover(ItemCarrinho item);

    /// <summary>Remove do carrinho os anúncios informados (após compra aprovada).</summary>
    void RemoverProdutos(int usuarioId, IEnumerable<int> produtoIds);
}
