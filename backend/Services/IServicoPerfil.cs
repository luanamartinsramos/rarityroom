using RarityRoom.Modelos;

namespace RarityRoom.Servicos;

public interface IServicoPerfil
{
    PerfilUsuario Obter(int usuarioId);

    PerfilUsuario AtualizarNome(int usuarioId, string nome);

    PerfilUsuario AtualizarEmail(int usuarioId, string email);

    PerfilUsuario DefinirFoto(int usuarioId, string fotoUrl);
}
