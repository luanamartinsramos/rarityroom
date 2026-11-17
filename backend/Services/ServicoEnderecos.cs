using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Endereços da conta. O primeiro vira principal; ao remover o principal, o
/// mais antigo restante assume.
/// </summary>
public class ServicoEnderecos : IServicoEnderecos
{
    private const int LimitePorUsuario = 5;

    private readonly IRepositorioEndereco repositorioEndereco;

    public ServicoEnderecos(IRepositorioEndereco repositorioEndereco)
    {
        this.repositorioEndereco = repositorioEndereco;
    }

    public List<Endereco> Listar(int usuarioId)
    {
        return repositorioEndereco.ObterPorUsuario(usuarioId);
    }

    public Endereco Criar(int usuarioId, DadosEntrega dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        if (repositorioEndereco.ContarPorUsuario(usuarioId) >= LimitePorUsuario)
        {
            throw new ConflitoException(
                $"Você pode manter até {LimitePorUsuario} endereços.");
        }

        bool primeiro = repositorioEndereco.ContarPorUsuario(usuarioId) == 0;

        return repositorioEndereco.Adicionar(
            new Endereco(usuarioId, dados, primeiro));
    }

    public Endereco Atualizar(int id, int usuarioId, DadosEntrega dados)
    {
        var endereco = ObterDoUsuario(id, usuarioId);

        endereco.Atualizar(dados);

        repositorioEndereco.Atualizar(endereco);

        return endereco;
    }

    public void Remover(int id, int usuarioId)
    {
        var endereco = ObterDoUsuario(id, usuarioId);

        repositorioEndereco.Remover(endereco.Id);

        if (!endereco.Principal)
        {
            return;
        }

        var restantes = repositorioEndereco.ObterPorUsuario(usuarioId);

        if (restantes.Count > 0)
        {
            var novoPrincipal = restantes[0];

            novoPrincipal.DefinirPrincipal(true);

            repositorioEndereco.Atualizar(novoPrincipal);
        }
    }

    public Endereco DefinirPrincipal(int id, int usuarioId)
    {
        var escolhido = ObterDoUsuario(id, usuarioId);

        foreach (var endereco in repositorioEndereco.ObterPorUsuario(usuarioId))
        {
            bool principal = endereco.Id == escolhido.Id;

            if (endereco.Principal != principal)
            {
                endereco.DefinirPrincipal(principal);

                repositorioEndereco.Atualizar(endereco);
            }
        }

        return escolhido;
    }

    private Endereco ObterDoUsuario(int id, int usuarioId)
    {
        var endereco = repositorioEndereco.ObterPorId(id);

        if (endereco == null || endereco.UsuarioId != usuarioId)
        {
            throw new NaoEncontradoException("Endereço não encontrado.");
        }

        return endereco;
    }
}
