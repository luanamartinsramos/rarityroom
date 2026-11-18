namespace RarityRoom.Servicos;

/// <summary>Nomes das políticas de limite de requisições (rate limiting).</summary>
public static class PoliticasLimite
{
    /// <summary>
    /// Login, cadastro e recuperação de senha: limite por IP, para dificultar
    /// testar muitas senhas ou e-mails em sequência.
    /// </summary>
    public const string Autenticacao = "autenticacao";
}
