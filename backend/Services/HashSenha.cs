using System.Security.Cryptography;

namespace RarityRoom.Servicos;

public class HashSenha : IHashSenha
{
    public string CriarHash(string senha)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32
        );

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerificarSenha(string senha, string hashCompleto)
    {
        string[] partes = hashCompleto.Split(':');

        if (partes.Length != 2)
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(partes[0]);
        byte[] hashEsperado = Convert.FromBase64String(partes[1]);

        byte[] hashAtual = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32
        );

        return CryptographicOperations.FixedTimeEquals(
            hashAtual,
            hashEsperado
        );
    }
}