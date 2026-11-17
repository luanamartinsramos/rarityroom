using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Monta a vitrine pública de um vendedor. A reputação é derivada das
/// avaliações — não existe nota armazenada para ficar fora de sincronia.
/// </summary>
public class ServicoVendedores : IServicoVendedores
{
    private readonly IRepositorioUsuario repositorioUsuario;

    private readonly IRepositorioProduto repositorioProduto;

    private readonly IRepositorioAvaliacao repositorioAvaliacao;

    private readonly IRepositorioLoja repositorioLoja;

    public ServicoVendedores(
        IRepositorioUsuario repositorioUsuario,
        IRepositorioProduto repositorioProduto,
        IRepositorioAvaliacao repositorioAvaliacao,
        IRepositorioLoja repositorioLoja)
    {
        this.repositorioUsuario = repositorioUsuario;
        this.repositorioProduto = repositorioProduto;
        this.repositorioAvaliacao = repositorioAvaliacao;
        this.repositorioLoja = repositorioLoja;
    }

    public PerfilVendedor Obter(int vendedorId)
    {
        var usuario = repositorioUsuario.ObterPorId(vendedorId);

        if (usuario == null)
        {
            throw new NaoEncontradoException("Vendedor não encontrado.");
        }

        var anuncios = repositorioProduto
            .ObterTodos()
            .Where(produto => produto.UsuarioId == vendedorId)
            .ToList();

        var avaliacoes = repositorioAvaliacao.ObterPorVendedor(vendedorId);

        decimal media = avaliacoes.Count == 0
            ? 0m
            : Math.Round(
                (decimal)avaliacoes.Average(avaliacao => avaliacao.Nota),
                2);

        return new PerfilVendedor(
            usuario.Id,
            usuario.Nome,
            usuario.TipoConta.ToString(),
            usuario.CriadoEm,
            media,
            avaliacoes.Count,
            anuncios.Count(produto => produto.Estado == EstadoAnuncio.Disponivel),
            anuncios.Count(produto => produto.Estado == EstadoAnuncio.Vendido),
            avaliacoes,
            anuncios,
            MontarLoja(repositorioLoja.ObterPorDono(vendedorId)));
    }

    private static LojaResumo? MontarLoja(Loja? loja)
    {
        return loja == null
            ? null
            : new LojaResumo(loja.Id, loja.Nome, loja.Descricao, loja.FotoUrl);
    }
}
