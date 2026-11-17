using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Carrinho único da aplicação, gravado no banco e vinculado ao usuário
/// autenticado. Adicionar ao carrinho NÃO reserva nem baixa estoque e NÃO
/// altera o anúncio: o estoque só muda quando o pagamento simulado é aprovado.
/// </summary>
public class ServicoCarrinho : IServicoCarrinho
{
    private readonly IRepositorioCarrinho repositorioCarrinho;

    private readonly IRepositorioProduto repositorioProduto;

    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly RegrasCompra regras;

    public ServicoCarrinho(
        IRepositorioCarrinho repositorioCarrinho,
        IRepositorioProduto repositorioProduto,
        IRepositorioUsuario repositorioUsuario,
        RegrasCompra regras)
    {
        this.repositorioCarrinho = repositorioCarrinho;
        this.repositorioProduto = repositorioProduto;
        this.repositorioUsuario = repositorioUsuario;
        this.regras = regras;
    }

    public CarrinhoResumo Obter(int usuarioId)
    {
        var itens = repositorioCarrinho.ObterPorUsuario(usuarioId);
        var linhas = new List<ItemCarrinhoResumo>();
        var nomes = new Dictionary<int, string>();

        foreach (var item in itens)
        {
            var produto = repositorioProduto.ObterPorId(item.ProdutoId);

            if (produto == null)
            {
                // Anúncio excluído pelo vendedor: sai do carrinho.
                repositorioCarrinho.Remover(item);
                continue;
            }

            if (!nomes.TryGetValue(produto.UsuarioId, out string? vendedor))
            {
                vendedor = repositorioUsuario.ObterPorId(produto.UsuarioId)?.Nome ?? "Vendedor";
                nomes[produto.UsuarioId] = vendedor;
            }

            decimal preco = regras.PrecoPara(produto, usuarioId);
            string? aviso = regras.Problema(produto, usuarioId, item.Quantidade);

            linhas.Add(new ItemCarrinhoResumo(
                produto.Id,
                produto.Nome,
                produto.ImagemUrl,
                produto.Categoria,
                preco,
                item.Quantidade,
                Math.Round(preco * item.Quantidade, 2),
                regras.LimitePara(produto, usuarioId),
                produto.UsuarioId,
                vendedor,
                aviso == null,
                regras.PrecoDeProposta(produto, usuarioId),
                aviso));
        }

        return new CarrinhoResumo(
            linhas,
            linhas.Sum(linha => linha.Quantidade),
            linhas.Where(linha => linha.Disponivel).Sum(linha => linha.Subtotal),
            linhas.Count > 0 && linhas.All(linha => linha.Disponivel));
    }

    public CarrinhoResumo Adicionar(int usuarioId, int produtoId, int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ValidacaoException("A quantidade deve ser maior que zero.");
        }

        var produto = repositorioProduto.ObterPorId(produtoId);
        var existente = repositorioCarrinho.Obter(usuarioId, produtoId);

        int total = (existente?.Quantidade ?? 0) + quantidade;

        regras.Validar(produto, usuarioId, total);

        if (existente == null)
        {
            repositorioCarrinho.Adicionar(new ItemCarrinho(usuarioId, produtoId, quantidade));
        }
        else
        {
            existente.DefinirQuantidade(total);
            repositorioCarrinho.Atualizar(existente);
        }

        return Obter(usuarioId);
    }

    public CarrinhoResumo AlterarQuantidade(int usuarioId, int produtoId, int quantidade)
    {
        var item = repositorioCarrinho.Obter(usuarioId, produtoId)
            ?? throw new NaoEncontradoException("Este item não está no seu carrinho.");

        regras.Validar(repositorioProduto.ObterPorId(produtoId), usuarioId, quantidade);

        item.DefinirQuantidade(quantidade);
        repositorioCarrinho.Atualizar(item);

        return Obter(usuarioId);
    }

    public CarrinhoResumo Remover(int usuarioId, int produtoId)
    {
        var item = repositorioCarrinho.Obter(usuarioId, produtoId)
            ?? throw new NaoEncontradoException("Este item não está no seu carrinho.");

        repositorioCarrinho.Remover(item);

        return Obter(usuarioId);
    }

    public CarrinhoResumo Limpar(int usuarioId)
    {
        repositorioCarrinho.RemoverProdutos(
            usuarioId,
            repositorioCarrinho.ObterPorUsuario(usuarioId).Select(item => item.ProdutoId));

        return Obter(usuarioId);
    }
}
