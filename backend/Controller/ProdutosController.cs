using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IServicoAnuncios servicoAnuncios;

    private readonly IServicoImagens servicoImagens;

    public ProdutosController(
        IServicoAnuncios servicoAnuncios,
        IServicoImagens servicoImagens)
    {
        this.servicoAnuncios = servicoAnuncios;
        this.servicoImagens = servicoImagens;
    }

    /// <summary>
    /// Vitrine pública com pesquisa. Sem parâmetros devolve o catálogo
    /// completo; com parâmetros filtra no servidor.
    /// </summary>
    [HttpGet]
    public IActionResult ObterTodos(
        [FromQuery] string? termo,
        [FromQuery] string? categoria,
        [FromQuery] string? estado,
        [FromQuery] string? franquia,
        [FromQuery] string? condicao,
        [FromQuery] decimal? precoMin,
        [FromQuery] decimal? precoMax,
        [FromQuery] string? ordem,
        [FromQuery] bool? destaque,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 0)
    {
        var filtro = new FiltroAnuncios(
            termo,
            categoria,
            estado,
            franquia,
            condicao,
            precoMin,
            precoMax,
            ordem,
            destaque,
            pagina,
            tamanhoPagina);

        // Sempre devolve os itens com vendedor, vendas e avaliações reais; o
        // total filtrado vai no cabeçalho para a paginação do cliente.
        var resultado = servicoAnuncios.ListarPaginado(filtro);

        Response.Headers["X-Total-Count"] = resultado.Total.ToString();

        return Ok(resultado.Itens);
    }

    /// <summary>Anúncios do usuário autenticado, em qualquer estado.</summary>
    [HttpGet("meus")]
    [Authorize]
    public IActionResult Meus()
    {
        return Ok(servicoAnuncios.ListarDoUsuario(User.ObterUsuarioId()));
    }

    [HttpGet("{id:int}")]
    public IActionResult ObterPorId(int id)
    {
        return Ok(servicoAnuncios.ObterDetalhe(id));
    }

    [HttpPost]
    [Authorize]
    public IActionResult Criar(ModeloProduto modelo)
    {
        var produto = servicoAnuncios.Criar(
            ParaDados(modelo),
            User.ObterUsuarioId());

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = produto.Id },
            produto
        );
    }

    [HttpPut("{id}")]
    [Authorize]
    public IActionResult Atualizar(int id, ModeloProduto modelo)
    {
        var produto = servicoAnuncios.Atualizar(
            id,
            ParaDados(modelo),
            User.ObterUsuarioId());

        return Ok(produto);
    }

    [HttpPatch("{id}/estado")]
    [Authorize]
    public IActionResult AlterarEstado(int id, ModeloEstadoAnuncio modelo)
    {
        var produto = servicoAnuncios.AlterarEstado(
            id,
            OperacaoDe(modelo.Operacao),
            User.ObterUsuarioId());

        return Ok(produto);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public IActionResult Remover(int id)
    {
        servicoAnuncios.Remover(id, User.ObterUsuarioId());

        return Ok(new
        {
            mensagem = "Produto removido com sucesso."
        });
    }

    /// <summary>
    /// Envia a imagem do anúncio. O arquivo é validado por tamanho e tipo, e o
    /// nome final é gerado pelo servidor.
    /// </summary>
    [HttpPost("{id}/imagem")]
    [Authorize]
    public async Task<IActionResult> EnviarImagem(int id, IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
        {
            throw new ValidacaoException("Envie um arquivo de imagem.");
        }

        // Permissão antes de tocar no disco: upload recusado não deixa arquivo.
        servicoAnuncios.GarantirDono(id, User.ObterUsuarioId());

        await using Stream conteudo = arquivo.OpenReadStream();

        string caminho = await servicoImagens.SalvarAsync(
            conteudo,
            arquivo.ContentType,
            arquivo.Length);

        var produto = servicoAnuncios.DefinirImagem(
            id,
            caminho,
            User.ObterUsuarioId());

        return Ok(produto);
    }

    private static DadosAnuncio ParaDados(ModeloProduto modelo)
    {
        return new DadosAnuncio(
            modelo.Nome,
            modelo.Descricao,
            modelo.Preco,
            modelo.Estoque,
            modelo.Categoria,
            modelo.Marca,
            modelo.Colecao,
            modelo.Condicao,
            modelo.ImagemUrl,
            ModalidadeDe(modelo.Modalidade),
            modelo.Franquia,
            RaridadeDe(modelo.Raridade),
            modelo.EdicaoLimitada,
            modelo.Destaque
        );
    }

    private static OperacaoAnuncio OperacaoDe(string operacao)
    {
        bool valida = Enum.TryParse(
            operacao,
            ignoreCase: true,
            out OperacaoAnuncio resultado);

        if (!valida || !Enum.IsDefined(resultado))
        {
            throw new ValidacaoException(
                "Operação inválida. Use reservar, vender, pausar, reativar ou cancelarReserva.");
        }

        return resultado;
    }

    private static ModalidadeNegociacao ModalidadeDe(string modalidade)
    {
        if (string.IsNullOrWhiteSpace(modalidade))
        {
            return ModalidadeNegociacao.PrecoFixo;
        }

        bool valida = Enum.TryParse(
            modalidade,
            ignoreCase: true,
            out ModalidadeNegociacao resultado);

        if (!valida || !Enum.IsDefined(resultado))
        {
            throw new ValidacaoException(
                "Modalidade inválida. Use PrecoFixo, AceitaOferta ou Liquidacao.");
        }

        return resultado;
    }

    private static Raridade RaridadeDe(string raridade)
    {
        if (string.IsNullOrWhiteSpace(raridade))
        {
            return Raridade.Comum;
        }

        bool valida = Enum.TryParse(
            raridade,
            ignoreCase: true,
            out Raridade resultado);

        if (!valida || !Enum.IsDefined(resultado))
        {
            throw new ValidacaoException(
                "Raridade inválida. Use Comum, Raro, Epico ou Lendario.");
        }

        return resultado;
    }
}
