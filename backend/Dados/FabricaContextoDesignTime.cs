using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RarityRoom.Dados;

/// <summary>
/// Usada apenas pelas ferramentas de linha de comando (dotnet ef). Sem ela, o
/// EF executaria o Program completo — inclusive a migração e o seed — só para
/// descobrir a configuração do contexto.
/// </summary>
public class FabricaContextoDesignTime
    : IDesignTimeDbContextFactory<RarityRoomContexto>
{
    public RarityRoomContexto CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<RarityRoomContexto>()
            .UseSqlite("Data Source=rarityroom.db")
            .Options;

        return new RarityRoomContexto(opcoes);
    }
}
