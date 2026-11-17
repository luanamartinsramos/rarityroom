namespace RarityRoom.Modelos.Excecoes;

/// <summary>
/// O recurso solicitado não existe.
/// </summary>
public class NaoEncontradoException : ExcecaoDominio
{
    public NaoEncontradoException(string mensagem)
        : base(mensagem, "nao-encontrado")
    {
    }
}
