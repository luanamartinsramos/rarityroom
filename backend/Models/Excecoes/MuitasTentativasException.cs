namespace RarityRoom.Modelos.Excecoes;

/// <summary>
/// Limite de tentativas atingido (ex.: senha errada repetidas vezes). O
/// cliente deve esperar antes de tentar de novo.
/// </summary>
public class MuitasTentativasException : ExcecaoDominio
{
    public MuitasTentativasException(string mensagem)
        : base(mensagem, "muitas-tentativas")
    {
    }
}
