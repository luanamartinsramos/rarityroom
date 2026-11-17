namespace RarityRoom.Modelos.Excecoes;

/// <summary>
/// A operação é válida em si, mas conflita com o estado atual do recurso
/// (ex.: e-mail já cadastrado, anúncio sem estoque).
/// </summary>
public class ConflitoException : ExcecaoDominio
{
    public ConflitoException(string mensagem, string codigo = "conflito")
        : base(mensagem, codigo)
    {
    }
}
