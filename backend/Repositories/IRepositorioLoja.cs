using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioLoja
{
    Loja? ObterPorId(int id);

    Loja? ObterPorDono(int donoId);

    List<Loja> ObterTodas();

    Loja Adicionar(Loja loja);

    bool Atualizar(Loja loja);
}
