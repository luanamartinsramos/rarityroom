using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Anúncio de um colecionável. Concentra as regras de formação e o ciclo de
/// vida de venda; nenhuma camada acima pode contorná-las.
/// </summary>
public class Produto
{
    private const int TamanhoMinimoNome = 3;
    private const int TamanhoMaximoNome = 120;
    private const int TamanhoMaximoDescricao = 2000;
    private const int TamanhoMinimoCategoria = 2;
    private const int TamanhoMaximoCategoria = 60;
    private const int TamanhoMaximoTextoCurto = 80;
    private const int TamanhoMaximoImagemUrl = 500;
    private const decimal PrecoMaximo = 1_000_000m;

    /// <summary>
    /// Tabela única de transições, indexada por (estado de origem, operação).
    /// Indexar apenas pelo destino faria "Reativar" e "CancelarReserva"
    /// colidirem, permitindo a operação errada no estado errado.
    /// </summary>
    private static readonly Dictionary<
        (EstadoAnuncio Origem, OperacaoAnuncio Operacao),
        EstadoAnuncio> Transicoes = new()
        {
            [(EstadoAnuncio.Disponivel, OperacaoAnuncio.Reservar)] =
                EstadoAnuncio.Reservado,

            [(EstadoAnuncio.Disponivel, OperacaoAnuncio.Vender)] =
                EstadoAnuncio.Vendido,

            [(EstadoAnuncio.Disponivel, OperacaoAnuncio.Pausar)] =
                EstadoAnuncio.Pausado,

            [(EstadoAnuncio.Reservado, OperacaoAnuncio.Vender)] =
                EstadoAnuncio.Vendido,

            [(EstadoAnuncio.Reservado, OperacaoAnuncio.CancelarReserva)] =
                EstadoAnuncio.Disponivel,

            [(EstadoAnuncio.Pausado, OperacaoAnuncio.Reativar)] =
                EstadoAnuncio.Disponivel
        };

    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Descricao { get; private set; } = string.Empty;

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }

    public string Categoria { get; private set; } = string.Empty;

    public string Marca { get; private set; } = string.Empty;

    public string Colecao { get; private set; } = string.Empty;

    /// <summary>Franquia do item (anime, filme, jogo...).</summary>
    public string Franquia { get; private set; } = string.Empty;

    public Raridade Raridade { get; private set; } = Raridade.Comum;

    public bool EdicaoLimitada { get; private set; }

    /// <summary>Destaque escolhido pelo vendedor na vitrine.</summary>
    public bool Destaque { get; private set; }

    public string Condicao { get; private set; } = string.Empty;

    public string ImagemUrl { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; }

    public EstadoAnuncio Estado { get; private set; } = EstadoAnuncio.Disponivel;

    public ModalidadeNegociacao Modalidade { get; private set; } = ModalidadeNegociacao.PrecoFixo;

    public bool Disponivel => Estado == EstadoAnuncio.Disponivel && Estoque > 0;

    public decimal ValorTotalEmEstoque => Preco * Estoque;

    /// <summary>Exigido pelo EF Core para materializar a entidade do banco.</summary>
    private Produto()
    {
    }

    public Produto(
        int proprietarioId,
        string nome,
        string descricao,
        decimal preco,
        int estoque,
        string categoria,
        string marca,
        string colecao,
        string condicao,
        string imagemUrl)
    {
        if (proprietarioId <= 0)
        {
            throw new ValidacaoException(
                "O anúncio precisa estar vinculado a um proprietário válido.");
        }

        UsuarioId = proprietarioId;
        Estado = EstadoAnuncio.Disponivel;
        CriadoEm = DateTime.UtcNow;

        AplicarDados(
            nome,
            descricao,
            preco,
            estoque,
            categoria,
            marca,
            colecao,
            condicao,
            imagemUrl
        );
    }

    public void Atualizar(
        string nome,
        string descricao,
        decimal preco,
        int estoque,
        string categoria,
        string marca,
        string colecao,
        string condicao,
        string imagemUrl)
    {
        if (Estado == EstadoAnuncio.Vendido)
        {
            throw new ConflitoException(
                "Um anúncio já vendido não pode ser editado.");
        }

        AplicarDados(
            nome,
            descricao,
            preco,
            estoque,
            categoria,
            marca,
            colecao,
            condicao,
            imagemUrl
        );
    }

    /// <summary>
    /// Garante que somente o dono altere o anúncio. Sem esta verificação,
    /// qualquer usuário autenticado editaria o anúncio de terceiros.
    /// </summary>
    public void ValidarPropriedade(int solicitanteId)
    {
        if (solicitanteId <= 0)
        {
            throw new AcessoNegadoException(
                "É necessário estar autenticado para alterar um anúncio.");
        }

        if (UsuarioId != solicitanteId)
        {
            throw new AcessoNegadoException(
                "Somente o proprietário pode alterar este anúncio.");
        }
    }

    public void ValidarExclusao(int solicitanteId)
    {
        ValidarPropriedade(solicitanteId);

        if (Estado == EstadoAnuncio.Vendido)
        {
            throw new ConflitoException(
                "Um anúncio já vendido não pode ser excluído.");
        }
    }

    /// <summary>
    /// Define como o vendedor aceita negociar. Anúncio vendido tem a
    /// modalidade congelada para não reescrever o histórico.
    /// </summary>
    /// <summary>
    /// Detalhes de coleção: franquia, raridade e edição limitada.
    /// </summary>
    public void DefinirDetalhes(
        string franquia,
        Raridade raridade,
        bool edicaoLimitada)
    {
        if (Estado == EstadoAnuncio.Vendido)
        {
            throw new ConflitoException(
                "Um anúncio já vendido não pode ser editado.");
        }

        if (!Enum.IsDefined(raridade))
        {
            throw new ValidacaoException("Raridade inválida.");
        }

        Franquia = TextoOpcional(franquia, "franquia", TamanhoMaximoTextoCurto);
        Raridade = raridade;
        EdicaoLimitada = edicaoLimitada;
    }

    public void DefinirDestaque(bool destaque)
    {
        Destaque = destaque;
    }

    public void DefinirModalidade(ModalidadeNegociacao modalidade)
    {
        if (Estado == EstadoAnuncio.Vendido)
        {
            throw new ConflitoException(
                "Um anúncio já vendido não pode ter a modalidade alterada.");
        }

        Modalidade = modalidade;
    }

    public void Reservar()
    {
        AplicarOperacao(OperacaoAnuncio.Reservar);
    }

    public void Vender()
    {
        AplicarOperacao(OperacaoAnuncio.Vender);
    }

    public void Pausar()
    {
        AplicarOperacao(OperacaoAnuncio.Pausar);
    }

    public void Reativar()
    {
        AplicarOperacao(OperacaoAnuncio.Reativar);
    }

    public void CancelarReserva()
    {
        AplicarOperacao(OperacaoAnuncio.CancelarReserva);
    }

    public void AplicarOperacao(OperacaoAnuncio operacao)
    {
        if (operacao == OperacaoAnuncio.Vender && Estoque <= 0)
        {
            throw new ConflitoException(
                "Não há estoque disponível para concluir a venda.");
        }

        if (!Transicoes.TryGetValue((Estado, operacao), out EstadoAnuncio destino))
        {
            throw new TransicaoInvalidaException(
                $"Não é possível {Descrever(operacao)} um anúncio no estado {Estado}.");
        }

        Estado = destino;
    }

    /// <summary>
    /// Baixa definitiva do estoque, feita somente quando o pagamento simulado é
    /// aprovado. Nunca deixa o estoque negativo: pedir mais do que existe falha.
    /// Ao zerar, o anúncio vira "Vendido"; se estava reservado por proposta e
    /// ainda sobram unidades, volta a ficar disponível para os demais.
    /// </summary>
    public void BaixarEstoque(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ValidacaoException("A quantidade deve ser maior que zero.");
        }

        if (Estado != EstadoAnuncio.Disponivel && Estado != EstadoAnuncio.Reservado)
        {
            throw new ConflitoException(
                $"\"{Nome}\" não está mais disponível para compra.");
        }

        if (quantidade > Estoque)
        {
            throw new ConflitoException(
                $"Estoque insuficiente para \"{Nome}\": restam {Estoque} unidade(s).");
        }

        Estoque -= quantidade;

        if (Estoque == 0)
        {
            Estado = EstadoAnuncio.Vendido;
        }
        else if (Estado == EstadoAnuncio.Reservado)
        {
            Estado = EstadoAnuncio.Disponivel;
        }
    }

    private const int MaximoReabastecimento = 1000;

    /// <summary>
    /// Reposição de estoque pelo vendedor (RN-REAB). Anúncio esgotado
    /// (Vendido) volta a ficar disponível; Disponível e Pausado só somam
    /// unidades. Reservado é recusado: há uma proposta aceita em andamento.
    /// A validação acontece antes de qualquer alteração, então uma recusa
    /// nunca deixa o anúncio pela metade.
    /// </summary>
    public void Reabastecer(int quantidade)
    {
        if (quantidade <= 0 || quantidade > MaximoReabastecimento)
        {
            throw new ValidacaoException(
                $"Informe entre 1 e {MaximoReabastecimento} unidades para repor.");
        }

        if (Estado == EstadoAnuncio.Reservado)
        {
            throw new TransicaoInvalidaException(
                "Não é possível repor estoque de um anúncio reservado por proposta aceita.");
        }

        Estoque += quantidade;

        if (Estado == EstadoAnuncio.Vendido)
        {
            Estado = EstadoAnuncio.Disponivel;
        }
    }

    /// <summary>
    /// Devolve unidades ao estoque quando uma compra já paga é cancelada
    /// (estorno simulado). Um anúncio que havia esgotado volta a ficar
    /// disponível.
    /// </summary>
    public void ReporEstoque(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ValidacaoException("A quantidade deve ser maior que zero.");
        }

        Estoque += quantidade;

        if (Estado == EstadoAnuncio.Vendido)
        {
            Estado = EstadoAnuncio.Disponivel;
        }
    }

    /// <summary>
    /// Define a imagem do anúncio. Aceita URL absoluta ou o caminho relativo
    /// produzido pelo serviço de imagens.
    /// </summary>
    public void DefinirImagem(string imagemUrl)
    {
        if (Estado == EstadoAnuncio.Vendido)
        {
            throw new ConflitoException(
                "Um anúncio já vendido não pode ser editado.");
        }

        ImagemUrl = ImagemValida(imagemUrl);
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador do anúncio deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador do anúncio já foi definido.");
        }

        Id = id;
    }

    private void AplicarDados(
        string nome,
        string descricao,
        decimal preco,
        int estoque,
        string categoria,
        string marca,
        string colecao,
        string condicao,
        string imagemUrl)
    {
        Nome = TextoObrigatorio(nome, "nome", TamanhoMinimoNome, TamanhoMaximoNome);
        Descricao = TextoOpcional(descricao, "descrição", TamanhoMaximoDescricao);
        Categoria = TextoObrigatorio(categoria, "categoria", TamanhoMinimoCategoria, TamanhoMaximoCategoria);
        Marca = TextoOpcional(marca, "marca", TamanhoMaximoTextoCurto);
        Colecao = TextoOpcional(colecao, "coleção", TamanhoMaximoTextoCurto);
        Condicao = TextoOpcional(condicao, "condição", TamanhoMaximoTextoCurto);
        ImagemUrl = ImagemValida(imagemUrl);
        Preco = PrecoValido(preco);
        Estoque = EstoqueValido(estoque);
    }

    private static string TextoObrigatorio(
        string? valor,
        string campo,
        int minimo,
        int maximo)
    {
        string texto = (valor ?? string.Empty).Trim();

        if (texto.Length == 0)
        {
            throw new ValidacaoException($"O {campo} é obrigatório.");
        }

        if (texto.Length < minimo)
        {
            throw new ValidacaoException(
                $"O {campo} deve ter pelo menos {minimo} caracteres.");
        }

        if (texto.Length > maximo)
        {
            throw new ValidacaoException(
                $"O {campo} deve ter no máximo {maximo} caracteres.");
        }

        return texto;
    }

    private static string TextoOpcional(
        string? valor,
        string campo,
        int maximo)
    {
        string texto = (valor ?? string.Empty).Trim();

        if (texto.Length > maximo)
        {
            throw new ValidacaoException(
                $"O {campo} deve ter no máximo {maximo} caracteres.");
        }

        return texto;
    }

    private static decimal PrecoValido(decimal preco)
    {
        if (preco <= 0)
        {
            throw new ValidacaoException("O preço deve ser maior que zero.");
        }

        if (preco > PrecoMaximo)
        {
            throw new ValidacaoException(
                $"O preço deve ser no máximo {PrecoMaximo:N2}.");
        }

        return Math.Round(preco, 2);
    }

    private static int EstoqueValido(int estoque)
    {
        if (estoque < 0)
        {
            throw new ValidacaoException("O estoque não pode ser negativo.");
        }

        return estoque;
    }

    private static string ImagemValida(string? imagemUrl)
    {
        string texto = (imagemUrl ?? string.Empty).Trim();

        if (texto.Length == 0)
        {
            return string.Empty;
        }

        if (texto.Length > TamanhoMaximoImagemUrl)
        {
            throw new ValidacaoException(
                $"A imagemUrl deve ter no máximo {TamanhoMaximoImagemUrl} caracteres.");
        }

        // Caminho gerado pelo próprio serviço de imagens da aplicação.
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
                "A imagemUrl deve ser um endereço http ou https válido.");
        }

        return texto;
    }

    private static string Descrever(OperacaoAnuncio operacao)
    {
        return operacao switch
        {
            OperacaoAnuncio.Reservar => "reservar",
            OperacaoAnuncio.Vender => "vender",
            OperacaoAnuncio.Pausar => "pausar",
            OperacaoAnuncio.Reativar => "reativar",
            OperacaoAnuncio.CancelarReserva => "cancelar a reserva de",
            _ => "alterar"
        };
    }
}
