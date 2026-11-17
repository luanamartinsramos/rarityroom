namespace RarityRoom.Modelos.Excecoes;

/// <summary>
/// O solicitante está autenticado, mas não é o dono do recurso.
/// </summary>
public class AcessoNegadoException : ExcecaoDominio
{
    public AcessoNegadoException(string mensagem)
        : base(mensagem, "acesso-negado")
    {
    }
}
