using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioRecuperacaoSenhaEf : IRepositorioRecuperacaoSenha
{
    private readonly RarityRoomContexto contexto;

    public RepositorioRecuperacaoSenhaEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public RecuperacaoSenha Adicionar(RecuperacaoSenha recuperacao)
    {
        ArgumentNullException.ThrowIfNull(recuperacao);

        contexto.RecuperacoesSenha.Add(recuperacao);
        contexto.SaveChanges();

        return recuperacao;
    }

    public RecuperacaoSenha? ObterPorToken(string token)
    {
        string procurado = (token ?? string.Empty).Trim();

        if (procurado.Length == 0)
        {
            return null;
        }

        return contexto.RecuperacoesSenha
            .Where(recuperacao => recuperacao.Token == procurado)
            .OrderByDescending(recuperacao => recuperacao.Id)
            .FirstOrDefault();
    }

    public bool Atualizar(RecuperacaoSenha recuperacao)
    {
        ArgumentNullException.ThrowIfNull(recuperacao);

        contexto.RecuperacoesSenha.Update(recuperacao);
        contexto.SaveChanges();

        return true;
    }

    /// <summary>
    /// Um novo pedido invalida os tokens anteriores do mesmo usuário.
    /// </summary>
    public void InvalidarDoUsuario(int usuarioId)
    {
        var pendentes = contexto.RecuperacoesSenha
            .Where(recuperacao => recuperacao.UsuarioId == usuarioId
                && !recuperacao.Usada)
            .ToList();

        foreach (var recuperacao in pendentes)
        {
            recuperacao.MarcarComoUsada();
        }

        if (pendentes.Count > 0)
        {
            contexto.RecuperacoesSenha.UpdateRange(pendentes);
            contexto.SaveChanges();
        }
    }
}
