using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

public class ServicoCategorias : IServicoCategorias
{
    private readonly IRepositorioCategoria repositorioCategoria;

    public ServicoCategorias(IRepositorioCategoria repositorioCategoria)
    {
        this.repositorioCategoria = repositorioCategoria;
    }

    public List<Categoria> Listar()
    {
        return repositorioCategoria.ObterTodas();
    }

    /// <summary>
    /// O slug é a chave de unicidade: "Funko", "funko" e "FUNKO" colidem,
    /// como devem colidir.
    /// </summary>
    public Categoria Criar(string nome)
    {
        var categoria = new Categoria(nome);

        if (repositorioCategoria.ObterPorSlug(categoria.Slug) != null)
        {
            throw new ConflitoException(
                $"A categoria \"{categoria.Nome}\" já está cadastrada.");
        }

        return repositorioCategoria.Adicionar(categoria);
    }
}
