using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Vínculo entre usuário e anúncio. A regra de não favoritar o próprio
/// anúncio vive na entidade: o construtor exige o dono do anúncio para
/// decidir.
/// </summary>
public class Favorito
{
    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public int ProdutoId { get; private set; }

    public DateTime CriadoEm { get; private set; }

    /// <summary>Exigido pelo EF Core para materializar a entidade do banco.</summary>
    private Favorito()
    {
    }

    public Favorito(
        int usuarioId,
        int produtoId,
        int proprietarioProdutoId)
    {
        if (usuarioId <= 0)
        {
            throw new ValidacaoException(
                "É necessário estar autenticado para favoritar um anúncio.");
        }

        if (produtoId <= 0)
        {
            throw new ValidacaoException(
                "O favorito precisa estar vinculado a um anúncio.");
        }

        if (usuarioId == proprietarioProdutoId)
        {
            throw new ValidacaoException(
                "Você não pode favoritar o próprio anúncio.");
        }

        UsuarioId = usuarioId;
        ProdutoId = produtoId;
        CriadoEm = DateTime.UtcNow;
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador do favorito deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador do favorito já foi definido.");
        }

        Id = id;
    }
}
