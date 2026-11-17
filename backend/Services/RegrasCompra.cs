using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;
using RarityRoom.Repositorios;

namespace RarityRoom.Servicos;

/// <summary>
/// Regras únicas de "o que pode ser comprado, por quem, quanto e a que preço".
/// O carrinho e o checkout usam exatamente esta classe, então adicionar pelo
/// catálogo, pela página do produto ou finalizar a compra seguem a mesma
/// validação — e sempre no servidor.
/// </summary>
public class RegrasCompra
{
    private readonly IRepositorioProposta repositorioProposta;

    public RegrasCompra(IRepositorioProposta repositorioProposta)
    {
        this.repositorioProposta = repositorioProposta;
    }

    /// <summary>
    /// Preço praticado para este comprador: o do anúncio ou, se o anúncio
    /// está reservado para ele por uma proposta aceita, o valor da proposta.
    /// </summary>
    public decimal PrecoPara(Produto produto, int compradorId)
    {
        var proposta = PropostaAceitaDe(produto, compradorId);

        return proposta?.Valor ?? produto.Preco;
    }

    public bool PrecoDeProposta(Produto produto, int compradorId)
    {
        return PropostaAceitaDe(produto, compradorId) != null;
    }

    /// <summary>Quantidade máxima que este comprador pode levar.</summary>
    public int LimitePara(Produto produto, int compradorId)
    {
        // Proposta aceita vale para uma unidade.
        return PropostaAceitaDe(produto, compradorId) != null
            ? Math.Min(1, produto.Estoque)
            : produto.Estoque;
    }

    /// <summary>
    /// Lança a exceção adequada quando a compra não é permitida: anúncio
    /// próprio, indisponível, quantidade inválida ou acima do estoque.
    /// </summary>
    public void Validar(Produto? produto, int compradorId, int quantidade)
    {
        if (produto == null)
        {
            throw new NaoEncontradoException("Este anúncio não existe mais.");
        }

        if (quantidade <= 0)
        {
            throw new ValidacaoException("A quantidade deve ser maior que zero.");
        }

        if (produto.UsuarioId == compradorId)
        {
            throw new ValidacaoException(
                "Este anúncio é seu: não é possível comprá-lo.");
        }

        switch (produto.Estado)
        {
            case EstadoAnuncio.Vendido:
                throw new ConflitoException($"\"{produto.Nome}\" está esgotado.");

            case EstadoAnuncio.Pausado:
                throw new ConflitoException(
                    $"\"{produto.Nome}\" foi pausado pelo vendedor.");

            case EstadoAnuncio.Reservado when PropostaAceitaDe(produto, compradorId) == null:
                throw new ConflitoException(
                    $"\"{produto.Nome}\" está reservado para outro comprador.");
        }

        if (produto.Estoque <= 0)
        {
            throw new ConflitoException($"\"{produto.Nome}\" está esgotado.");
        }

        int limite = LimitePara(produto, compradorId);

        if (quantidade > limite)
        {
            throw new ConflitoException(
                $"Estoque insuficiente para \"{produto.Nome}\": "
                + $"há {limite} unidade(s) disponível(is) e você selecionou {quantidade}.");
        }
    }

    /// <summary>Mesma validação, devolvendo a mensagem em vez de lançar.</summary>
    public string? Problema(Produto? produto, int compradorId, int quantidade)
    {
        try
        {
            Validar(produto, compradorId, quantidade);

            return null;
        }
        catch (ExcecaoDominio falha)
        {
            return falha.Message;
        }
    }

    private Proposta? PropostaAceitaDe(Produto produto, int compradorId)
    {
        if (produto.Estado != EstadoAnuncio.Reservado)
        {
            return null;
        }

        // A reserva pertence à proposta aceita mais recente do anúncio.
        var aceita = repositorioProposta
            .ObterPorAnuncio(produto.Id)
            .Where(proposta => proposta.Estado == EstadoProposta.Aceita)
            .OrderByDescending(proposta => proposta.CriadaEm)
            .ThenByDescending(proposta => proposta.Id)
            .FirstOrDefault();

        return aceita?.ProponenteId == compradorId ? aceita : null;
    }
}
