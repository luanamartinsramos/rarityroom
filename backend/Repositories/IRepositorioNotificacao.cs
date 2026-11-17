using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioNotificacao
{
    List<Notificacao> ObterPorUsuario(int usuarioId);

    Notificacao? ObterPorId(int id);

    int ContarNaoLidas(int usuarioId);

    Notificacao Adicionar(Notificacao notificacao);

    void Atualizar(IEnumerable<Notificacao> notificacoes);
}
