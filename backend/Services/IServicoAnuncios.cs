using RarityRoom.Modelos;
using RarityRoom.Modelos.Enums;

namespace RarityRoom.Servicos;

public interface IServicoAnuncios
{
    List<Produto> Listar(FiltroAnuncios? filtro = null);

    PaginaCatalogo ListarPaginado(FiltroAnuncios filtro);

    Produto ObterPorId(int id);

    ProdutoCatalogo ObterDetalhe(int id);

    List<ProdutoCatalogo> ListarDoUsuario(int usuarioId);

    Produto Criar(DadosAnuncio dados, int proprietarioId);

    Produto Atualizar(int id, DadosAnuncio dados, int solicitanteId);

    Produto AlterarEstado(int id, OperacaoAnuncio operacao, int solicitanteId);

    Produto DefinirImagem(int id, string imagemUrl, int solicitanteId);

    void Remover(int id, int solicitanteId);
}
