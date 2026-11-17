using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RarityRoom.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class PerfilEnderecosESenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "usuarios",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "enderecos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cep = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Logradouro = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Complemento = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Principal = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enderecos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recuperacoes_senha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Usada = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recuperacoes_senha", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_enderecos_UsuarioId",
                table: "enderecos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_recuperacoes_senha_Token",
                table: "recuperacoes_senha",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enderecos");

            migrationBuilder.DropTable(
                name: "recuperacoes_senha");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "usuarios");
        }
    }
}
