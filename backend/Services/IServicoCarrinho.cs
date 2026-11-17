using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoCarrinho
{
    CarrinhoResumo Obter(int usuarioId);

    CarrinhoResumo Adicionar(int usuarioId, int produtoId, int quantidade);

    CarrinhoResumo AlterarQuantidade(int usuarioId, int produtoId, int quantidade);

    CarrinhoResumo Remover(int usuarioId, int produtoId);

    CarrinhoResumo Limpar(int usuarioId);
}
