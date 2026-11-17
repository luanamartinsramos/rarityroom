namespace RarityRoom.Servicos;

public interface IServicoImagens
{
    Task<string> SalvarAsync(
        Stream conteudo,
        string tipoConteudo,
        long tamanho);
}
