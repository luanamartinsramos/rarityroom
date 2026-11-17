using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RarityRoom.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class PerfilECheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoConta",
                table: "usuarios",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Ambos");

            migrationBuilder.AddColumn<string>(
                name: "EntregaBairro",
                table: "pedidos",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntregaCep",
                table: "pedidos",
                type: "TEXT",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntregaCidade",
                table: "pedidos",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntregaComplemento",
                table: "pedidos",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntregaEstado",
                table: "pedidos",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntregaLogradouro",
                table: "pedidos",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntregaNumero",
                table: "pedidos",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FormaPagamento",
                table: "pedidos",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoConta",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "EntregaBairro",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaCep",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaCidade",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaComplemento",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaEstado",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaLogradouro",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaNumero",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "FormaPagamento",
                table: "pedidos");
        }
    }
}
