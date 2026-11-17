using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoVendedores
{
    PerfilVendedor Obter(int vendedorId);
}
