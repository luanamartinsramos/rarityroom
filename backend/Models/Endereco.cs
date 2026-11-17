using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Endereço salvo na conta. O primeiro vira principal; os pedidos continuam
/// guardando a própria cópia no checkout.
/// </summary>
public class Endereco
{
    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public string Cep { get; private set; } = string.Empty;

    public string Logradouro { get; private set; } = string.Empty;

    public string Numero { get; private set; } = string.Empty;

    public string Complemento { get; private set; } = string.Empty;

    public string Bairro { get; private set; } = string.Empty;

    public string Cidade { get; private set; } = string.Empty;

    public string Estado { get; private set; } = string.Empty;

    public bool Principal { get; private set; }

    public DateTime CriadoEm { get; private set; }

    private Endereco()
    {
    }

    public Endereco(int usuarioId, DadosEntrega dados, bool principal)
    {
        ArgumentNullException.ThrowIfNull(dados);

        if (usuarioId <= 0)
        {
            throw new ValidacaoException(
                "O endereço precisa estar vinculado a um usuário.");
        }

        UsuarioId = usuarioId;
        Aplicar(dados);
        Principal = principal;
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(DadosEntrega dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        Aplicar(dados);
    }

    public void DefinirPrincipal(bool principal)
    {
        Principal = principal;
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador do endereço deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador do endereço já foi definido.");
        }

        Id = id;
    }

    public DadosEntrega ParaEntrega()
    {
        return new DadosEntrega(
            Cep,
            Logradouro,
            Numero,
            Complemento,
            Bairro,
            Cidade,
            Estado);
    }

    private void Aplicar(DadosEntrega dados)
    {
        Cep = dados.Cep;
        Logradouro = dados.Logradouro;
        Numero = dados.Numero;
        Complemento = dados.Complemento;
        Bairro = dados.Bairro;
        Cidade = dados.Cidade;
        Estado = dados.Estado;
    }
}
