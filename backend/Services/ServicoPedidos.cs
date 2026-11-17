using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Checkout, pagamento simulado e ciclo do pedido.
///
/// Fluxo: carrinho → checkout (valida itens, preços e estoque; cria a compra
/// "aguardando pagamento" e um pedido por vendedor; NÃO mexe no estoque nem
/// limpa o carrinho) → pagamento simulado (valida o estoque de novo e, só se
/// aprovado, baixa o estoque, marca os pedidos como pagos e tira os itens do
/// carrinho, tudo numa única transação).
/// </summary>
public class ServicoPedidos : IServicoPedidos
{
    private readonly IRepositorioPedido repositorioPedido;

    private readonly IRepositorioProduto repositorioProduto;

    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly IRepositorioPagamento repositorioPagamento;

    private readonly IRepositorioCarrinho repositorioCarrinho;

    private readonly IUnidadeDeTrabalho unidade;

    private readonly RegrasCompra regras;

    private readonly IServicoNotificacoes notificacoes;

    private readonly TimeProvider relogio;

    public ServicoPedidos(
        IRepositorioPedido repositorioPedido,
        IRepositorioProduto repositorioProduto,
        IRepositorioUsuario repositorioUsuario,
        IRepositorioPagamento repositorioPagamento,
        IRepositorioCarrinho repositorioCarrinho,
        IUnidadeDeTrabalho unidade,
        RegrasCompra regras,
        IServicoNotificacoes notificacoes,
        TimeProvider? relogio = null)
    {
        this.repositorioPedido = repositorioPedido;
        this.repositorioProduto = repositorioProduto;
        this.repositorioUsuario = repositorioUsuario;
        this.repositorioPagamento = repositorioPagamento;
        this.repositorioCarrinho = repositorioCarrinho;
        this.unidade = unidade;
        this.regras = regras;
        this.notificacoes = notificacoes;
        this.relogio = relogio ?? TimeProvider.System;
    }

    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    // ------------------------------------------------------------------
    // Consultas
    // ------------------------------------------------------------------

    public List<Pedido> Listar(int usuarioId)
    {
        ExpirarVencidos(usuarioId);

        var compras = repositorioPedido.ObterPorComprador(usuarioId);
        var vendas = repositorioPedido.ObterPorVendedor(usuarioId);

        return compras
            .Concat(vendas)
            .DistinctBy(pedido => pedido.Id)
            .OrderByDescending(pedido => pedido.CriadoEm)
            .ThenByDescending(pedido => pedido.Id)
            .ToList();
    }

    public Pedido ObterPorId(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        if (!pedido.Participa(solicitanteId))
        {
            throw new AcessoNegadoException("Você não participa deste pedido.");
        }

        if (pedido.PagamentoId is int pagamentoId && pedido.CompradorId == solicitanteId)
        {
            ExpirarSeVencido(pagamentoId);
        }

        return pedido;
    }

    public CompraResumo ObterCompra(int pagamentoId, int solicitanteId)
    {
        ObterPagamentoDoComprador(pagamentoId, solicitanteId);

        ExpirarSeVencido(pagamentoId);

        return Resumo(ObterPagamentoDoComprador(pagamentoId, solicitanteId));
    }

    // ------------------------------------------------------------------
    // Checkout
    // ------------------------------------------------------------------

