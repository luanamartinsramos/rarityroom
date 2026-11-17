using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoPropostas
{
    List<Proposta> Listar(int usuarioId);

    Proposta Criar(
        int produtoId,
        decimal valor,
        string mensagem,
        int proponenteId);

    Proposta Aceitar(int id, int solicitanteId);

    Proposta Recusar(int id, int solicitanteId);

    Proposta Cancelar(int id, int solicitanteId);
}
