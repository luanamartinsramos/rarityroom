using Microsoft.EntityFrameworkCore;
using RarityRoom.Dados;
using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public class RepositorioEnderecoEf : IRepositorioEndereco
{
    private readonly RarityRoomContexto contexto;

    public RepositorioEnderecoEf(RarityRoomContexto contexto)
    {
        this.contexto = contexto;
    }

    public List<Endereco> ObterPorUsuario(int usuarioId)
    {
        return contexto.Enderecos
            .Where(endereco => endereco.UsuarioId == usuarioId)
            .OrderByDescending(endereco => endereco.Principal)
            .ThenBy(endereco => endereco.Id)
            .ToList();
    }

    public Endereco? ObterPorId(int id)
    {
        return contexto.Enderecos.FirstOrDefault(endereco => endereco.Id == id);
    }

    public int ContarPorUsuario(int usuarioId)
    {
        return contexto.Enderecos.Count(
            endereco => endereco.UsuarioId == usuarioId);
    }

    public Endereco Adicionar(Endereco endereco)
    {
        ArgumentNullException.ThrowIfNull(endereco);

        contexto.Enderecos.Add(endereco);
        contexto.SaveChanges();

        return endereco;
    }

    public bool Atualizar(Endereco endereco)
    {
        ArgumentNullException.ThrowIfNull(endereco);

        bool existe = contexto.Enderecos.Any(item => item.Id == endereco.Id);

        if (!existe)
        {
            return false;
        }

        contexto.Enderecos.Update(endereco);
        contexto.SaveChanges();

        return true;
    }

    public bool Remover(int id)
    {
        var endereco = contexto.Enderecos.FirstOrDefault(item => item.Id == id);

        if (endereco == null)
        {
            return false;
        }

        contexto.Enderecos.Remove(endereco);
        contexto.SaveChanges();

        return true;
    }
}
