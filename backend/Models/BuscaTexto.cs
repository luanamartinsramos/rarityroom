using System.Globalization;
using System.Text;

namespace RarityRoom.Modelos;

/// <summary>
/// Comparação de texto tolerante a acento e caixa. "Colecionáveis" e
/// "colecionaveis" precisam ser o mesmo termo de busca.
/// </summary>
public static class BuscaTexto
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        string decomposto = texto.Trim().Normalize(NormalizationForm.FormD);

        var semAcento = new StringBuilder(decomposto.Length);

        foreach (char caractere in decomposto)
        {
            bool ehAcento = CharUnicodeInfo.GetUnicodeCategory(caractere)
                == UnicodeCategory.NonSpacingMark;

            if (!ehAcento)
            {
                semAcento.Append(caractere);
            }
        }

        return semAcento
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    public static bool Contem(string? texto, string termoNormalizado)
    {
        if (string.IsNullOrEmpty(termoNormalizado))
        {
            return true;
        }

        return Normalizar(texto).Contains(termoNormalizado, StringComparison.Ordinal);
    }

    public static bool Igual(string? texto, string valorNormalizado)
    {
        return Normalizar(texto) == valorNormalizado;
    }
}
