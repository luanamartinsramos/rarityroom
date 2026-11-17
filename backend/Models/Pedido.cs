using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Pedido de um comprador para UM vendedor, como nos marketplaces: um checkout
/// com itens de vendedores diferentes gera um pedido por vendedor, todos
/// ligados ao mesmo <see cref="Pagamento"/>. Os itens guardam um snapshot de
/// nome e preço: se o vendedor editar o anúncio depois, o histórico continua
/// verdadeiro.
/// </summary>
public class Pedido
{
    public int Id { get; private set; }

    /// <summary>Número público e único do pedido (ex.: RR-7F3K9Q2M4B).</summary>
    public string Codigo { get; private set; } = string.Empty;

    /// <summary>Pagamento (checkout) ao qual o pedido pertence.</summary>
    public int? PagamentoId { get; private set; }

    public int CompradorId { get; private set; }

    public int VendedorId { get; private set; }

    private readonly List<ItemPedido> itens = new();

    /// <summary>
    /// Itens somente leitura. A lista real é privada: ninguém fora do pedido
    /// consegue incluir ou remover itens e quebrar a invariante
    /// Total = soma dos subtotais (o total é calculado só no construtor).
    /// </summary>
    public IReadOnlyList<ItemPedido> Itens => itens.AsReadOnly();

    public decimal Total { get; private set; }

    /// <summary>Resumo legível para listas e notificações.</summary>
    public string NomeProduto => Itens.Count switch
    {
        0 => string.Empty,
        1 => Itens[0].NomeProduto,
        _ => $"{Itens[0].NomeProduto} e mais {Itens.Count - 1} item(ns)"
    };

    public int QuantidadeTotal => Itens.Sum(item => item.Quantidade);

    /// <summary>Endereço informado no checkout, congelado no pedido.</summary>
    public DadosEntrega Entrega { get; private set; } = null!;

    public FormaPagamento FormaPagamento { get; private set; } =
        FormaPagamento.Pix;

    /// <summary>"Pendente" significa aguardando pagamento.</summary>
    public EstadoPedido Estado { get; private set; } = EstadoPedido.Pendente;

    public StatusPagamento StatusPagamento { get; private set; } =
        StatusPagamento.Pendente;

    /// <summary>
    /// Tabela única de transições, com chave (origem, operação). O pagamento
    /// não está aqui: ele só acontece pelo fluxo de <see cref="Pagamento"/>.
    /// </summary>
    private static readonly Dictionary<
        (EstadoPedido Origem, OperacaoPedido Operacao),
        EstadoPedido> Transicoes = new()
        {
            [(EstadoPedido.Pendente, OperacaoPedido.Pagar)] = EstadoPedido.Pago,
            [(EstadoPedido.Pendente, OperacaoPedido.Cancelar)] = EstadoPedido.Cancelado,
            [(EstadoPedido.Pago, OperacaoPedido.Enviar)] = EstadoPedido.Enviado,
            [(EstadoPedido.Pago, OperacaoPedido.Cancelar)] = EstadoPedido.Cancelado,
            [(EstadoPedido.Pago, OperacaoPedido.Concluir)] = EstadoPedido.Concluido,
            [(EstadoPedido.Enviado, OperacaoPedido.Entregar)] = EstadoPedido.Entregue,
            [(EstadoPedido.Enviado, OperacaoPedido.Concluir)] = EstadoPedido.Concluido,
            [(EstadoPedido.Entregue, OperacaoPedido.Concluir)] = EstadoPedido.Concluido
        };

    public DateTime CriadoEm { get; private set; }

    public DateTime? PagoEm { get; private set; }

    public DateTime? ConcluidoEm { get; private set; }

    public DateTime? CanceladoEm { get; private set; }

    public bool EstaPendente => Estado == EstadoPedido.Pendente;

    private Pedido()
    {
    }

    public Pedido(
        int compradorId,
        int vendedorId,
        IEnumerable<ItemPedido> itens,
        DadosEntrega entrega,
        FormaPagamento formaPagamento,
        int? pagamentoId = null)
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(entrega);

        if (!Enum.IsDefined(formaPagamento))
        {
            throw new ValidacaoException(
                "Informe uma forma de pagamento válida.");
        }

        if (compradorId <= 0)
        {
            throw new ValidacaoException(
                "É necessário estar autenticado para comprar.");
        }

