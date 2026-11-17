using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;

namespace RarityRoom.Servicos;

public interface IServicoNotificacoes
{
    void Notificar(int usuarioId, TipoNotificacao tipo, string mensagem);

    List<Notificacao> Listar(int usuarioId);

    int ContarNaoLidas(int usuarioId);

    Notificacao MarcarComoLida(int id, int usuarioId);

    void MarcarTodasComoLidas(int usuarioId);
}
