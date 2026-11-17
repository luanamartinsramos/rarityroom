using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RarityRoom.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "favoritos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favoritos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Preco = table.Column<decimal>(type: "TEXT", nullable: false),
                    Estoque = table.Column<int>(type: "INTEGER", nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Marca = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Colecao = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Condicao = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ImagemUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Modalidade = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "propostas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProponenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    Mensagem = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_propostas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_Slug",
                table: "categorias",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_favoritos_UsuarioId_ProdutoId",
                table: "favoritos",
                columns: new[] { "UsuarioId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_produtos_Categoria",
                table: "produtos",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_UsuarioId",
                table: "produtos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_propostas_ProdutoId",
                table: "propostas",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "favoritos");

            migrationBuilder.DropTable(
                name: "produtos");

            migrationBuilder.DropTable(
                name: "propostas");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
