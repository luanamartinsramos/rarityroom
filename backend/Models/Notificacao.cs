using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Aviso para um usuário sobre algo que aconteceu com ele. Nasce não lida e é
/// marcada pelo próprio destinatário.
/// </summary>
public class Notificacao
{
    private const int TamanhoMaximoMensagem = 300;

    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public TipoNotificacao Tipo { get; private set; }

    public string Mensagem { get; private set; } = string.Empty;

    public bool Lida { get; private set; }

    public DateTime CriadaEm { get; private set; }

    private Notificacao()
    {
    }

    public Notificacao(
        int usuarioId,
        TipoNotificacao tipo,
        string mensagem)
    {
        if (usuarioId <= 0)
        {
            throw new ValidacaoException(
                "A notificação precisa de um destinatário.");
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ValidacaoException("Tipo de notificação inválido.");
        }

        string texto = (mensagem ?? string.Empty).Trim();

        if (texto.Length == 0)
        {
            throw new ValidacaoException(
                "A notificação precisa de uma mensagem.");
        }

        if (texto.Length > TamanhoMaximoMensagem)
        {
            throw new ValidacaoException(
                $"A mensagem deve ter no máximo {TamanhoMaximoMensagem} caracteres.");
        }

        UsuarioId = usuarioId;
        Tipo = tipo;
        Mensagem = texto;
        Lida = false;
        CriadaEm = DateTime.UtcNow;
    }

    public void MarcarComoLida()
    {
        Lida = true;
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador da notificação deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador da notificação já foi definido.");
        }

        Id = id;
    }
}
