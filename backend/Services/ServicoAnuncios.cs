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

    private readonly IEstatisticasCatalogo? estatisticas;

    public ServicoAnuncios(
        IRepositorioProduto repositorioProduto,
        IRepositorioCategoria repositorioCategoria,
        IRepositorioUsuario repositorioUsuario,
        IEstatisticasCatalogo? estatisticas = null)
    {
        this.estatisticas = estatisticas;
        this.repositorioProduto = repositorioProduto;
        this.repositorioCategoria = repositorioCategoria;
        this.repositorioUsuario = repositorioUsuario;
    }

    /// <summary>
    /// Catálogo com pesquisa, filtros e ordenação. A comparação de texto
    /// ignora acento e caixa; o termo é quebrado em palavras e TODAS precisam
    /// aparecer em algum campo do anúncio (nome, descrição, categoria, marca,
    /// coleção, franquia, condição ou raridade). Sem filtro de estado, só
    /// anúncios que podem ser comprados aparecem na vitrine.
    /// </summary>
    public List<Produto> Listar(FiltroAnuncios? filtro = null)
    {
        var produtos = repositorioProduto.ObterTodos();

        if (filtro == null)
        {
            return produtos;
        }

        var indicadores = estatisticas?.ObterIndicadores()
            ?? new Dictionary<int, IndicadoresProduto>();

        return Filtrar(produtos, filtro, indicadores)
            .Select(item => item.Produto)
            .ToList();
    }

    private static List<(Produto Produto, int Pontos)> Filtrar(
        List<Produto> produtos,
        FiltroAnuncios filtro,
        Dictionary<int, IndicadoresProduto> indicadores)
    {
        IEnumerable<Produto> consulta = produtos;

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

        if (string.IsNullOrWhiteSpace(filtro.Estado))
        {
            // Vitrine: só o que pode ser comprado agora.
            consulta = consulta.Where(produto => produto.Disponivel);
        }
        else if (Enum.TryParse<EstadoAnuncio>(
                filtro.Estado,
                ignoreCase: true,
                out EstadoAnuncio estado)
            && Enum.IsDefined(estado))
        {
            consulta = consulta.Where(produto => produto.Estado == estado);
        }

        // Qualquer outro valor (ex.: "todos") não filtra por estado.

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

        string termo = BuscaTexto.Normalizar(filtro.Termo);
        var tokens = BuscaTexto.Tokens(filtro.Termo);

        var pontuados = consulta
            .Select(produto => (Produto: produto, Pontos: Pontuar(produto, termo, tokens)))
            .Where(item => tokens.Count == 0 || item.Pontos > 0)
            .ToList();

        IndicadoresProduto De(Produto produto) =>
            indicadores.TryGetValue(produto.Id, out var valor)
                ? valor
                : IndicadoresProduto.Vazio;

        IOrderedEnumerable<(Produto Produto, int Pontos)> ordenado = OrdemDe(filtro.Ordem) switch
        {
            OrdemCatalogo.MenorPreco => pontuados
                .OrderBy(item => item.Produto.Preco)
                .ThenByDescending(item => item.Pontos),

            OrdemCatalogo.MaiorPreco => pontuados
                .OrderByDescending(item => item.Produto.Preco)
                .ThenByDescending(item => item.Pontos),

            OrdemCatalogo.MaisRecentes => pontuados
                .OrderByDescending(item => item.Produto.CriadoEm)
                .ThenByDescending(item => item.Produto.Id),

            OrdemCatalogo.MaisAntigos => pontuados
                .OrderBy(item => item.Produto.CriadoEm)
                .ThenBy(item => item.Produto.Id),

            // Sem vendas = 0: vão para o fim, desempatados pela relevância.
            OrdemCatalogo.MaisVendidos => pontuados
                .OrderByDescending(item => De(item.Produto).Vendas)
                .ThenByDescending(item => item.Pontos)
                .ThenByDescending(item => item.Produto.CriadoEm),

            // Sem avaliação vai para o fim; empate de média favorece quem tem
            // mais avaliações.
            OrdemCatalogo.MelhorAvaliados => pontuados
                .OrderByDescending(item => De(item.Produto).TotalAvaliacoes > 0)
                .ThenByDescending(item => De(item.Produto).MediaAvaliacoes)
                .ThenByDescending(item => De(item.Produto).TotalAvaliacoes)
                .ThenByDescending(item => item.Pontos),

            _ => pontuados
                .OrderByDescending(item => item.Pontos)
                .ThenByDescending(item => item.Produto.Destaque)
                .ThenByDescending(item => De(item.Produto).Vendas)
                .ThenByDescending(item => item.Produto.CriadoEm)
        };

        return ordenado
            .ThenBy(item => item.Produto.Id)
            .ToList();
    }

    /// <summary>
    /// Pontuação de relevância. Zero significa "não corresponde": alguma
    /// palavra do termo não aparece em nenhum campo. O nome pesa mais que
    /// categoria/franquia/coleção/marca, que pesam mais que a descrição.
    /// Sem termo, todos recebem 1 (a ordem cai nos critérios seguintes).
    /// </summary>
    private static int Pontuar(Produto produto, string termo, List<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return 1;
        }

        string nome = BuscaTexto.Normalizar(produto.Nome);
        string atributos = BuscaTexto.Normalizar(string.Join(' ',
            produto.Categoria,
            produto.Franquia,
            produto.Colecao,
            produto.Marca));
        string caracteristicas = BuscaTexto.Normalizar(string.Join(' ',
            produto.Condicao,
            produto.Raridade.ToString(),
            produto.EdicaoLimitada ? "edicao limitada" : string.Empty));
        string descricao = BuscaTexto.Normalizar(produto.Descricao);

        int pontos = 0;

        foreach (string token in tokens)
        {
            int doToken = 0;

            if (nome.Contains(token, StringComparison.Ordinal))
            {
                doToken += 10;

                bool inicioDePalavra = nome.StartsWith(token, StringComparison.Ordinal)
                    || nome.Contains(" " + token, StringComparison.Ordinal);

                if (inicioDePalavra)
                {
                    doToken += 5;
                }
            }

            if (atributos.Contains(token, StringComparison.Ordinal))
            {
                doToken += 5;
            }

            if (caracteristicas.Contains(token, StringComparison.Ordinal))
            {
                doToken += 3;
            }

            if (descricao.Contains(token, StringComparison.Ordinal))
            {
                doToken += 1;
            }

            if (doToken == 0)
            {
                return 0;
            }

            pontos += doToken;
        }

        if (nome == termo)
        {
            pontos += 100;
        }
        else if (nome.StartsWith(termo, StringComparison.Ordinal))
        {
            pontos += 50;
        }
        else if (nome.Contains(termo, StringComparison.Ordinal))
        {
            pontos += 30;
        }

        return pontos;
    }

    private static OrdemCatalogo OrdemDe(string? ordem)
    {
        if (string.IsNullOrWhiteSpace(ordem))
        {
            return OrdemCatalogo.Relevancia;
        }

        // Compatibilidade com o nome antigo da opção.
        if (ordem.Equals("Novidades", StringComparison.OrdinalIgnoreCase))
        {
            return OrdemCatalogo.MaisRecentes;
        }

        bool valida = Enum.TryParse(ordem, ignoreCase: true, out OrdemCatalogo valor);

        return valida && Enum.IsDefined(valor)
            ? valor
            : OrdemCatalogo.Relevancia;
    }

    /// <summary>
    /// Página do catálogo, já com vendedor, vendas e avaliações reais de cada
    /// anúncio. Tamanho zero devolve tudo numa página só; o máximo é 60.
    /// </summary>
    public PaginaCatalogo ListarPaginado(FiltroAnuncios filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var indicadores = estatisticas?.ObterIndicadores()
            ?? new Dictionary<int, IndicadoresProduto>();

        var todos = Filtrar(repositorioProduto.ObterTodos(), filtro, indicadores)
            .Select(item => item.Produto)
            .ToList();

        int pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        int tamanho = filtro.TamanhoPagina <= 0
            ? Math.Max(todos.Count, 1)
            : Math.Min(filtro.TamanhoPagina, 60);

        var itens = todos
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return new PaginaCatalogo(
            MontarCatalogo(itens, indicadores),
            todos.Count,
            pagina,
            tamanho);
    }

    /// <summary>Todos os anúncios do usuário, em qualquer estado.</summary>
    public List<ProdutoCatalogo> ListarDoUsuario(int usuarioId)
    {
        var meus = repositorioProduto
            .ObterTodos()
            .Where(produto => produto.UsuarioId == usuarioId)
            .OrderByDescending(produto => produto.CriadoEm)
            .ToList();

        return MontarCatalogo(
            meus,
            estatisticas?.ObterIndicadores() ?? new Dictionary<int, IndicadoresProduto>());
    }

    public ProdutoCatalogo ObterDetalhe(int id)
    {
        var produto = ObterPorId(id);

        return MontarCatalogo(
            new List<Produto> { produto },
            estatisticas?.ObterIndicadores() ?? new Dictionary<int, IndicadoresProduto>())[0];
    }

    private List<ProdutoCatalogo> MontarCatalogo(
        List<Produto> produtos,
        Dictionary<int, IndicadoresProduto> indicadores)
    {
        var nomes = repositorioUsuario
            .ObterTodos()
            .ToDictionary(usuario => usuario.Id, usuario => usuario.Nome);

        return produtos
            .Select(produto =>
            {
                var numeros = indicadores.TryGetValue(produto.Id, out var valor)
                    ? valor
                    : IndicadoresProduto.Vazio;

                return ProdutoCatalogo.De(
                    produto,
                    nomes.TryGetValue(produto.UsuarioId, out string? nome) ? nome : "Vendedor",
                    numeros.Vendas,
                    numeros.MediaAvaliacoes,
                    numeros.TotalAvaliacoes);
            })
            .ToList();
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

    public void GarantirDono(int id, int solicitanteId)
    {
        ObterPorId(id).ValidarPropriedade(solicitanteId);
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
