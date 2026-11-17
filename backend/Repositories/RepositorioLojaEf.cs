using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioLojaEf : IRepositorioLoja
{
    private readonly RarityRoomContexto contexto;

    public RepositorioLojaEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public Loja? ObterPorId(int id)
    {
        return contexto.Lojas.FirstOrDefault(loja => loja.Id == id);
    }

    public Loja? ObterPorDono(int donoId)
    {
        return contexto.Lojas.FirstOrDefault(loja => loja.DonoId == donoId);
    }

    public List<Loja> ObterTodas()
    {
        return contexto.Lojas
            .AsNoTracking()
            .OrderBy(loja => loja.Nome)
            .ToList();
    }

    public Loja Adicionar(Loja loja)
    {
        ArgumentNullException.ThrowIfNull(loja);

        contexto.Lojas.Add(loja);
        contexto.SaveChanges();

        return loja;
    }

    public bool Atualizar(Loja loja)
    {
        ArgumentNullException.ThrowIfNull(loja);

        bool existe = contexto.Lojas.Any(item => item.Id == loja.Id);

        if (!existe)
        {
            return false;
        }

        contexto.Lojas.Update(loja);
        contexto.SaveChanges();

        return true;
    }
}
