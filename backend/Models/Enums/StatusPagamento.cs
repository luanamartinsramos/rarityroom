namespace RarityRoom.Modelos.Enums;

/// <summary>
/// Situação do pagamento simulado. Os nomes ficam gravados como texto no
/// banco; "Pendente" é o estado "aguardando pagamento" e "Pago" é o
/// "pagamento aprovado".
/// </summary>
public enum StatusPagamento
{
    /// <summary>Aguardando pagamento.</summary>
    Pendente = 1,

    /// <summary>Pagamento aprovado pela simulação.</summary>
    Pago = 2,

    /// <summary>Pagamento recusado pela simulação (cartão).</summary>
    Recusado = 3,

    /// <summary>Prazo do PIX ou do boleto venceu sem confirmação.</summary>
    Expirado = 4,

    /// <summary>Compra cancelada antes da confirmação do pagamento.</summary>
    Cancelado = 5,

    /// <summary>Compra paga e cancelada depois: valor devolvido (simulação).</summary>
    Estornado = 6
}
