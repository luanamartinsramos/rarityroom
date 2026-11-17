using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

public class ServicoNotificacoes : IServicoNotificacoes
{
    private readonly IRepositorioNotificacao repositorioNotificacao;

    public ServicoNotificacoes(IRepositorioNotificacao repositorioNotificacao)
    {
        this.repositorioNotificacao = repositorioNotificacao;
    }

    /// <summary>
    /// Registra o aviso. Uma notificação nunca derruba a operação que a
    /// originou: se o destinatário for inválido, o erro é do chamador.
    /// </summary>
    public void Notificar(int usuarioId, TipoNotificacao tipo, string mensagem)
    {
        var notificacao = new Notificacao(usuarioId, tipo, mensagem);

        repositorioNotificacao.Adicionar(notificacao);
    }

    public List<Notificacao> Listar(int usuarioId)
    {
        return repositorioNotificacao.ObterPorUsuario(usuarioId);
    }

    public int ContarNaoLidas(int usuarioId)
    {
        return repositorioNotificacao.ContarNaoLidas(usuarioId);
    }

    public Notificacao MarcarComoLida(int id, int usuarioId)
    {
        var notificacao = repositorioNotificacao.ObterPorId(id);

        if (notificacao == null || notificacao.UsuarioId != usuarioId)
        {
            throw new NaoEncontradoException("Notificação não encontrada.");
        }

        notificacao.MarcarComoLida();

        repositorioNotificacao.Atualizar(new[] { notificacao });

        return notificacao;
    }

    public void MarcarTodasComoLidas(int usuarioId)
    {
        var pendentes = repositorioNotificacao
            .ObterPorUsuario(usuarioId)
            .Where(notificacao => !notificacao.Lida)
            .ToList();

        foreach (var notificacao in pendentes)
        {
            notificacao.MarcarComoLida();
        }

        repositorioNotificacao.Atualizar(pendentes);
    }
}
