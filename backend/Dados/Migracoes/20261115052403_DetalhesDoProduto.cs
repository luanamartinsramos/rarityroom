using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RarityRoom.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class DetalhesDoProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Destaque",
                table: "produtos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EdicaoLimitada",
                table: "produtos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Franquia",
                table: "produtos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Raridade",
                table: "produtos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Destaque",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "EdicaoLimitada",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "Franquia",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "Raridade",
                table: "produtos");
        }
    }
}
