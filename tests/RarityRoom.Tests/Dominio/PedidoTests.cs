using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Servicos;
using Xunit;

namespace RarityRoom.Tests.Dominio;

public class PedidoTests
{
    private static readonly DateTime Agora = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

    private static Produto Anuncio(int proprietarioId = 1, decimal preco = 249.90m, int id = 10)
    {
        var produto = Apoio.Produto(proprietarioId: proprietarioId, preco: preco);
        produto.DefinirId(id);

        return produto;
    }

    private static Pedido NovoPedido(Produto produto, int compradorId, int quantidade = 1)
    {
        return new Pedido(
            compradorId,
            produto.UsuarioId,
            new[] { new ItemPedido(produto, quantidade, produto.Preco) },
            Apoio.Entrega(),
            FormaPagamento.Pix);
    }

    [Fact]
    public void Construtor_DeveGuardarSnapshotDoAnuncio()
    {
        var pedido = NovoPedido(Anuncio(preco: 249.90m), compradorId: 2, quantidade: 2);

        Assert.Equal(249.90m, pedido.Itens[0].PrecoUnitario);
        Assert.Equal(2, pedido.Itens[0].Quantidade);
        Assert.Equal(499.80m, pedido.Total);
        Assert.Equal("Funko Batman", pedido.NomeProduto);
        Assert.Equal(1, pedido.VendedorId);
        Assert.Equal(EstadoPedido.Pendente, pedido.Estado);
        Assert.StartsWith("RR-", pedido.Codigo);
    }

    [Fact]
    public void Codigo_DeveSerUnicoPorPedido()
    {
        var codigos = Enumerable.Range(0, 200)
            .Select(_ => NovoPedido(Anuncio(), compradorId: 2).Codigo)
            .ToHashSet();

        Assert.Equal(200, codigos.Count);
    }

    [Fact]
    public void Snapshot_NaoMudaQuandoOAnuncioMuda()
    {
        var anuncio = Anuncio(preco: 249.90m);
        var pedido = NovoPedido(anuncio, compradorId: 2);

        anuncio.Atualizar(
            "Funko Batman", "editado", 999.90m, 1, "Funko",
            string.Empty, string.Empty, string.Empty, string.Empty);

        Assert.Equal(249.90m, pedido.Itens[0].PrecoUnitario);
        Assert.Equal(249.90m, pedido.Total);
    }

