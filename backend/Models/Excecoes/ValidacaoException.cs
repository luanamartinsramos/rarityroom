namespace RarityRoom.Modelos.Excecoes;

/// <summary>
/// Dados fornecidos violam uma regra de formação (obrigatório, tamanho, faixa).
/// </summary>
public class ValidacaoException : ExcecaoDominio
{
    public ValidacaoException(string mensagem)
        : base(mensagem, "dados-invalidos")
    {
    }
}
