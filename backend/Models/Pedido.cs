using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Compra de um anúncio. Guarda um <b>snapshot</b> do nome e do preço no
/// momento da compra: se o vendedor editar o anúncio depois, o histórico do
/// pedido continua verdadeiro.
/// </summary>
public class Pedido
{
    public int Id { get; private set; }

    public int ProdutoId { get; private set; }

    public int CompradorId { get; private set; }

    public int VendedorId { get; private set; }

    public string NomeProduto { get; private set; } = string.Empty;

    public decimal PrecoUnitario { get; private set; }

    /// <summary>Endereço informado no checkout, congelado no pedido.</summary>
    public DadosEntrega Entrega { get; private set; } = null!;

    public FormaPagamento FormaPagamento { get; private set; } =
        FormaPagamento.Pix;

    public EstadoPedido Estado { get; private set; } = EstadoPedido.Pendente;

    /// <summary>Resultado do pagamento demonstrativo.</summary>
    public StatusPagamento StatusPagamento { get; private set; } =
        StatusPagamento.Pendente;

    /// <summary>
    /// Tabela única de transições, com chave (origem, operação). Concluir a
    /// partir de Pendente existe para o fluxo antigo continuar válido.
    /// </summary>
    private static readonly Dictionary<
        (EstadoPedido Origem, OperacaoPedido Operacao),
        EstadoPedido> Transicoes = new()
        {
            [(EstadoPedido.Pendente, OperacaoPedido.Pagar)] = EstadoPedido.Pago,
            [(EstadoPedido.Pendente, OperacaoPedido.Cancelar)] = EstadoPedido.Cancelado,
            [(EstadoPedido.Pendente, OperacaoPedido.Concluir)] = EstadoPedido.Concluido,
            [(EstadoPedido.Pago, OperacaoPedido.Enviar)] = EstadoPedido.Enviado,
            [(EstadoPedido.Pago, OperacaoPedido.Cancelar)] = EstadoPedido.Cancelado,
            [(EstadoPedido.Pago, OperacaoPedido.Concluir)] = EstadoPedido.Concluido,
            [(EstadoPedido.Enviado, OperacaoPedido.Entregar)] = EstadoPedido.Entregue,
            [(EstadoPedido.Enviado, OperacaoPedido.Concluir)] = EstadoPedido.Concluido,
            [(EstadoPedido.Entregue, OperacaoPedido.Concluir)] = EstadoPedido.Concluido
        };

    public DateTime CriadoEm { get; private set; }

    public DateTime? ConcluidoEm { get; private set; }

    public DateTime? CanceladoEm { get; private set; }

    public bool EstaPendente => Estado == EstadoPedido.Pendente;

    private Pedido()
    {
    }

    public Pedido(
        Produto produto,
        int compradorId,
        DadosEntrega entrega,
        FormaPagamento formaPagamento)
    {
        ArgumentNullException.ThrowIfNull(produto);
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

        if (produto.UsuarioId == compradorId)
        {
            throw new ValidacaoException(
                "Você não pode comprar o próprio anúncio.");
        }

        if (produto.Estado != EstadoAnuncio.Disponivel)
        {
            throw new ConflitoException(
                $"O anúncio não está disponível para compra (estado {produto.Estado}).");
        }

        ProdutoId = produto.Id;
        CompradorId = compradorId;
        VendedorId = produto.UsuarioId;
        NomeProduto = produto.Nome;
        PrecoUnitario = produto.Preco;
        Entrega = entrega;
        FormaPagamento = formaPagamento;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra o pagamento demonstrativo. Não há gateway: o resultado é
    /// informado pelo cliente e fica gravado no pedido.
    /// </summary>
    public void Pagar(bool aprovado)
    {
        if (Estado != EstadoPedido.Pendente)
        {
            throw new TransicaoInvalidaException(
                $"Não é possível pagar um pedido no estado {Estado}.");
        }

        if (!aprovado)
        {
            StatusPagamento = StatusPagamento.Recusado;

            throw new PagamentoRecusadoException(
                "Pagamento recusado pela simulação.");
        }

        StatusPagamento = StatusPagamento.Pago;

        Aplicar(OperacaoPedido.Pagar);
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

    public void Cancelar()
    {
        Aplicar(OperacaoPedido.Cancelar);

        CanceladoEm = DateTime.UtcNow;
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
                $"Não é possível {operacao} um pedido no estado {Estado}.");
        }

        Estado = destino;
    }
}