    [Fact]
    public void Construtor_NoProprioAnuncio_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(
            () => NovoPedido(Anuncio(proprietarioId: 1), compradorId: 1));
    }

    [Fact]
    public void Construtor_SemItens_DeveFalhar()
    {
        Assert.Throws<ValidacaoException>(() => new Pedido(
            2, 1, Array.Empty<ItemPedido>(), Apoio.Entrega(), FormaPagamento.Pix));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Item_ComQuantidadeInvalida_DeveFalhar(int quantidade)
    {
        Assert.Throws<ValidacaoException>(
            () => new ItemPedido(Anuncio(), quantidade, 10m));
    }

    [Fact]
    public void Concluir_SemPagamento_DeveFalhar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        Assert.Throws<TransicaoInvalidaException>(() => pedido.Concluir());
    }

    [Fact]
    public void Concluir_JaConcluido_DeveFalhar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);
        pedido.ConfirmarPagamento(Agora);
        pedido.Concluir();

        var excecao = Assert.Throws<TransicaoInvalidaException>(
            () => pedido.Concluir());

        Assert.Equal("transicao-invalida", excecao.Codigo);
    }

    [Fact]
    public void ValidarVendedor_PeloComprador_DeveNegar()
    {
        var pedido = NovoPedido(Anuncio(proprietarioId: 1), compradorId: 2);

        Assert.Throws<AcessoNegadoException>(
            () => pedido.ValidarVendedor(2));
    }

    [Fact]
    public void ConfirmarPagamento_DeveMarcarPagoERegistrarStatus()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        pedido.ConfirmarPagamento(Agora);

        Assert.Equal(EstadoPedido.Pago, pedido.Estado);
        Assert.Equal(StatusPagamento.Pago, pedido.StatusPagamento);
        Assert.Equal(Agora, pedido.PagoEm);
    }

    [Theory]
    [InlineData(StatusPagamento.Recusado)]
    [InlineData(StatusPagamento.Expirado)]
    [InlineData(StatusPagamento.Cancelado)]
    public void EncerrarSemPagamento_DeveCancelarComOMotivo(StatusPagamento motivo)
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        pedido.EncerrarSemPagamento(motivo, Agora);

        Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);
        Assert.Equal(motivo, pedido.StatusPagamento);
    }

    [Fact]
    public void Enviar_AntesDoPagamento_DeveFalhar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        Assert.Throws<TransicaoInvalidaException>(() => pedido.Enviar());
    }

    [Fact]
    public void CicloCompleto_PagarEnviarEntregar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);

        pedido.ConfirmarPagamento(Agora);
        pedido.Enviar();
        pedido.Entregar();

        Assert.Equal(EstadoPedido.Entregue, pedido.Estado);
    }

    [Fact]
    public void Entregar_SemEnvio_DeveFalhar()
    {
        var pedido = NovoPedido(Anuncio(), compradorId: 2);
        pedido.ConfirmarPagamento(Agora);

        Assert.Throws<TransicaoInvalidaException>(() => pedido.Entregar());
    }

    [Fact]
    public void CancelarPago_DeveEstornar_EDepoisDoEnvioFalha()
    {
        var pago = NovoPedido(Anuncio(), compradorId: 2);
        pago.ConfirmarPagamento(Agora);
        pago.CancelarPago(Agora);

        Assert.Equal(EstadoPedido.Cancelado, pago.Estado);
        Assert.Equal(StatusPagamento.Estornado, pago.StatusPagamento);

        var enviado = NovoPedido(Anuncio(), compradorId: 2);
        enviado.ConfirmarPagamento(Agora);
        enviado.Enviar();

        Assert.Throws<TransicaoInvalidaException>(() => enviado.CancelarPago(Agora));
    }
}

public class EstoqueTests
{
    [Fact]
    public void BaixarEstoque_DeveDecrementar_EAoZerarFicaVendido()
    {
        var produto = Apoio.Produto(estoque: 3);

        produto.BaixarEstoque(2);

        Assert.Equal(1, produto.Estoque);
        Assert.Equal(EstadoAnuncio.Disponivel, produto.Estado);

        produto.BaixarEstoque(1);

        Assert.Equal(0, produto.Estoque);
        Assert.Equal(EstadoAnuncio.Vendido, produto.Estado);
    }

    [Fact]
    public void BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar()
    {
        var produto = Apoio.Produto(estoque: 3);

        Assert.Throws<ConflitoException>(() => produto.BaixarEstoque(4));
        Assert.Equal(3, produto.Estoque);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BaixarEstoque_QuantidadeInvalida_DeveFalhar(int quantidade)
    {
        Assert.Throws<ValidacaoException>(
            () => Apoio.Produto(estoque: 3).BaixarEstoque(quantidade));
    }

    [Fact]
    public void ReporEstoque_DeAnuncioEsgotado_VoltaADisponivel()
    {
        var produto = Apoio.Produto(estoque: 1);
        produto.BaixarEstoque(1);

        produto.ReporEstoque(1);

        Assert.Equal(1, produto.Estoque);
        Assert.Equal(EstadoAnuncio.Disponivel, produto.Estado);
    }
}

public class PagamentoTests
{
    private static readonly DateTime Agora = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Pix_DeveGerarCodigoFicticioEPrazo()
    {
        var pagamento = new Pagamento(2, FormaPagamento.Pix, 100m, "chave", Agora);

        Assert.Equal(StatusPagamento.Pendente, pagamento.Status);
        Assert.Contains("SIMULACAO", pagamento.CodigoPix);
        Assert.Equal(Agora + Pagamento.PrazoPix, pagamento.ExpiraEm);
        Assert.StartsWith("PAG-", pagamento.Codigo);
    }

