using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioNotificacaoEf : IRepositorioNotificacao
{
    private readonly RarityRoomContexto contexto;

    public RepositorioNotificacaoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public List<Notificacao> ObterPorUsuario(int usuarioId)
    {
        return contexto.Notificacoes
            .AsNoTracking()
            .Where(notificacao => notificacao.UsuarioId == usuarioId)
            .OrderByDescending(notificacao => notificacao.CriadaEm)
            .Take(50)
            .ToList();
    }

    public Notificacao? ObterPorId(int id)
    {
        return contexto.Notificacoes.FirstOrDefault(
            notificacao => notificacao.Id == id);
    }

    public int ContarNaoLidas(int usuarioId)
    {
        return contexto.Notificacoes.Count(
            notificacao => notificacao.UsuarioId == usuarioId
                && !notificacao.Lida);
    }

    public Notificacao Adicionar(Notificacao notificacao)
    {
        ArgumentNullException.ThrowIfNull(notificacao);

        contexto.Notificacoes.Add(notificacao);
        contexto.SaveChanges();

        return notificacao;
    }

    public void Atualizar(IEnumerable<Notificacao> notificacoes)
    {
        ArgumentNullException.ThrowIfNull(notificacoes);

        var lista = notificacoes.ToList();

        if (lista.Count == 0)
        {
            return;
        }

        contexto.Notificacoes.UpdateRange(lista);
        contexto.SaveChanges();
    }
}
