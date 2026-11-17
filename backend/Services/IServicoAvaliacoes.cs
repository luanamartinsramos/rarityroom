using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoAvaliacoes
{
    Avaliacao Avaliar(
        int pedidoId,
        int autorId,
        int nota,
        string comentario);

    ReputacaoVendedor ObterReputacao(int vendedorId);

    List<Avaliacao> ListarDoVendedor(int vendedorId);

    ResumoAvaliacoes ObterResumoProduto(int produtoId);
}
