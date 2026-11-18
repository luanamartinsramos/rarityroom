using RarityRoom.Modelos.Enums;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Modelos;

/// <summary>
/// Conta da plataforma. A senha nunca é armazenada em texto puro: a entidade
/// recebe apenas o hash já calculado.
/// </summary>
public class Usuario
{
    private const int TamanhoMinimoNome = 3;
    private const int TamanhoMaximoNome = 120;
    private const int TamanhoMaximoEmail = 200;
    private const int TamanhoMinimoSenha = 8;

    private const int TamanhoMaximoSenha = 128;

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string SenhaHash { get; private set; } = string.Empty;

    public bool Ativo { get; private set; }

    /// <summary>Papel escolhido no cadastro.</summary>
    public TipoConta TipoConta { get; private set; } = TipoConta.Ambos;

    public bool PodeComprar =>
        TipoConta is TipoConta.Comprador or TipoConta.Ambos;

    public bool PodeVender =>
        TipoConta is TipoConta.Vendedor or TipoConta.Ambos;

    /// <summary>
    /// Papel administrativo. Não é escolhido no cadastro: só é concedido pelo
    /// seed de configuração, nunca pela API pública.
    /// </summary>
    public bool Administrador { get; private set; }

    /// <summary>Foto de perfil (URL absoluta ou caminho /uploads).</summary>
    public string FotoUrl { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; }

    /// <summary>Exigido pelo EF Core para materializar a entidade do banco.</summary>
    private Usuario()
    {
    }

    public Usuario(
        string nome,
        string email,
        string senhaHash,
        TipoConta tipoConta = TipoConta.Ambos,
        bool administrador = false)
    {
        Nome = NomeValido(nome);
        Email = EmailValido(email);
        SenhaHash = HashValido(senhaHash);
        TipoConta = tipoConta;
        Administrador = administrador;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public void AlterarNome(string nome)
    {
        Nome = NomeValido(nome);
    }

    public void AlterarEmail(string email)
    {
        Email = EmailValido(email);
    }

    /// <summary>
    /// Troca o hash da senha. A validação da senha em texto claro é
    /// responsabilidade de quem chama (cadastro ou redefinição).
    /// </summary>
    public void AlterarSenha(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            throw new ValidacaoException("O hash da senha é obrigatório.");
        }

        SenhaHash = senhaHash;
    }

    public void DefinirFoto(string fotoUrl)
    {
        string texto = (fotoUrl ?? string.Empty).Trim();

        if (texto.Length == 0)
        {
            FotoUrl = string.Empty;
            return;
        }

        if (texto.Length > 500)
        {
            throw new ValidacaoException(
                "A foto de perfil deve ter no máximo 500 caracteres.");
        }

        if (!texto.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            bool urlAbsoluta = Uri.TryCreate(texto, UriKind.Absolute, out Uri? uri);

            if (!urlAbsoluta
                || uri == null
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ValidacaoException(
                    "A foto de perfil deve ser um endereço http ou https válido.");
            }
        }

        FotoUrl = texto;
    }

    public void Desativar()
    {
        Ativo = false;
    }

    public void Ativar()
    {
        Ativo = true;
    }

    public bool EhProprietario(int outroUsuarioId)
    {
        return outroUsuarioId > 0 && Id == outroUsuarioId;
    }

    /// <summary>
    /// Regra de senha em texto puro. Vive aqui porque só o domínio decide o
    /// que é uma senha aceitável; o serviço apenas aplica.
    /// </summary>
    public static void ValidarSenha(string? senha)
    {
        string texto = senha ?? string.Empty;

        if (texto.Length == 0)
        {
            throw new ValidacaoException("A senha é obrigatória.");
        }

        if (texto.Length < TamanhoMinimoSenha)
        {
            throw new ValidacaoException(
                $"A senha deve ter pelo menos {TamanhoMinimoSenha} caracteres.");
        }

        if (texto.Length > TamanhoMaximoSenha)
        {
            throw new ValidacaoException(
                $"A senha deve ter no máximo {TamanhoMaximoSenha} caracteres.");
        }

        // Senhas só de números ("12345678") ou só de letras são as primeiras
        // testadas em ataques de dicionário.
        if (!texto.Any(char.IsLetter) || !texto.Any(char.IsDigit))
        {
            throw new ValidacaoException(
                "A senha deve ter pelo menos uma letra e um número.");
        }
    }

    internal void TornarAdministrador()
    {
        Administrador = true;
    }

    internal void DefinirId(int id)
    {
        if (id <= 0)
        {
            throw new ValidacaoException(
                "O identificador do usuário deve ser positivo.");
        }

        if (Id != 0)
        {
            throw new ConflitoException(
                "O identificador do usuário já foi definido.");
        }

        Id = id;
    }

    private static string NomeValido(string? nome)
    {
        string texto = (nome ?? string.Empty).Trim();

        if (texto.Length == 0)
        {
            throw new ValidacaoException("O nome é obrigatório.");
        }

        if (texto.Length < TamanhoMinimoNome)
        {
            throw new ValidacaoException(
                $"O nome deve ter pelo menos {TamanhoMinimoNome} caracteres.");
        }

        if (texto.Length > TamanhoMaximoNome)
        {
            throw new ValidacaoException(
                $"O nome deve ter no máximo {TamanhoMaximoNome} caracteres.");
        }

        return texto;
    }

    private static string EmailValido(string? email)
    {
        string texto = (email ?? string.Empty).Trim();

        if (texto.Length == 0)
        {
            throw new ValidacaoException("O e-mail é obrigatório.");
        }

        if (texto.Length > TamanhoMaximoEmail)
        {
            throw new ValidacaoException(
                $"O e-mail deve ter no máximo {TamanhoMaximoEmail} caracteres.");
        }

        int posicaoArroba = texto.IndexOf('@');
        bool arrobaUnica = posicaoArroba > 0
            && posicaoArroba == texto.LastIndexOf('@');

        if (!arrobaUnica || texto.Contains(' '))
        {
            throw new ValidacaoException("Informe um e-mail válido.");
        }

        string dominio = texto[(posicaoArroba + 1)..];

        if (!dominio.Contains('.') || dominio.StartsWith('.') || dominio.EndsWith('.'))
        {
            throw new ValidacaoException("Informe um e-mail válido.");
        }

        return texto;
    }

    private static string HashValido(string? senhaHash)
    {
        string texto = (senhaHash ?? string.Empty).Trim();

        if (texto.Length == 0 || !texto.Contains(':'))
        {
            throw new ValidacaoException(
                "A senha deve ser armazenada como hash com salt.");
        }

        return texto;
    }
}