    [Fact]
    public void Boleto_DeveGerarLinhaDigitavelDe47Digitos()
    {
        var pagamento = new Pagamento(2, FormaPagamento.Boleto, 123.45m, "chave", Agora);

        string digitos = new(pagamento.LinhaDigitavel.Where(char.IsDigit).ToArray());

        Assert.Equal(47, digitos.Length);
        Assert.StartsWith("999", digitos);
        Assert.EndsWith("0000012345", digitos);
    }

    [Fact]
    public void VerificarExpiracao_AposOPrazo_DeveExpirar()
    {
        var pagamento = new Pagamento(2, FormaPagamento.Pix, 100m, "chave", Agora);

        Assert.False(pagamento.VerificarExpiracao(Agora.AddMinutes(10)));
        Assert.True(pagamento.VerificarExpiracao(Agora.AddMinutes(31)));
        Assert.Equal(StatusPagamento.Expirado, pagamento.Status);
    }

    [Fact]
    public void Aprovar_JaRecusado_DeveFalhar()
    {
        var pagamento = new Pagamento(2, FormaPagamento.CartaoCredito, 100m, "chave", Agora);
        pagamento.Recusar("teste", Agora);

        Assert.Throws<TransicaoInvalidaException>(() => pagamento.Aprovar(Agora));
    }
}

public class ValidadorCartaoTests
{
    private static readonly DateTime Agora = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

    private static ModeloCartao Cartao(
        string numero = "4111 1111 1111 1111",
        string validade = "12/30",
        string cvv = "123",
        string cpf = "",
        int parcelas = 1)
    {
        return new ModeloCartao
        {
            NomeImpresso = "ANA LIMA",
            Numero = numero,
            Validade = validade,
            Cvv = cvv,
            Cpf = cpf,
            Parcelas = parcelas
        };
    }

    [Fact]
    public void CartaoValido_DeveSerAprovadoGuardandoSoOFinal()
    {
        var resultado = ValidadorCartao.Validar(Cartao(), Agora);

        Assert.Null(resultado.MotivoRecusa);
        Assert.Equal("1111", resultado.Final);
        Assert.Equal("Visa", resultado.Bandeira);
    }

    [Fact]
    public void CartaoDeTesteDeRecusa_DeveSerRecusado()
    {
        var resultado = ValidadorCartao.Validar(Cartao(numero: "4000 0000 0000 0002"), Agora);

        Assert.NotNull(resultado.MotivoRecusa);
    }

    [Theory]
    [InlineData("4111 1111 1111 1112", "12/30", "123", "", 1)]   // Luhn inválido
    [InlineData("4111 1111 1111 1111", "10/26", "123", "", 1)]   // vencido
    [InlineData("4111 1111 1111 1111", "13/30", "123", "", 1)]   // mês inválido
    [InlineData("4111 1111 1111 1111", "12/30", "12", "", 1)]    // CVV curto
    [InlineData("4111 1111 1111 1111", "12/30", "123", "111.111.111-11", 1)] // CPF inválido
    [InlineData("4111 1111 1111 1111", "12/30", "123", "", 13)]  // parcelas
    public void DadosInvalidos_DevemSerRejeitados(
        string numero, string validade, string cvv, string cpf, int parcelas)
    {
        Assert.Throws<ValidacaoException>(
            () => ValidadorCartao.Validar(Cartao(numero, validade, cvv, cpf, parcelas), Agora));
    }

    [Fact]
    public void CpfValido_DeveSerAceito()
    {
        Assert.True(ValidadorCartao.CpfValido("52998224725"));
    }
}
