namespace RarityRoom.Modelos;

public class Usuario
{
    public int Id { get; private set; }

    public string Nome { get; private set; }

    public string Email { get; private set; }

    public string SenhaHash { get; private set; }

    public bool Ativo { get; private set; }

    public Usuario(
        int id,
        string nome,
        string email,
        string senhaHash)
    {
        Id = id;
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        Ativo = true;
    }

    public void AlterarNome(string nome)
    {
        Nome = nome;
    }

    public void AlterarEmail(string email)
    {
        Email = email;
    }

    public void Desativar()
    {
        Ativo = false;
    }

    public void Ativar()
    {
        Ativo = true;
    }
}