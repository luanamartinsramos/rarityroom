using RarityRoom.Modelos.Enums;

namespace RarityRoom.Modelos;

/// <summary>
/// Dados aceitos para criar ou editar um anúncio. Desacopla a camada de
/// serviço do modelo de requisição HTTP: a regra de validade vive em
/// <see cref="Produto"/>, não na forma como o JSON chega.
/// </summary>
public record DadosAnuncio(
    string Nome,
    string Descricao,
    decimal Preco,
    int Estoque,
    string Categoria,
    string Marca,
    string Colecao,
    string Condicao,
    string ImagemUrl,
    ModalidadeNegociacao Modalidade = ModalidadeNegociacao.PrecoFixo,
    string Franquia = "",
    Raridade Raridade = Raridade.Comum,
    bool EdicaoLimitada = false,
    bool Destaque = false);
