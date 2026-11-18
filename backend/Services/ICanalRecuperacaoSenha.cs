namespace RarityRoom.Servicos;

/// <summary>
/// Meio de entrega do token de redefinição de senha (e-mail, SMS...). Fica
/// atrás de uma interface para que o token chegue só ao dono da conta, nunca
/// na resposta HTTP de quem fez o pedido.
/// </summary>
public interface ICanalRecuperacaoSenha
{
    void Enviar(string email, string nome, string token);
}
