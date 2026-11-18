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

    /// <summary>
    /// Lança 404/403 se o anúncio não existe ou não pertence ao solicitante.
    /// Usado antes de gravar arquivos, para que um upload recusado não deixe
    /// lixo no disco.
    /// </summary>
    void GarantirDono(int id, int solicitanteId);
}
