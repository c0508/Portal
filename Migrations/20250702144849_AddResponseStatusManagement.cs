using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseStatusManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Responses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusUpdatedAt",
                table: "Responses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusUpdatedById",
                table: "Responses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResponseStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResponseId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseStatusHistories_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResponseStatusHistories_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_StatusUpdatedById",
                table: "Responses",
                column: "StatusUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseStatusHistories_ChangedById",
                table: "ResponseStatusHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseStatusHistories_ResponseId",
                table: "ResponseStatusHistories",
                column: "ResponseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_Users_StatusUpdatedById",
                table: "Responses",
                column: "StatusUpdatedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Responses_Users_StatusUpdatedById",
                table: "Responses");

            migrationBuilder.DropTable(
                name: "ResponseStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Responses_StatusUpdatedById",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "StatusUpdatedAt",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "StatusUpdatedById",
                table: "Responses");
        }
    }
}
