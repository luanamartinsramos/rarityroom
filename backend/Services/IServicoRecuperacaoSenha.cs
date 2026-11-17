namespace RarityRoom.Servicos;

public interface IServicoRecuperacaoSenha
{
    /// <summary>
    /// Gera o token de redefinição. Retorna nulo quando o e-mail não existe ou
    /// a conta está suspensa — o chamador responde de forma genérica.
    /// </summary>
    string? Solicitar(string email);

    void Redefinir(string email, string token, string novaSenha);
}
