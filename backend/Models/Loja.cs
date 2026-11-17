using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Vitrine do vendedor. Cada conta pode ter no máximo uma loja; o nome e a
/// descrição são públicos e a foto usa o mesmo serviço de imagens dos anúncios.
/// </summary>
public class Loja
{
    private const int TamanhoMinimoNome = 3;
    private const int TamanhoMaximoNome = 80;
    private const int TamanhoMaximoDescricao = 500;
    private const int TamanhoMaximoFoto = 500;

    public int Id { get; private set; }

    public int DonoId { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Descricao { get; private set; } = string.Empty;

    public string FotoUrl { get; private set; } = string.Empty;

    public DateTime CriadaEm { get; private set; }

    private Loja()
    {
    }

    public Loja(int donoId, string nome, string descricao)
    {
        if (donoId <= 0)
        {
            throw new ValidacaoException(
                "A loja precisa estar vinculada a um vendedor.");
        }

        DonoId = donoId;
        CriadaEm = DateTime.UtcNow;

        AplicarDados(nome, descricao);
    }

    public void Atualizar(string nome, string descricao)
    {
        AplicarDados(nome, descricao);
    }

    public void DefinirFoto(string fotoUrl)
    {
        FotoUrl = FotoValida(fotoUrl);
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador da loja deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador da loja já foi definido.");
        }

        Id = id;
    }

    private void AplicarDados(string nome, string descricao)
    {
        string nomeLimpo = (nome ?? string.Empty).Trim();

        if (nomeLimpo.Length < TamanhoMinimoNome)
        {
            throw new ValidacaoException(
                $"O nome da loja deve ter pelo menos {TamanhoMinimoNome} caracteres.");
        }

        if (nomeLimpo.Length > TamanhoMaximoNome)
        {
            throw new ValidacaoException(
                $"O nome da loja deve ter no máximo {TamanhoMaximoNome} caracteres.");
        }

        string descricaoLimpa = (descricao ?? string.Empty).Trim();

        if (descricaoLimpa.Length > TamanhoMaximoDescricao)
        {
            throw new ValidacaoException(
                $"A descrição da loja deve ter no máximo {TamanhoMaximoDescricao} caracteres.");
        }

        Nome = nomeLimpo;
        Descricao = descricaoLimpa;
    }

    private static string FotoValida(string? fotoUrl)
    {
        string texto = (fotoUrl ?? string.Empty).Trim();

        if (texto.Length == 0)
        {
            return string.Empty;
        }

        if (texto.Length > TamanhoMaximoFoto)
        {
            throw new ValidacaoException(
                $"A foto da loja deve ter no máximo {TamanhoMaximoFoto} caracteres.");
        }

        if (texto.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            return texto;
        }

        bool urlAbsoluta = Uri.TryCreate(texto, UriKind.Absolute, out Uri? uri);

        if (!urlAbsoluta
            || uri == null
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidacaoException(
                "A foto da loja deve ser um endereço http ou https válido.");
        }

        return texto;
    }
}
