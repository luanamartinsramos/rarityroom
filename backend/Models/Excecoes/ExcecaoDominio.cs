namespace RarityRoom.Modelos.Excecoes;

/// <summary>
/// Raiz de todas as falhas de regra de negócio. O código é estável e serve de
/// contrato para a API (campo "codigo" na resposta de erro).
/// </summary>
public abstract class ExcecaoDominio : Exception
{
    protected ExcecaoDominio(string mensagem, string codigo)
        : base(mensagem)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
