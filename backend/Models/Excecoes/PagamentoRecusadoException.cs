namespace RarityRoom.Modelos.Excecoes;

/// <summary>
/// A simulação de pagamento recusou a cobrança. É um conflito de negócio:
/// o pedido continua existindo, com o status de pagamento recusado.
/// </summary>
public class PagamentoRecusadoException : ConflitoException
{
    public PagamentoRecusadoException(string mensagem)
        : base(mensagem, "pagamento-recusado")
    {
    }
}
