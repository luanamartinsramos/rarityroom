using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Modelos.Negociacao;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Negociação entre comprador e vendedor. A decisão de aceitar uma oferta não
/// é um if/else por modalidade: o serviço escolhe a estratégia correspondente
/// e delega a decisão, então uma nova modalidade entra sem alterar este
/// código.
/// </summary>
public class ServicoPropostas : IServicoPropostas
{
    private readonly IRepositorioProposta repositorioProposta;

    private readonly IRepositorioProduto repositorioProduto;

    private readonly IReadOnlyDictionary<
        ModalidadeNegociacao,
        IEstrategiaNegociacao> estrategias;

    private readonly IServicoNotificacoes notificacoes;

    public ServicoPropostas(
        IRepositorioProposta repositorioProposta,
        IRepositorioProduto repositorioProduto,
        IEnumerable<IEstrategiaNegociacao> estrategias,
        IServicoNotificacoes notificacoes)
    {
        this.repositorioProposta = repositorioProposta;
        this.repositorioProduto = repositorioProduto;
        this.estrategias = estrategias.ToDictionary(
            estrategia => estrategia.Modalidade);
        this.notificacoes = notificacoes;
    }

    public List<Proposta> Listar(int usuarioId)
    {
        var anunciosProprios = repositorioProduto
            .ObterTodos()
            .Where(produto => produto.UsuarioId == usuarioId)
            .Select(produto => produto.Id)
            .ToHashSet();

        return repositorioProposta
            .ObterTodas()
            .Where(proposta =>
                proposta.ProponenteId == usuarioId
                || anunciosProprios.Contains(proposta.ProdutoId))
            .OrderByDescending(proposta => proposta.CriadaEm)
            .ToList();
    }

    public Proposta Criar(
        int produtoId,
        decimal valor,
        string mensagem,
        int proponenteId)
    {
        var produto = ObterProduto(produtoId);

        if (produto.UsuarioId == proponenteId)
        {
            throw new ValidacaoException(
                "Você não pode enviar proposta para o próprio anúncio.");
        }

        if (produto.Estado != EstadoAnuncio.Disponivel)
        {
            throw new ConflitoException(
                $"O anúncio não está disponível para propostas (estado {produto.Estado}).");
        }

        var proposta = new Proposta(produtoId, proponenteId, valor, mensagem);

        var salva = repositorioProposta.Adicionar(proposta);

        notificacoes.Notificar(
            produto.UsuarioId,
            TipoNotificacao.PropostaRecebida,
            $"Você recebeu uma proposta para \"{produto.Nome}\". Abra suas propostas para responder.");

        return salva;
    }

    public Proposta Aceitar(int id, int solicitanteId)
    {
        var proposta = Obter(id);

        var produto = ObterProduto(proposta.ProdutoId);

        produto.ValidarPropriedade(solicitanteId);

        if (produto.Estado != EstadoAnuncio.Disponivel)
        {
            throw new ConflitoException(
                $"O anúncio não está disponível para aceitar propostas (estado {produto.Estado}).");
        }

        var estrategia = ObterEstrategia(produto.Modalidade);

        if (!estrategia.Aceita(produto.Preco, proposta.Valor))
        {
            throw new ConflitoException(
                $"Oferta recusada pela modalidade {produto.Modalidade}: o mínimo aceito é {estrategia.ValorMinimo(produto.Preco)}.");
        }

        proposta.Aceitar();

        produto.Reservar();

        repositorioProposta.Atualizar(proposta);

        repositorioProduto.Atualizar(produto);

        RecusarDemaisPendentes(proposta);

        notificacoes.Notificar(
            proposta.ProponenteId,
            TipoNotificacao.PropostaAceita,
            "Sua proposta foi aceita pelo vendedor.");

        return proposta;
    }

    public Proposta Recusar(int id, int solicitanteId)
    {
        var proposta = Obter(id);

        var produto = ObterProduto(proposta.ProdutoId);

        produto.ValidarPropriedade(solicitanteId);

        proposta.Recusar();

        repositorioProposta.Atualizar(proposta);

        notificacoes.Notificar(
            proposta.ProponenteId,
            TipoNotificacao.PropostaRecusada,
            "Sua proposta foi recusada pelo vendedor.");

        return proposta;
    }

    public Proposta Cancelar(int id, int solicitanteId)
    {
        var proposta = Obter(id);

        proposta.ValidarProponente(solicitanteId);

        proposta.Cancelar();

        repositorioProposta.Atualizar(proposta);

        return proposta;
    }

    /// <summary>
    /// Aceitar uma proposta fecha a negociação: as demais ofertas pendentes do
    /// mesmo anúncio são recusadas, para não deixar compradores esperando por
    /// um item que já tem dono.
    /// </summary>
    private void RecusarDemaisPendentes(Proposta aceita)
    {
        foreach (var pendente in repositorioProposta.ObterPorAnuncio(aceita.ProdutoId))
        {
            if (pendente.Id == aceita.Id || !pendente.EstaPendente)
            {
                continue;
            }

            pendente.Recusar();

            repositorioProposta.Atualizar(pendente);
        }
    }

    private Proposta Obter(int id)
    {
        var proposta = repositorioProposta.ObterPorId(id);

        if (proposta == null)
        {
            throw new NaoEncontradoException("Proposta não encontrada.");
        }

        return proposta;
    }

    private Produto ObterProduto(int produtoId)
    {
        var produto = repositorioProduto.ObterPorId(produtoId);

        if (produto == null)
        {
            throw new NaoEncontradoException("Anúncio não encontrado.");
        }

        return produto;
    }

    private IEstrategiaNegociacao ObterEstrategia(ModalidadeNegociacao modalidade)
    {
        if (!estrategias.TryGetValue(modalidade, out var estrategia))
        {
            throw new ValidacaoException(
                $"Não há estratégia de negociação para a modalidade {modalidade}.");
        }

        return estrategia;
    }
}
