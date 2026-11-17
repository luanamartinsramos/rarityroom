using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

public interface IRepositorioRecuperacaoSenha
{
    RecuperacaoSenha Adicionar(RecuperacaoSenha recuperacao);

    RecuperacaoSenha? ObterPorToken(string token);

    bool Atualizar(RecuperacaoSenha recuperacao);

    void InvalidarDoUsuario(int usuarioId);
}
