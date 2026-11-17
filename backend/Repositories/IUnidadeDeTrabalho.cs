namespace RarityRoom.Repositorios;

/// <summary>
/// Executa um bloco inteiro dentro de uma transação do banco: ou tudo é
/// gravado, ou nada é. No SQLite a transação é aberta com BEGIN IMMEDIATE,
/// que reserva a escrita logo no início — duas compras do mesmo estoque são
/// processadas uma depois da outra, nunca intercaladas.
/// </summary>
public interface IUnidadeDeTrabalho
{
    T Executar<T>(Func<T> operacao);
}
