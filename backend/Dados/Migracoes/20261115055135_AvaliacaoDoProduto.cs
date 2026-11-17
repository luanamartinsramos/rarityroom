using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RarityRoom.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class AvaliacaoDoProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProdutoId",
                table: "avaliacoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Preserva o vínculo das avaliações já existentes com o produto
            // do pedido avaliado.
            migrationBuilder.Sql(
                "UPDATE avaliacoes SET ProdutoId = COALESCE((SELECT p.ProdutoId FROM pedidos p WHERE p.Id = avaliacoes.PedidoId), 0);");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_ProdutoId",
                table: "avaliacoes",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_avaliacoes_ProdutoId",
                table: "avaliacoes");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "avaliacoes");
        }
    }
}
