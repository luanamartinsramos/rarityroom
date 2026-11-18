namespace RarityRoom.Servicos;

public interface IServicoRecuperacaoSenha
{
    /// <summary>
    /// Gera o token de redefinição e o entrega pelo canal configurado. Não
    /// retorna nada de propósito: a resposta da API é sempre a mesma, exista
    /// ou não o e-mail, e o token nunca volta para quem pediu.
    /// </summary>
    void Solicitar(string email);

    void Redefinir(string email, string token, string novaSenha);
}