    public CompraResumo Finalizar(
        int compradorId,
        DadosEntrega entrega,
        FormaPagamento formaPagamento,
        string chaveIdempotencia)
    {
        ArgumentNullException.ThrowIfNull(entrega);

        if (repositorioUsuario.ObterPorId(compradorId) == null)
        {
            throw new AcessoNegadoException("Entre na sua conta para finalizar a compra.");
        }

        string chave = (chaveIdempotencia ?? string.Empty).Trim();

        return unidade.Executar(() =>
        {
            // Reenvio da mesma tentativa (duplo clique, rede instável): devolve
            // a compra já criada em vez de duplicar.
            if (chave.Length > 0
                && repositorioPagamento.ObterPorChave(compradorId, chave) is Pagamento existente)
            {
                return Resumo(existente);
            }

            var carrinho = repositorioCarrinho.ObterPorUsuario(compradorId);

            if (carrinho.Count == 0)
            {
                throw new ValidacaoException("Seu carrinho está vazio.");
            }

            var linhas = new List<(Produto Produto, int Quantidade, decimal Preco)>();
            var problemas = new List<string>();

            foreach (var item in carrinho)
            {
                var produto = repositorioProduto.ObterPorId(item.ProdutoId);
                string? problema = regras.Problema(produto, compradorId, item.Quantidade);

                if (problema != null)
                {
                    problemas.Add(problema);
                    continue;
                }

                linhas.Add((produto!, item.Quantidade, regras.PrecoPara(produto!, compradorId)));
            }

            if (problemas.Count > 0)
            {
                throw new ConflitoException(
                    "Revise seu carrinho antes de finalizar: " + string.Join(" ", problemas),
                    "estoque-insuficiente");
            }

            var agora = Agora;

            // Uma compra aguardando pagamento por vez: um novo checkout
            // substitui a tentativa anterior (que não tinha baixado estoque).
            foreach (var anterior in repositorioPagamento.ObterPendentesDoComprador(compradorId))
            {
                anterior.Cancelar("Substituída por um novo checkout.", agora);
                repositorioPagamento.Atualizar(anterior);
                EncerrarPedidos(anterior.Id, StatusPagamento.Cancelado, agora);
            }

            decimal total = linhas.Sum(linha => Math.Round(linha.Preco, 2) * linha.Quantidade);

            var pagamento = repositorioPagamento.Adicionar(new Pagamento(
                compradorId,
                formaPagamento,
                total,
                chave,
                agora));

            foreach (var grupo in linhas.GroupBy(linha => linha.Produto.UsuarioId))
            {
                var itens = grupo
                    .Select(linha => new ItemPedido(linha.Produto, linha.Quantidade, linha.Preco))
                    .ToList();

                repositorioPedido.Adicionar(new Pedido(
                    compradorId,
                    grupo.Key,
                    itens,
                    // Cada pedido guarda a própria cópia do endereço (o EF não
                    // permite compartilhar a mesma instância entre pedidos).
                    entrega with { },
                    formaPagamento,
                    pagamento.Id));
            }

            return Resumo(pagamento);
        });
    }

    // ------------------------------------------------------------------
    // Pagamento simulado
    // ------------------------------------------------------------------

    /// <summary>"Simular confirmação" do PIX ou do boleto.</summary>
    public CompraResumo SimularConfirmacao(int pagamentoId, int solicitanteId)
    {
        return unidade.Executar(() =>
        {
            var pagamento = ObterPagamentoDoComprador(pagamentoId, solicitanteId);

            if (pagamento.Forma == FormaPagamento.CartaoCredito
                && pagamento.Status == StatusPagamento.Pendente)
            {
                throw new ValidacaoException(
                    "Pagamentos com cartão são confirmados pelo formulário do cartão.");
            }

            return Aprovar(pagamento);
        });
    }

    public CompraResumo PagarComCartao(int pagamentoId, ModeloCartao cartao, int solicitanteId)
    {
        return unidade.Executar(() =>
        {
            var pagamento = ObterPagamentoDoComprador(pagamentoId, solicitanteId);

            if (pagamento.Forma != FormaPagamento.CartaoCredito)
            {
                throw new ValidacaoException("Esta compra não foi feita com cartão de crédito.");
            }

            if (pagamento.Status != StatusPagamento.Pendente)
            {
                return Resumo(pagamento);
            }

            // Erro de digitação não muda a compra: o comprador corrige e tenta.
            var validado = ValidadorCartao.Validar(cartao, Agora);

            pagamento.RegistrarCartao(validado.Final, validado.Bandeira, validado.Parcelas);

            if (validado.MotivoRecusa != null)
            {
                if (pagamento.VerificarExpiracao(Agora))
                {
                    repositorioPagamento.Atualizar(pagamento);
                    EncerrarPedidos(pagamento.Id, StatusPagamento.Expirado, Agora);

                    return Resumo(pagamento);
                }

                pagamento.Recusar(validado.MotivoRecusa, Agora);
                repositorioPagamento.Atualizar(pagamento);
                EncerrarPedidos(pagamento.Id, StatusPagamento.Recusado, Agora);

                return Resumo(pagamento);
            }

            return Aprovar(pagamento);
        });
    }

    /// <summary>Botão de demonstração: força o vencimento do PIX/boleto.</summary>
    public CompraResumo SimularExpiracao(int pagamentoId, int solicitanteId)
    {
        return unidade.Executar(() =>
        {
            var pagamento = ObterPagamentoDoComprador(pagamentoId, solicitanteId);

            pagamento.Expirar(Agora);
            repositorioPagamento.Atualizar(pagamento);
            EncerrarPedidos(pagamento.Id, StatusPagamento.Expirado, Agora);

            return Resumo(pagamento);
        });
    }

