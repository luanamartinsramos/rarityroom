using System.Collections.Concurrent;
using RarityRoom.Servicos;

namespace RarityRoom.Tests.Api;

/// <summary>Caixa de e-mail falsa: guarda o último token enviado a cada endereço.</summary>
public class CanalRecuperacaoMemoria : ICanalRecuperacaoSenha
{
    private readonly ConcurrentDictionary<string, string> tokens =
        new(StringComparer.OrdinalIgnoreCase);

    public void Enviar(string email, string nome, string token)
    {
        tokens[email] = token;
    }

    public string? UltimoToken(string email)
    {
        return tokens.TryGetValue(email, out string? token) ? token : null;
    }
}
