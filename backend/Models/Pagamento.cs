using System.Security.Cryptography;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Pagamento SIMULADO de uma compra (um checkout). Não existe gateway, banco
/// ou API financeira: os códigos de PIX e boleto são fictícios e sem validade,
/// e do cartão só ficam os 4 últimos dígitos e a bandeira — número completo,
/// validade e CVV nunca são gravados.
/// </summary>
public class Pagamento
{
    public static readonly TimeSpan PrazoPix = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan PrazoBoleto = TimeSpan.FromDays(3);

    public const int MaximoParcelas = 12;

    public int Id { get; private set; }

    /// <summary>Identificador público e único da compra (ex.: PAG-7F3K9Q2M4B).</summary>
    public string Codigo { get; private set; } = string.Empty;

    public int CompradorId { get; private set; }

    public FormaPagamento Forma { get; private set; }

    public StatusPagamento Status { get; private set; } = StatusPagamento.Pendente;

    public decimal Valor { get; private set; }

    public int Parcelas { get; private set; } = 1;

    public DateTime CriadoEm { get; private set; }

    public DateTime ExpiraEm { get; private set; }

    /// <summary>Quando o pagamento saiu de "aguardando" (aprovado, recusado...).</summary>
    public DateTime? ProcessadoEm { get; private set; }

    /// <summary>PIX copia e cola fictício (não é um BR Code válido).</summary>
    public string CodigoPix { get; private set; } = string.Empty;

    /// <summary>Linha digitável fictícia (banco 999 inexistente).</summary>
    public string LinhaDigitavel { get; private set; } = string.Empty;

    public string CartaoFinal { get; private set; } = string.Empty;

    public string CartaoBandeira { get; private set; } = string.Empty;

    public string Motivo { get; private set; } = string.Empty;

    /// <summary>
    /// Chave enviada pelo cliente em cada tentativa de checkout. Repetir a
    /// mesma chave (duplo clique, reenvio) devolve a compra já criada.
    /// </summary>
    public string ChaveIdempotencia { get; private set; } = string.Empty;

    public bool AguardandoPagamento => Status == StatusPagamento.Pendente;

    public decimal ValorParcela => Parcelas <= 1
        ? Valor
        : Math.Round(Valor / Parcelas, 2);

    private Pagamento()
    {
    }

    public Pagamento(
        int compradorId,
        FormaPagamento forma,
        decimal valor,
        string chaveIdempotencia,
        DateTime agora)
    {
        if (compradorId <= 0)
        {
            throw new AcessoNegadoException(
                "É necessário estar autenticado para comprar.");
        }

        if (!Enum.IsDefined(forma))
        {
            throw new ValidacaoException("Informe uma forma de pagamento válida.");
        }

        if (valor <= 0)
        {
            throw new ValidacaoException("Não é possível criar uma compra sem valor.");
        }

        string chave = (chaveIdempotencia ?? string.Empty).Trim();

        CompradorId = compradorId;
        Forma = forma;
        Valor = Math.Round(valor, 2);
        CriadoEm = agora;
        Codigo = GerarCodigo("PAG-");
        ChaveIdempotencia = chave.Length is > 0 and <= 64 ? chave : Guid.NewGuid().ToString("N");

        ExpiraEm = forma switch
        {
            FormaPagamento.Pix => agora.Add(PrazoPix),
            FormaPagamento.Boleto => agora.Add(PrazoBoleto),
            _ => agora.AddMinutes(30)
        };

        if (forma == FormaPagamento.Pix)
        {
            CodigoPix = GerarPixFicticio();
        }
        else if (forma == FormaPagamento.Boleto)
        {
            LinhaDigitavel = GerarLinhaDigitavel();
        }
    }

    /// <summary>
    /// Expira o pagamento se o prazo passou. Devolve true quando mudou.
    /// </summary>
    public bool VerificarExpiracao(DateTime agora)
    {
        if (Status != StatusPagamento.Pendente || agora < ExpiraEm)
        {
            return false;
        }

        Finalizar(StatusPagamento.Expirado, "O prazo para pagamento terminou.", agora);

        return true;
    }

    public void Aprovar(DateTime agora)
    {
        GarantirPendente();

        Finalizar(StatusPagamento.Pago, string.Empty, agora);
    }

    public void Recusar(string motivo, DateTime agora)
    {
        GarantirPendente();

        Finalizar(StatusPagamento.Recusado, motivo, agora);
    }

    public void Cancelar(string motivo, DateTime agora)
    {
        GarantirPendente();

        Finalizar(StatusPagamento.Cancelado, motivo, agora);
    }

    /// <summary>Usado pelo botão "simular expiração" da demonstração.</summary>
    public void Expirar(DateTime agora)
    {
        GarantirPendente();

        Finalizar(StatusPagamento.Expirado, "Prazo de pagamento expirado (simulação).", agora);
    }

    /// <summary>
    /// Guarda apenas o que pode ser exibido depois: 4 últimos dígitos,
    /// bandeira e parcelas.
    /// </summary>
    public void RegistrarCartao(string final, string bandeira, int parcelas)
    {
        if (Forma != FormaPagamento.CartaoCredito)
        {
            throw new ConflitoException("Esta compra não foi feita com cartão de crédito.");
        }

        if (parcelas < 1 || parcelas > MaximoParcelas)
        {
            throw new ValidacaoException(
                $"Escolha entre 1 e {MaximoParcelas} parcelas.");
        }

        CartaoFinal = final;
        CartaoBandeira = bandeira;
        Parcelas = parcelas;
    }

    internal void DefinirId(int id)
    {
        Id = id;
    }

    private void GarantirPendente()
    {
        if (Status != StatusPagamento.Pendente)
        {
            throw new TransicaoInvalidaException(
                $"Este pagamento já foi processado (situação: {Status}).");
        }
    }

    private void Finalizar(StatusPagamento status, string motivo, DateTime agora)
    {
        Status = status;
        Motivo = motivo ?? string.Empty;
        ProcessadoEm = agora;
    }

    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    internal static string GerarCodigo(string prefixo)
    {
        Span<char> letras = stackalloc char[10];

        for (int i = 0; i < letras.Length; i++)
        {
            letras[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        }

        return prefixo + new string(letras);
    }

    private static string Digitos(int quantidade)
    {
        Span<char> digitos = stackalloc char[quantidade];

        for (int i = 0; i < quantidade; i++)
        {
            digitos[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        }

        return new string(digitos);
    }

    /// <summary>
    /// Texto no formato aparente de um "copia e cola", mas marcado como
    /// simulação e sem chave, CRC ou instituição reais.
    /// </summary>
    private string GerarPixFicticio()
    {
        string valor = Valor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        return $"00020126SIMULACAO-RARITYROOM-SEM-VALOR-REAL5204000053039865406{valor}"
            + $"5802BR5910RARITYROOM6009DEMO62140510{Codigo}6304{Digitos(4)}";
    }

    /// <summary>
    /// 47 dígitos no formato de linha digitável, com código de banco 999
    /// (inexistente). Não tem validade bancária.
    /// </summary>
    private string GerarLinhaDigitavel()
    {
        string valorCentavos = ((long)(Valor * 100)).ToString().PadLeft(10, '0');

        string campos = "9999" + Digitos(5) + Digitos(1)
            + Digitos(10) + Digitos(1)
            + Digitos(10) + Digitos(1)
            + Digitos(1)
            + "0000" + valorCentavos;

        return $"{campos[..5]}.{campos[5..10]} {campos[10..15]}.{campos[15..21]} "
            + $"{campos[21..26]}.{campos[26..32]} {campos[32]} {campos[33..]}";
    }
}
