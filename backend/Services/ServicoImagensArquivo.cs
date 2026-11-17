using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Servicos;

/// <summary>
/// Armazena a imagem em disco e devolve o caminho público. O nome do arquivo é
/// gerado pelo servidor e a extensão vem do tipo de conteúdo declarado — nunca
/// do nome enviado pelo cliente, o que elimina path traversal e extensão
/// forjada.
/// </summary>
public class ServicoImagensArquivo : IServicoImagens
{
    public const long TamanhoMaximoBytes = 2 * 1024 * 1024;

    private static readonly Dictionary<string, string> ExtensoesPorTipo =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private readonly string diretorio;

    public ServicoImagensArquivo(string diretorio)
    {
        this.diretorio = diretorio;
    }

    public async Task<string> SalvarAsync(
        Stream conteudo,
        string tipoConteudo,
        long tamanho)
    {
        if (conteudo == null)
        {
            throw new ValidacaoException("Nenhum arquivo foi enviado.");
        }

        if (tamanho <= 0)
        {
            throw new ValidacaoException("O arquivo está vazio.");
        }

        if (tamanho > TamanhoMaximoBytes)
        {
            throw new ValidacaoException(
                $"A imagem deve ter no máximo {TamanhoMaximoBytes / (1024 * 1024)} MB.");
        }

        if (!ExtensoesPorTipo.TryGetValue(
                tipoConteudo ?? string.Empty,
                out string? extensao))
        {
            throw new ValidacaoException(
                "Formato não suportado. Envie JPG, PNG ou WEBP.");
        }

        Directory.CreateDirectory(diretorio);

        string nome = $"{Guid.NewGuid():N}{extensao}";
        string caminho = Path.Combine(diretorio, nome);

        await using (var destino = File.Create(caminho))
        {
            await conteudo.CopyToAsync(destino);
        }

        return $"/uploads/{nome}";
    }
}
