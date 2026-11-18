using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Servicos;

/// <summary>
/// Armazena a imagem em disco e devolve o caminho público. O nome do arquivo é
/// gerado pelo servidor e a extensão vem do tipo de conteúdo declarado — nunca
/// do nome enviado pelo cliente, o que elimina path traversal e extensão
/// forjada. Além do tipo declarado, os primeiros bytes do arquivo precisam
/// ter a assinatura do formato: um HTML enviado como "image/png" é recusado.
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

        // Lê no máximo 1 byte além do limite: o tamanho declarado pode mentir,
        // e um arquivo gigante não deve ser carregado inteiro na memória.
        using var memoria = new MemoryStream();

        byte[] bloco = new byte[81920];
        int lidos;

        while (memoria.Length <= TamanhoMaximoBytes
            && (lidos = await conteudo.ReadAsync(
                bloco.AsMemory(0, (int)Math.Min(bloco.Length, TamanhoMaximoBytes + 1 - memoria.Length)))) > 0)
        {
            memoria.Write(bloco, 0, lidos);
        }

        if (memoria.Length > TamanhoMaximoBytes)
        {
            throw new ValidacaoException(
                $"A imagem deve ter no máximo {TamanhoMaximoBytes / (1024 * 1024)} MB.");
        }

        if (!AssinaturaConfere(memoria.GetBuffer().AsSpan(0, (int)memoria.Length), extensao))
        {
            throw new ValidacaoException(
                "O conteúdo do arquivo não é uma imagem JPG, PNG ou WEBP válida.");
        }

        Directory.CreateDirectory(diretorio);

        string nome = $"{Guid.NewGuid():N}{extensao}";
        string caminho = Path.Combine(diretorio, nome);

        memoria.Position = 0;

        await using (var destino = File.Create(caminho))
        {
            await memoria.CopyToAsync(destino);
        }

        return $"/uploads/{nome}";
    }

    /// <summary>
    /// Confere os "magic bytes" do formato:
    /// JPEG FF D8 FF · PNG 89 50 4E 47 0D 0A 1A 0A · WEBP "RIFF" ???? "WEBP".
    /// </summary>
    private static bool AssinaturaConfere(ReadOnlySpan<byte> bytes, string extensao)
    {
        return extensao switch
        {
            ".jpg" => bytes.Length >= 3
                && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,

            ".png" => bytes.Length >= 8
                && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),

            ".webp" => bytes.Length >= 12
                && bytes[..4].SequenceEqual("RIFF"u8)
                && bytes[8..12].SequenceEqual("WEBP"u8),

            _ => false
        };
    }
}
