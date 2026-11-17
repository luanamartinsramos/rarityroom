using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Endereço de entrega do pedido. É um snapshot: mudar o cadastro depois não
/// reescreve o endereço de uma compra já feita.
/// </summary>
public record DadosEntrega(
    string Cep,
    string Logradouro,
    string Numero,
    string Complemento,
    string Bairro,
    string Cidade,
    string Estado)
{
    /// <summary>
    /// Valida e normaliza os campos. CEP aceita "00000-000" ou "00000000";
    /// a UF precisa ter duas letras.
    /// </summary>
    public static DadosEntrega Criar(
        string? cep,
        string? logradouro,
        string? numero,
        string? complemento,
        string? bairro,
        string? cidade,
        string? estado)
    {
        string cepLimpo = new((cep ?? string.Empty).Where(char.IsDigit).ToArray());

        if (cepLimpo.Length != 8)
        {
            throw new ValidacaoException("O CEP deve ter 8 dígitos.");
        }

        string logradouroLimpo = Obrigatorio(logradouro, "logradouro", 3, 120);
        string numeroLimpo = Obrigatorio(numero, "número", 1, 10);
        string bairroLimpo = Obrigatorio(bairro, "bairro", 2, 80);
        string cidadeLimpa = Obrigatorio(cidade, "cidade", 2, 80);

        string estadoLimpo = (estado ?? string.Empty).Trim().ToUpperInvariant();

        if (estadoLimpo.Length != 2 || !estadoLimpo.All(char.IsLetter))
        {
            throw new ValidacaoException(
                "O estado deve ser a sigla com duas letras (ex.: SP).");
        }

        string complementoLimpo = (complemento ?? string.Empty).Trim();

        if (complementoLimpo.Length > 80)
        {
            throw new ValidacaoException(
                "O complemento deve ter no máximo 80 caracteres.");
        }

        return new DadosEntrega(
            $"{cepLimpo[..5]}-{cepLimpo[5..]}",
            logradouroLimpo,
            numeroLimpo,
            complementoLimpo,
            bairroLimpo,
            cidadeLimpa,
            estadoLimpo);
    }

    private static string Obrigatorio(
        string? valor,
        string campo,
        int minimo,
        int maximo)
    {
        string texto = (valor ?? string.Empty).Trim();

        if (texto.Length < minimo)
        {
            throw new ValidacaoException(
                $"O {campo} do endereço é obrigatório.");
        }

        if (texto.Length > maximo)
        {
            throw new ValidacaoException(
                $"O {campo} do endereço deve ter no máximo {maximo} caracteres.");
        }

        return texto;
    }
}
