using System.Globalization;
using RarityRoom.Modelos;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Servicos;

/// <summary>
/// Validação e decisão do cartão de crédito SIMULADO. Nada aqui consulta
/// banco ou operadora. Regra da simulação: qualquer cartão válido é aprovado,
/// exceto os números de teste de recusa abaixo.
/// </summary>
public static class ValidadorCartao
{
    /// <summary>Cartões de teste que a simulação recusa.</summary>
    public static readonly IReadOnlyDictionary<string, string> NumerosRecusados =
        new Dictionary<string, string>
        {
            ["4000000000000002"] = "Pagamento recusado pela operadora (simulação).",
            ["4000000000009995"] = "Saldo/limite insuficiente (simulação)."
        };

    public record CartaoValidado(string Final, string Bandeira, int Parcelas, string? MotivoRecusa);

    public static CartaoValidado Validar(ModeloCartao cartao, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(cartao);

        var erros = new List<string>();

        string nome = (cartao.NomeImpresso ?? string.Empty).Trim();

        if (nome.Length < 3 || !nome.Any(char.IsLetter))
        {
            erros.Add("Informe o nome impresso no cartão.");
        }

        string numero = new((cartao.Numero ?? string.Empty).Where(char.IsDigit).ToArray());
        string bandeira = Bandeira(numero);

        if (numero.Length < 13 || numero.Length > 19 || !Luhn(numero))
        {
            erros.Add("Número do cartão inválido.");
        }

        if (!ValidadeOk(cartao.Validade, agora, out string erroValidade))
        {
            erros.Add(erroValidade);
        }

        string cvv = (cartao.Cvv ?? string.Empty).Trim();
        int tamanhoCvv = bandeira == "Amex" ? 4 : 3;

        if (cvv.Length != tamanhoCvv || !cvv.All(char.IsDigit))
        {
            erros.Add($"O código de segurança (CVV) deve ter {tamanhoCvv} dígitos.");
        }

        string cpf = new((cartao.Cpf ?? string.Empty).Where(char.IsDigit).ToArray());

        if (cpf.Length > 0 && !CpfValido(cpf))
        {
            erros.Add("CPF do titular inválido.");
        }

        if (cartao.Parcelas < 1 || cartao.Parcelas > Pagamento.MaximoParcelas)
        {
            erros.Add($"Escolha entre 1 e {Pagamento.MaximoParcelas} parcelas.");
        }

        if (erros.Count > 0)
        {
            throw new ValidacaoException(string.Join(" ", erros));
        }

        NumerosRecusados.TryGetValue(numero, out string? recusa);

        return new CartaoValidado(numero[^4..], bandeira, cartao.Parcelas, recusa);
    }

    public static bool Luhn(string digitos)
    {
        int soma = 0;
        bool dobrar = false;

        for (int i = digitos.Length - 1; i >= 0; i--)
        {
            int valor = digitos[i] - '0';

            if (dobrar)
            {
                valor *= 2;

                if (valor > 9)
                {
                    valor -= 9;
                }
            }

            soma += valor;
            dobrar = !dobrar;
        }

        return soma % 10 == 0;
    }

    public static string Bandeira(string numero)
    {
        if (numero.StartsWith("34") || numero.StartsWith("37"))
        {
            return "Amex";
        }

        string[] elo = { "4011", "4389", "4514", "4576", "5041", "5066", "5067", "6362", "6363", "6504", "6505", "6516", "6550" };

        if (elo.Any(numero.StartsWith))
        {
            return "Elo";
        }

        if (numero.StartsWith("6062"))
        {
            return "Hipercard";
        }

        if (numero.StartsWith('4'))
        {
            return "Visa";
        }

        if (numero.Length >= 2 && int.TryParse(numero[..2], out int dois) && dois is >= 51 and <= 55)
        {
            return "Mastercard";
        }

        if (numero.Length >= 4 && int.TryParse(numero[..4], out int quatro) && quatro is >= 2221 and <= 2720)
        {
            return "Mastercard";
        }

        return "Cartão";
    }

    private static bool ValidadeOk(string? validade, DateTime agora, out string erro)
    {
        erro = "Validade inválida. Use MM/AA.";

        string texto = (validade ?? string.Empty).Trim();

        if (!DateTime.TryParseExact(
                texto,
                new[] { "MM/yy", "MM/yyyy" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime data))
        {
            return false;
        }

        // O cartão vale até o último dia do mês impresso.
        var fimDoMes = new DateTime(data.Year, data.Month, 1).AddMonths(1);

        if (fimDoMes <= agora)
        {
            erro = "Cartão vencido.";
            return false;
        }

        return true;
    }

    public static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
        {
            return false;
        }

        int Digito(int tamanho)
        {
            int soma = 0;

            for (int i = 0; i < tamanho; i++)
            {
                soma += (cpf[i] - '0') * (tamanho + 1 - i);
            }

            int resto = soma % 11;

            return resto < 2 ? 0 : 11 - resto;
        }

        return Digito(9) == cpf[9] - '0' && Digito(10) == cpf[10] - '0';
    }
}
