using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Loja do vendedor: uma por conta, criada apenas por quem pode vender.
/// </summary>
public class ServicoLojas : IServicoLojas
{
    private readonly IRepositorioLoja repositorioLoja;

    private readonly IRepositorioUsuario repositorioUsuario;

    public ServicoLojas(
        IRepositorioLoja repositorioLoja,
        IRepositorioUsuario repositorioUsuario)
    {
        this.repositorioLoja = repositorioLoja;
        this.repositorioUsuario = repositorioUsuario;
    }

    public Loja ObterPorId(int id)
    {
        var loja = repositorioLoja.ObterPorId(id);

        if (loja == null)
        {
            throw new NaoEncontradoException("Loja não encontrada.");
        }

        return loja;
    }

    public Loja? ObterDoDono(int donoId)
    {
        return repositorioLoja.ObterPorDono(donoId);
    }

    public Loja Criar(int donoId, string nome, string descricao)
    {
        var dono = repositorioUsuario.ObterPorId(donoId);

        if (dono == null)
        {
            throw new NaoEncontradoException("Usuário não encontrado.");
        }

        if (repositorioLoja.ObterPorDono(donoId) != null)
        {
            throw new ConflitoException("Você já possui uma loja.");
        }

        return repositorioLoja.Adicionar(new Loja(donoId, nome, descricao));
    }

    public Loja Atualizar(int donoId, string nome, string descricao)
    {
        var loja = ObterDoDonoOuFalhar(donoId);

        loja.Atualizar(nome, descricao);

        repositorioLoja.Atualizar(loja);

        return loja;
    }

    public Loja DefinirFoto(int donoId, string fotoUrl)
    {
        var loja = ObterDoDonoOuFalhar(donoId);

        loja.DefinirFoto(fotoUrl);

        repositorioLoja.Atualizar(loja);

        return loja;
    }

    private Loja ObterDoDonoOuFalhar(int donoId)
    {
        var loja = repositorioLoja.ObterPorDono(donoId);

        if (loja == null)
        {
            throw new NaoEncontradoException(
                "Você ainda não criou uma loja.");
        }

        return loja;
    }
}