    /// <summary>Cancela a compra inteira enquanto aguarda pagamento.</summary>
    public CompraResumo CancelarCompra(int pagamentoId, int solicitanteId)
    {
        return unidade.Executar(() =>
        {
            var pagamento = ObterPagamentoDoComprador(pagamentoId, solicitanteId);

            pagamento.Cancelar("Compra cancelada pelo comprador.", Agora);
            repositorioPagamento.Atualizar(pagamento);
            EncerrarPedidos(pagamento.Id, StatusPagamento.Cancelado, Agora);

            return Resumo(pagamento);
        });
    }

    /// <summary>
    /// Aprovação: revalida e baixa o estoque, marca os pedidos como pagos e
    /// tira os itens comprados do carrinho. Executa dentro da transação do
    /// chamador; o token de concorrência do estoque garante que duas compras
    /// simultâneas não vendam a mesma unidade.
    /// </summary>
    private CompraResumo Aprovar(Pagamento pagamento)
    {
        var agora = Agora;

        // Repetir a confirmação não processa de novo.
        if (pagamento.Status == StatusPagamento.Pago)
        {
            return Resumo(pagamento);
        }

        if (pagamento.VerificarExpiracao(agora))
        {
            repositorioPagamento.Atualizar(pagamento);
            EncerrarPedidos(pagamento.Id, StatusPagamento.Expirado, agora);

            return Resumo(pagamento);
        }

        if (pagamento.Status != StatusPagamento.Pendente)
        {
            throw new TransicaoInvalidaException(
                $"Este pagamento não pode mais ser confirmado (situação: {pagamento.Status}).");
        }

        var pedidos = repositorioPedido.ObterPorPagamento(pagamento.Id);
        var itens = pedidos.SelectMany(pedido => pedido.Itens).ToList();

        // Segunda validação de estoque, agora com a escrita reservada.
        var produtos = new Dictionary<int, Produto>();
        var faltas = new List<string>();

        foreach (var item in itens)
        {
            var produto = repositorioProduto.ObterPorId(item.ProdutoId);
            string? problema = regras.Problema(produto, pagamento.CompradorId, item.Quantidade);

            if (problema != null)
            {
                faltas.Add(problema);
            }
            else
            {
                produtos[item.ProdutoId] = produto!;
            }
        }

        if (faltas.Count > 0)
        {
            pagamento.Cancelar(
                "Pagamento não processado — estoque insuficiente: " + string.Join(" ", faltas)
                    + " Ajuste as quantidades no carrinho e finalize novamente.",
                agora);

            repositorioPagamento.Atualizar(pagamento);
            EncerrarPedidos(pagamento.Id, StatusPagamento.Cancelado, agora);

            return Resumo(pagamento);
        }

        foreach (var item in itens)
        {
            var produto = produtos[item.ProdutoId];

            produto.BaixarEstoque(item.Quantidade);
            repositorioProduto.Atualizar(produto);
        }

        pagamento.Aprovar(agora);
        repositorioPagamento.Atualizar(pagamento);

        foreach (var pedido in pedidos)
        {
            pedido.ConfirmarPagamento(agora);
            repositorioPedido.Atualizar(pedido);

            notificacoes.Notificar(
                pedido.VendedorId,
                TipoNotificacao.PedidoPago,
                $"Pedido {pedido.Codigo} pago: \"{pedido.NomeProduto}\". Prepare o envio.");
        }

        repositorioCarrinho.RemoverProdutos(
            pagamento.CompradorId,
            itens.Select(item => item.ProdutoId));

        return Resumo(pagamento);
    }

    // ------------------------------------------------------------------
    // Ciclo do pedido (vendedor/comprador)
    // ------------------------------------------------------------------

    public Pedido Enviar(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        pedido.ValidarVendedor(solicitanteId);

        pedido.Enviar();

        repositorioPedido.Atualizar(pedido);

        notificacoes.Notificar(
            pedido.CompradorId,
            TipoNotificacao.PedidoEnviado,
            $"Seu pedido {pedido.Codigo} (\"{pedido.NomeProduto}\") foi enviado.");

        return pedido;
    }

    public Pedido Entregar(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        pedido.ValidarComprador(solicitanteId);

        pedido.Entregar();

        repositorioPedido.Atualizar(pedido);

        return pedido;
    }

