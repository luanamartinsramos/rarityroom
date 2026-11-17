using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

/// <summary>
/// Dublê de teste do repositório de anúncios. Mantém a mesma semântica do
/// repositório real (id único e crescente, cópia defensiva, acesso seguro sob
/// concorrência) sem tocar em banco — o que mantém a suíte rápida. A
/// persistência real é exercitada em <c>RepositorioEfTests</c>.
/// </summary>
public class RepositorioProdutoMemoria : IRepositorioProduto
{
    private readonly List<Produto> produtos = new();

    private readonly object trava = new();

    private int ultimoId;

    public List<Produto> ObterTodos()
    {
        lock (trava)
        {
            return new List<Produto>(produtos);
        }
    }

    public Produto? ObterPorId(int id)
    {
        lock (trava)
        {
            return produtos.FirstOrDefault(produto => produto.Id == id);
        }
    }

    public Produto Adicionar(Produto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        lock (trava)
        {
            produto.DefinirId(++ultimoId);

            produtos.Add(produto);

            return produto;
        }
    }

    public bool Atualizar(Produto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        lock (trava)
        {
            int indice = produtos.FindIndex(item => item.Id == produto.Id);

            if (indice < 0)
            {
                return false;
            }

            produtos[indice] = produto;

            return true;
        }
    }

    public bool Remover(int id)
    {
        lock (trava)
        {
            int indice = produtos.FindIndex(produto => produto.Id == id);

            if (indice < 0)
            {
                return false;
            }

            produtos.RemoveAt(indice);

            return true;
        }
    }
}
