using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Servicos;

namespace RarityRoom.Controllers;

[ApiController]
[Route("api/perfil")]
[Authorize]
public class PerfilController : ControllerBase
{
    private readonly IServicoPerfil servicoPerfil;

    private readonly IServicoImagens servicoImagens;

    public PerfilController(
        IServicoPerfil servicoPerfil,
        IServicoImagens servicoImagens)
    {
        this.servicoPerfil = servicoPerfil;
        this.servicoImagens = servicoImagens;
    }

    [HttpGet]
    public IActionResult Obter()
    {
        return Ok(servicoPerfil.Obter(User.ObterUsuarioId()));
    }

    [HttpPut("nome")]
    public IActionResult AlterarNome(ModeloNome modelo)
    {
        return Ok(servicoPerfil.AtualizarNome(
            User.ObterUsuarioId(),
            modelo.Nome));
    }

    [HttpPut("email")]
    public IActionResult AlterarEmail(ModeloEmail modelo)
    {
        return Ok(servicoPerfil.AtualizarEmail(
            User.ObterUsuarioId(),
            modelo.Email,
            modelo.SenhaAtual));
    }

    /// <summary>Foto de perfil: mesmo serviço de imagens dos anúncios.</summary>
    [HttpPost("foto")]
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

        return Ok(servicoPerfil.DefinirFoto(
            User.ObterUsuarioId(),
            caminho));
    }
}
