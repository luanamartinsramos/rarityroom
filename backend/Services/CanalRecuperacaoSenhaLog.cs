namespace RarityRoom.Servicos;

/// <summary>
/// E-mail simulado: sem provedor de e-mail no projeto, a mensagem é escrita
/// no log do servidor (terminal do backend). Quem tem acesso ao servidor
/// consegue concluir a demonstração; quem só conhece o e-mail da vítima, não.
/// Em produção, troque por uma implementação que envie e-mail de verdade.
/// </summary>
public class CanalRecuperacaoSenhaLog : ICanalRecuperacaoSenha
{
    private readonly ILogger<CanalRecuperacaoSenhaLog> logger;

    public CanalRecuperacaoSenhaLog(ILogger<CanalRecuperacaoSenhaLog> logger)
    {
        this.logger = logger;
    }

    public void Enviar(string email, string nome, string token)
    {
        logger.LogWarning(
            "[E-MAIL SIMULADO] Para: {Email} ({Nome}) — token de redefinição de senha: {Token} (válido por 30 minutos)",
            email,
            nome,
            token);
    }
}
