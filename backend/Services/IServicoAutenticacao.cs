using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoAutenticacao
{
    Usuario Cadastrar(
        string nome,
        string email,
        string senha);

    Usuario? Entrar(
        string email,
        string senha);

    bool EmailExiste(string email);
}