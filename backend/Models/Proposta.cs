using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Oferta de compra enviada por um usuário para um anúncio. O ciclo de vida
/// pertence à entidade: só uma proposta pendente pode ser aceita, recusada ou
/// cancelada.
/// </summary>
public class Proposta
{
    private const int TamanhoMaximoMensagem = 500;

    public int Id { get; private set; }

    public int ProdutoId { get; private set; }

    public int ProponenteId { get; private set; }

    public decimal Valor { get; private set; }

    public string Mensagem { get; private set; } = string.Empty;

    public EstadoProposta Estado { get; private set; } = EstadoProposta.Pendente;

    public DateTime CriadaEm { get; private set; }

    public bool EstaPendente => Estado == EstadoProposta.Pendente;

    /// <summary>Exigido pelo EF Core para materializar a entidade do banco.</summary>
    private Proposta()
    {
    }

    public Proposta(
        int produtoId,
        int proponenteId,
        decimal valor,
        string mensagem)
    {
        if (produtoId <= 0)
        {
            throw new ValidacaoException(
                "A proposta precisa estar vinculada a um anúncio.");
        }

        if (proponenteId <= 0)
        {
            throw new ValidacaoException(
                "A proposta precisa ter um proponente.");
        }

        if (valor <= 0)
        {
            throw new ValidacaoException(
                "O valor da proposta deve ser maior que zero.");
        }

        string texto = (mensagem ?? string.Empty).Trim();

        if (texto.Length > TamanhoMaximoMensagem)
        {
            throw new ValidacaoException(
                $"A mensagem deve ter no máximo {TamanhoMaximoMensagem} caracteres.");
        }

        ProdutoId = produtoId;
        ProponenteId = proponenteId;
        Valor = Math.Round(valor, 2);
        Mensagem = texto;
        Estado = EstadoProposta.Pendente;
        CriadaEm = DateTime.UtcNow;
    }

    public void Aceitar()
    {
        Transicionar(EstadoProposta.Aceita, "aceitar");
    }

    public void Recusar()
    {
        Transicionar(EstadoProposta.Recusada, "recusar");
    }

    public void Cancelar()
    {
        Transicionar(EstadoProposta.Cancelada, "cancelar");
    }

    /// <summary>
    /// Somente quem enviou a proposta pode cancelá-la.
    /// </summary>
    public void ValidarProponente(int solicitanteId)
    {
        if (ProponenteId != solicitanteId)
        {
            throw new AcessoNegadoException(
                "Somente quem enviou a proposta pode cancelá-la.");
        }
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador da proposta deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador da proposta já foi definido.");
        }

        Id = id;
    }

    private void Transicionar(EstadoProposta destino, string acao)
    {
        if (Estado != EstadoProposta.Pendente)
        {
            throw new TransicaoInvalidaException(
                $"Não é possível {acao} uma proposta no estado {Estado}.");
        }

        Estado = destino;
    }
}
