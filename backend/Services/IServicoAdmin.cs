using RarityRoom.Modelos;
using RarityRoom.Modelos.Admin;

namespace RarityRoom.Servicos;

public interface IServicoAdmin
{
    ResumoAdmin Resumo(int solicitanteId);

    List<UsuarioAdmin> ListarUsuarios(int solicitanteId);

    void SuspenderUsuario(int id, int solicitanteId);

    void ReativarUsuario(int id, int solicitanteId);

    List<Produto> ListarAnuncios(int solicitanteId);

    Produto ModerarAnuncio(int id, string acao, int solicitanteId);

    List<Pedido> ListarPedidos(int solicitanteId);
}
