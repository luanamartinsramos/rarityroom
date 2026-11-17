using RarityRoom.Modelos;
using RarityRoom.Modelos.Admin;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Operações de moderação. Toda entrada passa por <see cref="GarantirAdmin"/>:
/// a autorização é verificada no serviço, não apenas no controller.
/// </summary>
public class ServicoAdmin : IServicoAdmin
{
    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly IRepositorioProduto repositorioProduto;

    private readonly IRepositorioPedido repositorioPedido;

    public ServicoAdmin(
        IRepositorioUsuario repositorioUsuario,
        IRepositorioProduto repositorioProduto,
        IRepositorioPedido repositorioPedido)
    {
        this.repositorioUsuario = repositorioUsuario;
        this.repositorioProduto = repositorioProduto;
        this.repositorioPedido = repositorioPedido;
    }

    public ResumoAdmin Resumo(int solicitanteId)
    {
        GarantirAdmin(solicitanteId);

        var usuarios = repositorioUsuario.ObterTodos();
        var anuncios = repositorioProduto.ObterTodos();
        var pedidos = repositorioPedido.ObterTodos();

        return new ResumoAdmin(
            usuarios.Count,
            usuarios.Count(usuario => usuario.Ativo),
            anuncios.Count(anuncio => anuncio.Estado == EstadoAnuncio.Disponivel),
            anuncios.Count(anuncio => anuncio.Estado == EstadoAnuncio.Vendido),
            pedidos.Count,
            pedidos
                .Where(pedido => pedido.StatusPagamento == StatusPagamento.Pago
                    && pedido.Estado != EstadoPedido.Cancelado)
                .Sum(pedido => pedido.Total));
    }

    public List<UsuarioAdmin> ListarUsuarios(int solicitanteId)
    {
        GarantirAdmin(solicitanteId);

        return repositorioUsuario
            .ObterTodos()
            .Select(usuario => new UsuarioAdmin(
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.TipoConta.ToString(),
                usuario.Ativo,
                usuario.Administrador,
                usuario.CriadoEm))
            .ToList();
    }

    public void SuspenderUsuario(int id, int solicitanteId)
    {
        var solicitante = GarantirAdmin(solicitanteId);

        if (solicitante.Id == id)
        {
            throw new ConflitoException(
                "Um administrador não pode suspender a própria conta.");
        }

        var usuario = ObterUsuario(id);

        usuario.Desativar();

        repositorioUsuario.Atualizar(usuario);
    }

    public void ReativarUsuario(int id, int solicitanteId)
    {
        GarantirAdmin(solicitanteId);

        var usuario = ObterUsuario(id);

        usuario.Ativar();

        repositorioUsuario.Atualizar(usuario);
    }

    public List<Produto> ListarAnuncios(int solicitanteId)
    {
        GarantirAdmin(solicitanteId);

        return repositorioProduto.ObterTodos();
    }

    public Produto ModerarAnuncio(int id, string acao, int solicitanteId)
    {
        GarantirAdmin(solicitanteId);

        var produto = repositorioProduto.ObterPorId(id);

        if (produto == null)
        {
            throw new NaoEncontradoException("Anúncio não encontrado.");
        }

        switch ((acao ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "pausar":
                produto.Pausar();

                repositorioProduto.Atualizar(produto);

                return produto;

            case "remover":
                repositorioProduto.Remover(id);

                return produto;

            default:
                throw new ValidacaoException(
                    "Ação inválida. Use pausar ou remover.");
        }
    }

    public List<Pedido> ListarPedidos(int solicitanteId)
    {
        GarantirAdmin(solicitanteId);

        return repositorioPedido.ObterTodos();
    }

    private Usuario GarantirAdmin(int solicitanteId)
    {
        var solicitante = repositorioUsuario.ObterPorId(solicitanteId);

        if (solicitante == null || !solicitante.Administrador)
        {
            throw new AcessoNegadoException(
                "Área restrita a administradores.");
        }

        return solicitante;
    }

    private Usuario ObterUsuario(int id)
    {
        var usuario = repositorioUsuario.ObterPorId(id);

        if (usuario == null)
        {
            throw new NaoEncontradoException("Usuário não encontrado.");
        }

        return usuario;
    }
}
