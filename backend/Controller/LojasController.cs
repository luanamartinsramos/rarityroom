using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/lojas")]
public class LojasController : ControllerBase
{
    private readonly IServicoLojas servicoLojas;

    private readonly IServicoImagens servicoImagens;

    public LojasController(
        IServicoLojas servicoLojas,
        IServicoImagens servicoImagens)
    {
        this.servicoLojas = servicoLojas;
        this.servicoImagens = servicoImagens;
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public IActionResult Obter(int id)
    {
        return Ok(servicoLojas.ObterPorId(id));
    }

    [HttpGet("minha")]
    [Authorize]
    public IActionResult Minha()
    {
        var loja = servicoLojas.ObterDoDono(User.ObterUsuarioId());

        if (loja == null)
        {
            throw new NaoEncontradoException(
                "Você ainda não criou uma loja.");
        }

        return Ok(loja);
    }

    [HttpPost]
    [Authorize]
    public IActionResult Criar(ModeloLoja modelo)
    {
        var loja = servicoLojas.Criar(
            User.ObterUsuarioId(),
            modelo.Nome,
            modelo.Descricao);

        return StatusCode(StatusCodes.Status201Created, loja);
    }

    [HttpPut]
    [Authorize]
    public IActionResult Atualizar(ModeloLoja modelo)
    {
        return Ok(servicoLojas.Atualizar(
            User.ObterUsuarioId(),
            modelo.Nome,
            modelo.Descricao));
    }

    /// <summary>
    /// Foto da loja: mesmo serviço de imagens dos anúncios (2 MB, JPG/PNG/WEBP,
    /// nome gerado pelo servidor).
    /// </summary>
    [HttpPost("foto")]
    [Authorize]
    public async Task<IActionResult> EnviarFoto(IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
        {
            throw new ValidacaoException("Envie um arquivo de imagem.");
        }

        await using Stream conteudo = arquivo.OpenReadStream();

        string caminho = await servicoImagens.SalvarAsync(
            conteudo,
            arquivo.ContentType,
            arquivo.Length);

        return Ok(servicoLojas.DefinirFoto(User.ObterUsuarioId(), caminho));
    }
}
