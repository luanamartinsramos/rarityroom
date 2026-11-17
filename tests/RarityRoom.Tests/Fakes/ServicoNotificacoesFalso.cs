using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Servicos;

namespace RarityRoom.Tests.Fakes;

/// <summary>
/// Dublê que apenas registra o que seria notificado. Permite testar o efeito
/// no fluxo sem depender de banco.
/// </summary>
public class ServicoNotificacoesFalso : IServicoNotificacoes
{
    public List<(int UsuarioId, TipoNotificacao Tipo)> Enviadas { get; } = new();

    public void Notificar(int usuarioId, TipoNotificacao tipo, string mensagem)
    {
        Enviadas.Add((usuarioId, tipo));
    }

    public List<Notificacao> Listar(int usuarioId)
    {
        return new List<Notificacao>();
    }

    public int ContarNaoLidas(int usuarioId)
    {
        return 0;
    }

    public Notificacao MarcarComoLida(int id, int usuarioId)
    {
        throw new NotSupportedException(
            "O dublê de notificações não implementa marcação de leitura.");
    }

    public void MarcarTodasComoLidas(int usuarioId)
    {
    }
}
