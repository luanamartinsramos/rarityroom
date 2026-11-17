namespace RarityRoom.Modelos.Excecoes;

/// <summary>
/// A operação solicitada não é permitida a partir do estado atual do anúncio.
/// </summary>
public class TransicaoInvalidaException : ConflitoException
{
    public TransicaoInvalidaException(string mensagem)
        : base(mensagem, "transicao-invalida")
    {
    }
}