        if (vendedorId == compradorId)
        {
            throw new ValidacaoException(
                "Você não pode comprar o próprio anúncio.");
        }

        var lista = itens.ToList();

        if (lista.Count == 0)
        {
            throw new ValidacaoException("Não é possível criar um pedido vazio.");
        }

        if (lista.Select(item => item.ProdutoId).Distinct().Count() != lista.Count)
        {
            throw new ValidacaoException("O pedido não pode repetir o mesmo anúncio.");
        }

        Codigo = Pagamento.GerarCodigo("RR-");
        PagamentoId = pagamentoId;
        CompradorId = compradorId;
        VendedorId = vendedorId;
        this.itens.AddRange(lista);
        Total = lista.Sum(item => item.Subtotal);
        Entrega = entrega;
        FormaPagamento = formaPagamento;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra a aprovação do pagamento simulado. Só o serviço de pedidos
    /// chama isto, depois de validar e baixar o estoque na mesma transação.
    /// </summary>
    public void ConfirmarPagamento(DateTime agora)
    {
        if (Estado != EstadoPedido.Pendente)
        {
            throw new TransicaoInvalidaException(
                $"Não é possível pagar um pedido no estado {Estado}.");
        }

        Aplicar(OperacaoPedido.Pagar);

        StatusPagamento = StatusPagamento.Pago;
        PagoEm = agora;
    }

    /// <summary>
    /// O pagamento não foi concluído (recusado, expirado ou compra cancelada):
    /// o pedido é encerrado sem mexer no estoque, que nunca foi baixado.
    /// </summary>
    public void EncerrarSemPagamento(StatusPagamento motivo, DateTime agora)
    {
        if (motivo is not (StatusPagamento.Recusado
            or StatusPagamento.Expirado
            or StatusPagamento.Cancelado))
        {
            throw new ValidacaoException("Motivo de encerramento inválido.");
        }

        if (Estado != EstadoPedido.Pendente)
        {
            throw new TransicaoInvalidaException(
                $"O pedido já está no estado {Estado}.");
        }

        Aplicar(OperacaoPedido.Cancelar);

        StatusPagamento = motivo;
        CanceladoEm = agora;
    }

    public void Enviar()
    {
        Aplicar(OperacaoPedido.Enviar);
    }

    public void Entregar()
    {
        Aplicar(OperacaoPedido.Entregar);
    }

    public void Concluir()
    {
        Aplicar(OperacaoPedido.Concluir);

        ConcluidoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancela um pedido já pago e ainda não enviado. O valor é estornado
    /// (simulação) e o serviço devolve as unidades ao estoque.
    /// </summary>
    public void CancelarPago(DateTime agora)
    {
        if (Estado != EstadoPedido.Pago)
        {
            throw new TransicaoInvalidaException(
                $"Só é possível cancelar um pedido pago que ainda não foi enviado (estado atual: {Estado}).");
        }

        Aplicar(OperacaoPedido.Cancelar);

        StatusPagamento = StatusPagamento.Estornado;
        CanceladoEm = agora;
    }

    public bool Participa(int usuarioId)
    {
        return CompradorId == usuarioId || VendedorId == usuarioId;
    }

    public void ValidarComprador(int solicitanteId)
    {
        if (CompradorId != solicitanteId)
        {
            throw new AcessoNegadoException(
                "Somente o comprador pode realizar esta operação.");
        }
    }

    public void ValidarVendedor(int solicitanteId)
    {
        if (VendedorId != solicitanteId)
        {
            throw new AcessoNegadoException(
                "Somente o vendedor pode realizar esta operação.");
        }
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador do pedido deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador do pedido já foi definido.");
        }

        Id = id;
    }

    private void Aplicar(OperacaoPedido operacao)
    {
        if (!Transicoes.TryGetValue((Estado, operacao), out EstadoPedido destino))
        {
            throw new TransicaoInvalidaException(
                $"Não é possível {Descrever(operacao)} um pedido no estado {Estado}.");
        }

        Estado = destino;
    }

    private static string Descrever(OperacaoPedido operacao)
    {
        return operacao switch
        {
            OperacaoPedido.Pagar => "pagar",
            OperacaoPedido.Enviar => "enviar",
            OperacaoPedido.Entregar => "confirmar a entrega de",
            OperacaoPedido.Concluir => "concluir",
            OperacaoPedido.Cancelar => "cancelar",
            _ => "alterar"
        };
    }
}
