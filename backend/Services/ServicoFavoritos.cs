using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

public class ServicoFavoritos : IServicoFavoritos
{
    private readonly IRepositorioFavorito repositorioFavorito;

    private readonly IRepositorioProduto repositorioProduto;

    public ServicoFavoritos(
        IRepositorioFavorito repositorioFavorito,
        IRepositorioProduto repositorioProduto)
    {
        this.repositorioFavorito = repositorioFavorito;
        this.repositorioProduto = repositorioProduto;
    }

    public List<Produto> Listar(int usuarioId)
    {
        var idsFavoritos = repositorioFavorito
            .ObterPorUsuario(usuarioId)
            .Select(favorito => favorito.ProdutoId)
            .ToHashSet();

        return repositorioProduto
            .ObterTodos()
            .Where(produto => idsFavoritos.Contains(produto.Id))
            .ToList();
    }

    public Favorito Adicionar(int produtoId, int usuarioId)
    {
        var produto = repositorioProduto.ObterPorId(produtoId);

        if (produto == null)
        {
            throw new NaoEncontradoException("Anúncio não encontrado.");
        }

        if (repositorioFavorito.Obter(usuarioId, produtoId) != null)
        {
            throw new ConflitoException(
                "Este anúncio já está nos seus favoritos.");
        }

        var favorito = new Favorito(usuarioId, produtoId, produto.UsuarioId);

        return repositorioFavorito.Adicionar(favorito);
    }

    public void Remover(int produtoId, int usuarioId)
    {
        if (!repositorioFavorito.Remover(usuarioId, produtoId))
        {
            throw new NaoEncontradoException(
                "Este anúncio não está nos seus favoritos.");
        }
    }
}
