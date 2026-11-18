using System.Collections.Concurrent;
using RarityRoom.Modelos.Excecoes;

namespace RarityRoom.Servicos;

/// <summary>
/// Bloqueio temporário por conta após senhas erradas seguidas. Impede que
/// alguém teste milhares de senhas contra o mesmo e-mail. O estado fica em
/// memória: reiniciar o servidor zera os contadores, o que é aceitável aqui.
/// </summary>
public class ProtecaoForcaBruta
{
    public const int TentativasPermitidas = 5;

    public static readonly TimeSpan Janela = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan Bloqueio = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Registro> registros =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly TimeProvider relogio;

    public ProtecaoForcaBruta(TimeProvider relogio)
    {
        this.relogio = relogio;
    }

    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    /// <summary>Lança 429 se a conta está temporariamente bloqueada.</summary>
    public void GarantirLiberado(string chave)
    {
        if (registros.TryGetValue(Normalizar(chave), out var registro)
            && registro.BloqueadoAte is DateTime ate
            && ate > Agora)
        {
            int minutos = Math.Max(1, (int)Math.Ceiling((ate - Agora).TotalMinutes));

            throw new MuitasTentativasException(
                $"Muitas tentativas com senha errada. Tente novamente em {minutos} minuto(s).");
        }
    }

    public void RegistrarFalha(string chave)
    {
        var agora = Agora;

        registros.AddOrUpdate(
            Normalizar(chave),
            _ => new Registro(1, agora, null),
            (_, atual) =>
            {
                // Janela vencida ou bloqueio já cumprido: recomeça a contagem.
                bool recomeca = agora - atual.InicioJanela > Janela
                    || (atual.BloqueadoAte is DateTime ate && ate <= agora);

                int falhas = recomeca ? 1 : atual.Falhas + 1;
                var inicio = recomeca ? agora : atual.InicioJanela;

                return new Registro(
                    falhas,
                    inicio,
                    falhas >= TentativasPermitidas ? agora.Add(Bloqueio) : null);
            });
    }

    public void RegistrarSucesso(string chave)
    {
        registros.TryRemove(Normalizar(chave), out _);
    }

    private static string Normalizar(string chave)
    {
        return (chave ?? string.Empty).Trim().ToLowerInvariant();
    }

    private sealed record Registro(int Falhas, DateTime InicioJanela, DateTime? BloqueadoAte);
}
