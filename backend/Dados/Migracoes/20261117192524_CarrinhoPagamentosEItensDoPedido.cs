using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RarityRoom.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class CarrinhoPagamentosEItensDoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pedidos_ProdutoId",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "IX_avaliacoes_PedidoId",
                table: "avaliacoes");

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "pedidos",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PagamentoId",
                table: "pedidos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PagoEm",
                table: "pedidos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "itens_carrinho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    AdicionadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_carrinho", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "itens_pedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NomeProduto = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ImagemUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "TEXT", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_pedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_itens_pedido_pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pagamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CompradorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Forma = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    Parcelas = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CodigoPix = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    LinhaDigitavel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CartaoFinal = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    CartaoBandeira = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ChaveIdempotencia = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamentos", x => x.Id);
                });

            // Dados existentes: cada pedido antigo tinha exatamente um anúncio e
            // uma unidade. Ele vira um item em itens_pedido (com o mesmo nome e
            // preço congelados) e o preço unitário passa a ser o total.
            migrationBuilder.Sql(@"
                INSERT INTO itens_pedido (PedidoId, ProdutoId, NomeProduto, ImagemUrl, PrecoUnitario, Quantidade)
                SELECT p.Id, p.ProdutoId, p.NomeProduto, COALESCE(pr.ImagemUrl, ''), p.PrecoUnitario, 1
                FROM pedidos p
                LEFT JOIN produtos pr ON pr.Id = p.ProdutoId;");

            // Número público único para os pedidos antigos.
            migrationBuilder.Sql(
                "UPDATE pedidos SET Codigo = 'RR-' || printf('%08d', Id) WHERE Codigo = '';");

            // Pagamentos aprovados no fluxo antigo ganham a data de pagamento.
            migrationBuilder.Sql(
                "UPDATE pedidos SET PagoEm = CriadoEm WHERE StatusPagamento = 'Pago' AND PagoEm IS NULL;");

            // Pedidos antigos que nunca foram pagos não têm uma compra/pagamento
            // simulado associado (o fluxo antigo não criava um) e não podem mais
            // ser pagos: ficam registrados como pagamento expirado.
            migrationBuilder.Sql(@"
                UPDATE pedidos
                SET Estado = 'Cancelado',
                    StatusPagamento = CASE WHEN StatusPagamento = 'Recusado' THEN 'Recusado' ELSE 'Expirado' END,
                    CanceladoEm = COALESCE(CanceladoEm, CriadoEm)
                WHERE Estado = 'Pendente';");

            migrationBuilder.Sql(
                "UPDATE pedidos SET StatusPagamento = 'Pago' WHERE StatusPagamento = '' AND Estado IN ('Pago','Enviado','Entregue','Concluido');");

            migrationBuilder.DropColumn(
                name: "NomeProduto",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "pedidos");

            migrationBuilder.RenameColumn(
                name: "PrecoUnitario",
                table: "pedidos",
                newName: "Total");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_Codigo",
                table: "pedidos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_PagamentoId",
                table: "pedidos",
                column: "PagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_PedidoId_ProdutoId",
                table: "avaliacoes",
                columns: new[] { "PedidoId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_itens_carrinho_UsuarioId_ProdutoId",
                table: "itens_carrinho",
                columns: new[] { "UsuarioId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_itens_pedido_PedidoId",
                table: "itens_pedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_itens_pedido_ProdutoId",
                table: "itens_pedido",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_Codigo",
                table: "pagamentos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_CompradorId_ChaveIdempotencia",
                table: "pagamentos",
                columns: new[] { "CompradorId", "ChaveIdempotencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_carrinho");

            migrationBuilder.DropTable(
                name: "itens_pedido");

            migrationBuilder.DropTable(
                name: "pagamentos");

            migrationBuilder.DropIndex(
                name: "IX_pedidos_Codigo",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "IX_pedidos_PagamentoId",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "IX_avaliacoes_PedidoId_ProdutoId",
                table: "avaliacoes");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "PagamentoId",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "PagoEm",
                table: "pedidos");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "pedidos",
                newName: "PrecoUnitario");

            migrationBuilder.AddColumn<string>(
                name: "NomeProduto",
                table: "pedidos",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProdutoId",
                table: "pedidos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_ProdutoId",
                table: "pedidos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_PedidoId",
                table: "avaliacoes",
                column: "PedidoId",
                unique: true);
        }
    }
}
