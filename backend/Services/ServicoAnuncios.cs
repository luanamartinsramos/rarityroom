using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Orquestra o ciclo de vida dos anúncios. A verificação de propriedade é
/// feita em toda operação de escrita — o controller nunca é a autoridade.
/// </summary>
public class ServicoAnuncios : IServicoAnuncios
{
    private readonly IRepositorioProduto repositorioProduto;

    private readonly IRepositorioCategoria repositorioCategoria;

    private readonly IRepositorioUsuario repositorioUsuario;

    public ServicoAnuncios(
        IRepositorioProduto repositorioProduto,
        IRepositorioCategoria repositorioCategoria,
        IRepositorioUsuario repositorioUsuario)
    {
        this.repositorioProduto = repositorioProduto;
        this.repositorioCategoria = repositorioCategoria;
        this.repositorioUsuario = repositorioUsuario;
    }

    /// <summary>
    /// Catálogo com filtro opcional. A comparação de texto ignora acento e
    /// caixa: quem digita "colecionaveis" encontra "Colecionáveis".
    /// </summary>
    /// <summary>
    /// Catálogo com filtro e ordenação. A comparação de texto ignora acento e
    /// caixa: quem digita "colecionaveis" encontra "Colecionáveis".
    /// </summary>
    public List<Produto> Listar(FiltroAnuncios? filtro = null)
    {
        var produtos = repositorioProduto.ObterTodos();

        if (filtro == null)
        {
            return produtos;
        }

        IEnumerable<Produto> consulta = produtos;

        if (!string.IsNullOrWhiteSpace(filtro.Termo))
        {
            string termo = BuscaTexto.Normalizar(filtro.Termo);

            consulta = consulta.Where(produto =>
                BuscaTexto.Contem(produto.Nome, termo)
                || BuscaTexto.Contem(produto.Descricao, termo)
                || BuscaTexto.Contem(produto.Marca, termo)
                || BuscaTexto.Contem(produto.Colecao, termo)
                || BuscaTexto.Contem(produto.Franquia, termo)
                || BuscaTexto.Contem(produto.Categoria, termo)
                || BuscaTexto.Contem(produto.Condicao, termo));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
        {
            string categoria = BuscaTexto.Normalizar(filtro.Categoria);

            consulta = consulta.Where(
                produto => BuscaTexto.Igual(produto.Categoria, categoria));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Franquia))
        {
            string franquia = BuscaTexto.Normalizar(filtro.Franquia);

            consulta = consulta.Where(
                produto => BuscaTexto.Igual(produto.Franquia, franquia));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Condicao))
        {
            string condicao = BuscaTexto.Normalizar(filtro.Condicao);

            consulta = consulta.Where(
                produto => BuscaTexto.Igual(produto.Condicao, condicao));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Estado)
            && Enum.TryParse<EstadoAnuncio>(
                filtro.Estado,
                ignoreCase: true,
                out EstadoAnuncio estado))
        {
            consulta = consulta.Where(produto => produto.Estado == estado);
        }

        if (filtro.SomenteDestaque == true)
        {
            consulta = consulta.Where(produto => produto.Destaque);
        }

        if (filtro.PrecoMinimo is decimal minimo)
        {
            consulta = consulta.Where(produto => produto.Preco >= minimo);
        }

        if (filtro.PrecoMaximo is decimal maximo)
        {
            consulta = consulta.Where(produto => produto.Preco <= maximo);
        }

        return OrdemDe(filtro.Ordem) switch
        {
            OrdemCatalogo.MenorPreco => consulta
                .OrderBy(produto => produto.Preco)
                .ThenBy(produto => produto.Id)
                .ToList(),

            OrdemCatalogo.MaiorPreco => consulta
                .OrderByDescending(produto => produto.Preco)
                .ThenBy(produto => produto.Id)
                .ToList(),

            OrdemCatalogo.Novidades => consulta
                .OrderByDescending(produto => produto.CriadoEm)
                .ThenByDescending(produto => produto.Id)
                .ToList(),

            _ => consulta
                .OrderByDescending(produto => produto.Destaque)
                .ThenBy(produto => produto.Id)
                .ToList()
        };
    }

    private static OrdemCatalogo OrdemDe(string? ordem)
    {
        if (string.IsNullOrWhiteSpace(ordem))
        {
            return OrdemCatalogo.Relevancia;
        }

        bool valida = Enum.TryParse(ordem, ignoreCase: true, out OrdemCatalogo valor);

        return valida && Enum.IsDefined(valor)
            ? valor
            : OrdemCatalogo.Relevancia;
    }

    /// <summary>
    /// Página do catálogo. Sem tamanho informado a listagem não é paginada;
    /// o tamanho é limitado a 60 itens por requisição.
    /// </summary>
    public PaginaCatalogo ListarPaginado(FiltroAnuncios filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        int pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        int tamanho = filtro.TamanhoPagina <= 0
            ? 12
            : Math.Min(filtro.TamanhoPagina, 60);

        var todos = Listar(filtro);

        var itens = todos
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return new PaginaCatalogo(itens, todos.Count, pagina, tamanho);
    }

    public Produto ObterPorId(int id)
    {
        var produto = repositorioProduto.ObterPorId(id);

        if (produto == null)
        {
            throw new NaoEncontradoException("Anúncio não encontrado.");
        }

        return produto;
    }

    public Produto Criar(DadosAnuncio dados, int proprietarioId)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var proprietario = repositorioUsuario.ObterPorId(proprietarioId);

        if (proprietario == null)
        {
            throw new NaoEncontradoException("Usuário não encontrado.");
        }

        var categoria = ResolverCategoria(dados.Categoria);

        var produto = new Produto(
            proprietarioId,
            dados.Nome,
            dados.Descricao,
            dados.Preco,
            dados.Estoque,
            categoria.Nome,
            dados.Marca,
            dados.Colecao,
            dados.Condicao,
            dados.ImagemUrl
        );

        produto.DefinirModalidade(dados.Modalidade);
        produto.DefinirDetalhes(
            dados.Franquia,
            dados.Raridade,
            dados.EdicaoLimitada);
        produto.DefinirDestaque(dados.Destaque);

        return repositorioProduto.Adicionar(produto);
    }

    public Produto Atualizar(int id, DadosAnuncio dados, int solicitanteId)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var produto = ObterPorId(id);

        produto.ValidarPropriedade(solicitanteId);

        var categoria = ResolverCategoria(dados.Categoria);

        produto.Atualizar(
            dados.Nome,
            dados.Descricao,
            dados.Preco,
            dados.Estoque,
            categoria.Nome,
            dados.Marca,
            dados.Colecao,
            dados.Condicao,
            dados.ImagemUrl
        );

        produto.DefinirModalidade(dados.Modalidade);
        produto.DefinirDetalhes(
            dados.Franquia,
            dados.Raridade,
            dados.EdicaoLimitada);
        produto.DefinirDestaque(dados.Destaque);

        if (!repositorioProduto.Atualizar(produto))
        {
            throw new NaoEncontradoException("Anúncio não encontrado.");
        }

        return produto;
    }

    public Produto AlterarEstado(int id, OperacaoAnuncio operacao, int solicitanteId)
    {
        var produto = ObterPorId(id);

        produto.ValidarPropriedade(solicitanteId);

        produto.AplicarOperacao(operacao);

        if (!repositorioProduto.Atualizar(produto))
        {
            throw new NaoEncontradoException("Anúncio não encontrado.");
        }

        return produto;
    }

    public Produto DefinirImagem(int id, string imagemUrl, int solicitanteId)
    {
        var produto = ObterPorId(id);

        produto.ValidarPropriedade(solicitanteId);

        produto.DefinirImagem(imagemUrl);

        if (!repositorioProduto.Atualizar(produto))
        {
            throw new NaoEncontradoException("Anúncio não encontrado.");
        }

        return produto;
    }

    public void Remover(int id, int solicitanteId)
    {
        var produto = ObterPorId(id);

        produto.ValidarExclusao(solicitanteId);

        repositorioProduto.Remover(id);
    }

    /// <summary>
    /// Regra de catálogo: o anúncio só pode usar uma categoria cadastrada, e
    /// grava o nome canônico (o do catálogo), não o texto digitado.
    /// </summary>
    private Categoria ResolverCategoria(string nome)
    {
        var categoria = repositorioCategoria.ObterPorNome(nome);

        if (categoria == null)
        {
            throw new ValidacaoException(
                $"A categoria \"{nome}\" não está cadastrada.");
        }

        return categoria;
    }
}
