namespace RarityRoom.Servicos;

public interface IHashSenha
{
    string CriarHash(string senha);

    bool VerificarSenha(string senha, string hash);
}