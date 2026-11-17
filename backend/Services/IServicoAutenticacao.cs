using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoAutenticacao
{
    /// <summary>Toda conta nasce compradora e vendedora.</summary>
    Usuario Cadastrar(
        string nome,
        string email,
        string senha);

    Usuario? Entrar(
        string email,
        string senha);

    bool EmailExiste(string email);
}