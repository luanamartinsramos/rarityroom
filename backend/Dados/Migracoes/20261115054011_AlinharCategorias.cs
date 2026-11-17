using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RarityRoom.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class AlinharCategorias : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// Renomeia as categorias antigas para o catálogo geek atual e move os
        /// anúncios existentes, sem perder nenhum produto.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE produtos SET Categoria = 'Action Figures' WHERE Categoria = 'Action Figure'");
            migrationBuilder.Sql("UPDATE produtos SET Categoria = 'Animes' WHERE Categoria = 'Anime'");
            migrationBuilder.Sql("UPDATE produtos SET Categoria = 'Cartas Colecionáveis' WHERE Categoria = 'Card Games'");
            migrationBuilder.Sql("UPDATE produtos SET Categoria = 'Colecionáveis Raros' WHERE Categoria = 'Colecionáveis'");
            migrationBuilder.Sql("UPDATE produtos SET Categoria = 'Funko Pop' WHERE Categoria = 'Funko'");
            migrationBuilder.Sql("UPDATE produtos SET Categoria = 'HQs e Quadrinhos' WHERE Categoria IN ('Marvel', 'DC Comics')");
            migrationBuilder.Sql("DELETE FROM categorias WHERE Nome IN ('Action Figure', 'Anime', 'Card Games', 'Colecionáveis', 'Funko', 'Marvel', 'DC Comics')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
