using System.Globalization;
using System.Text;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Categoria do catálogo geek. O slug é derivado do nome (sem acentos, em
/// minúsculas), o que torna "Colecionáveis", "colecionaveis" e
/// "COLECIONÁVEIS" a mesma categoria — comparação por texto puro criaria
/// três categorias distintas.
/// </summary>
public class Categoria
{
    private const int TamanhoMinimoNome = 2;
    private const int TamanhoMaximoNome = 60;

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    /// <summary>Exigido pelo EF Core para materializar a entidade do banco.</summary>
    private Categoria()
    {
    }

    public Categoria(string nome)
    {
        string texto = (nome ?? string.Empty).Trim();

        if (texto.Length < TamanhoMinimoNome)
        {
            throw new ValidacaoException(
                $"A categoria deve ter pelo menos {TamanhoMinimoNome} caracteres.");
        }

        if (texto.Length > TamanhoMaximoNome)
        {
            throw new ValidacaoException(
                $"A categoria deve ter no máximo {TamanhoMaximoNome} caracteres.");
        }

        string slug = GerarSlug(texto);

        if (slug.Length == 0)
        {
            throw new ValidacaoException(
                "A categoria precisa conter ao menos uma letra ou número.");
        }

        Nome = texto;
        Slug = slug;
    }

    /// <summary>
    /// Normaliza o nome: remove acentos, converte para minúsculas e troca
    /// qualquer sequência de símbolos por um único hífen.
    /// </summary>
    public static string GerarSlug(string? nome)
    {
        string texto = (nome ?? string.Empty).Trim();

        if (texto.Length == 0)
        {
            return string.Empty;
        }

        string decomposto = texto.Normalize(NormalizationForm.FormD);

        var semAcento = new StringBuilder(decomposto.Length);

        foreach (char caractere in decomposto)
        {
            bool ehAcento = CharUnicodeInfo.GetUnicodeCategory(caractere)
                == UnicodeCategory.NonSpacingMark;

            if (!ehAcento)
            {
                semAcento.Append(char.ToLowerInvariant(caractere));
            }
        }

        string normalizado = semAcento.ToString().Normalize(NormalizationForm.FormC);

        var slug = new StringBuilder(normalizado.Length);
        bool ultimoEhHifen = false;

        foreach (char caractere in normalizado)
        {
            if (char.IsLetterOrDigit(caractere))
            {
                slug.Append(caractere);
                ultimoEhHifen = false;
            }
            else if (!ultimoEhHifen)
            {
                slug.Append('-');
                ultimoEhHifen = true;
            }
        }

        return slug.ToString().Trim('-');
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador da categoria deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador da categoria já foi definido.");
        }

        Id = id;
    }
}