    /// <summary>
    /// Conclusão pelo vendedor. O estoque já foi baixado no pagamento, então
    /// aqui só o estado do pedido muda.
    /// </summary>
    public Pedido Concluir(int id, int solicitanteId)
    {
        var pedido = Obter(id);

        pedido.ValidarVendedor(solicitanteId);

        pedido.Concluir();

        repositorioPedido.Atualizar(pedido);

        notificacoes.Notificar(
            pedido.CompradorId,
            TipoNotificacao.PedidoConcluido,
            $"Seu pedido {pedido.Codigo} foi concluído.");

        return pedido;
    }

    /// <summary>
    /// Cancelamento. Aguardando pagamento: cancela a compra inteira (nada de
    /// estoque a devolver). Pago e não enviado: estorna (simulação) e devolve
    /// as unidades ao estoque.
    /// </summary>
    public Pedido Cancelar(int id, int solicitanteId)
    {
        return unidade.Executar(() =>
        {
            var pedido = Obter(id);

            if (!pedido.Participa(solicitanteId))
            {
                throw new AcessoNegadoException("Você não participa deste pedido.");
            }

            var agora = Agora;

            if (pedido.Estado == EstadoPedido.Pendente)
            {
                if (pedido.PagamentoId is int pagamentoId
                    && repositorioPagamento.ObterPorId(pagamentoId) is Pagamento pagamento
                    && pagamento.Status == StatusPagamento.Pendente)
                {
                    pagamento.Cancelar("Compra cancelada.", agora);
                    repositorioPagamento.Atualizar(pagamento);
                    EncerrarPedidos(pagamento.Id, StatusPagamento.Cancelado, agora);
                }
                else
                {
                    pedido.EncerrarSemPagamento(StatusPagamento.Cancelado, agora);
                    repositorioPedido.Atualizar(pedido);
                }

                return Obter(id);
            }

            pedido.CancelarPago(agora);

            foreach (var item in pedido.Itens)
            {
                var produto = repositorioProduto.ObterPorId(item.ProdutoId);

                if (produto != null)
                {
                    produto.ReporEstoque(item.Quantidade);
                    repositorioProduto.Atualizar(produto);
                }
            }

            repositorioPedido.Atualizar(pedido);

            int outraParte = pedido.CompradorId == solicitanteId
                ? pedido.VendedorId
                : pedido.CompradorId;

            notificacoes.Notificar(
                outraParte,
                TipoNotificacao.PedidoCancelado,
                $"O pedido {pedido.Codigo} foi cancelado e o valor estornado (simulação).");

            return pedido;
        });
    }

    // ------------------------------------------------------------------
    // Apoio
    // ------------------------------------------------------------------

    private void ExpirarVencidos(int compradorId)
    {
        foreach (var pagamento in repositorioPagamento.ObterPendentesDoComprador(compradorId))
        {
            ExpirarSeVencido(pagamento.Id);
        }
    }

    private void ExpirarSeVencido(int pagamentoId)
    {
        var pagamento = repositorioPagamento.ObterPorId(pagamentoId);

        if (pagamento == null || pagamento.Status != StatusPagamento.Pendente || Agora < pagamento.ExpiraEm)
        {
            return;
        }

        unidade.Executar(() =>
        {
            if (pagamento.VerificarExpiracao(Agora))
            {
                repositorioPagamento.Atualizar(pagamento);
                EncerrarPedidos(pagamento.Id, StatusPagamento.Expirado, Agora);
            }

            return true;
        });
    }

    private void EncerrarPedidos(int pagamentoId, StatusPagamento motivo, DateTime agora)
    {
        foreach (var pedido in repositorioPedido.ObterPorPagamento(pagamentoId))
        {
            if (pedido.Estado == EstadoPedido.Pendente)
            {
                pedido.EncerrarSemPagamento(motivo, agora);
                repositorioPedido.Atualizar(pedido);
            }
        }
    }

    private CompraResumo Resumo(Pagamento pagamento)
    {
        return CompraResumo.De(pagamento, repositorioPedido.ObterPorPagamento(pagamento.Id));
    }

    private Pagamento ObterPagamentoDoComprador(int pagamentoId, int solicitanteId)
    {
        var pagamento = repositorioPagamento.ObterPorId(pagamentoId)
            ?? throw new NaoEncontradoException("Compra não encontrada.");

        if (pagamento.CompradorId != solicitanteId)
        {
            throw new AcessoNegadoException("Esta compra pertence a outro usuário.");
        }

        return pagamento;
    }

    private Pedido Obter(int id)
    {
        return repositorioPedido.ObterPorId(id)
            ?? throw new NaoEncontradoException("Pedido não encontrado.");
    }
}
