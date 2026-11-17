using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoCategorias
{
    List<Categoria> Listar();

    Categoria Criar(string nome);
}
