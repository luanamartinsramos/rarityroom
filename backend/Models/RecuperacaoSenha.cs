using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Pedido de redefinição de senha. Sem serviço de e-mail configurado, o token
/// é devolvido na própria resposta e claramente marcado como demonstração.
/// </summary>
public class RecuperacaoSenha
{
    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public string Token { get; private set; } = string.Empty;

    public DateTime ExpiraEm { get; private set; }

    public bool Usada { get; private set; }

    private RecuperacaoSenha()
    {
    }

    public RecuperacaoSenha(int usuarioId, string token, DateTime expiraEm)
    {
        if (usuarioId <= 0)
        {
            throw new ValidacaoException(
                "A recuperação precisa estar vinculada a um usuário.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ValidacaoException("O token é obrigatório.");
        }

        UsuarioId = usuarioId;
        Token = token.Trim();
        ExpiraEm = expiraEm;
    }

    public bool EstaValido(DateTime agora)
    {
        return !Usada && agora <= ExpiraEm;
    }

    public void MarcarComoUsada()
    {
        Usada = true;
    }

    internal void DefinirId(int id)
    {
        Id = id;
    }
}
