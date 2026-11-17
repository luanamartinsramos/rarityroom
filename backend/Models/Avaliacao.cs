using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Avaliação do comprador sobre o vendedor, vinculada a um pedido concluído.
/// A nota pertence ao pedido: uma compra gera no máximo uma avaliação.
/// </summary>
public class Avaliacao
{
    private const int NotaMinima = 1;
    private const int NotaMaxima = 5;
    private const int TamanhoMaximoComentario = 500;

    public int Id { get; private set; }

    public int PedidoId { get; private set; }

    /// <summary>
    /// Anúncio avaliado. Vem do pedido, então a média do produto é derivada
    /// das mesmas avaliações que medem o vendedor.
    /// </summary>
    public int ProdutoId { get; private set; }

    public int AutorId { get; private set; }

    public int VendedorId { get; private set; }

    public int Nota { get; private set; }

    public string Comentario { get; private set; } = string.Empty;

    public DateTime CriadaEm { get; private set; }

    private Avaliacao()
    {
    }

    public Avaliacao(
        Pedido pedido,
        int autorId,
        int nota,
        string comentario)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        if (pedido.Estado != EstadoPedido.Concluido)
        {
            throw new ConflitoException(
                "Só é possível avaliar um pedido concluído.");
        }

        if (pedido.CompradorId != autorId)
        {
            throw new AcessoNegadoException(
                "Somente o comprador pode avaliar este pedido.");
        }

        if (nota < NotaMinima || nota > NotaMaxima)
        {
            throw new ValidacaoException(
                $"A nota deve estar entre {NotaMinima} e {NotaMaxima}.");
        }

        string texto = (comentario ?? string.Empty).Trim();

        if (texto.Length > TamanhoMaximoComentario)
        {
            throw new ValidacaoException(
                $"O comentário deve ter no máximo {TamanhoMaximoComentario} caracteres.");
        }

        PedidoId = pedido.Id;
        ProdutoId = pedido.ProdutoId;
        AutorId = autorId;
        VendedorId = pedido.VendedorId;
        Nota = nota;
        Comentario = texto;
        CriadaEm = DateTime.UtcNow;
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador da avaliação deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador da avaliação já foi definido.");
        }

        Id = id;
    }
}
