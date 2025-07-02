using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerPrePopulationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrePopulated",
                table: "Responses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrePopulatedAccepted",
                table: "Responses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SourceResponseId",
                table: "Responses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Responses_SourceResponseId",
                table: "Responses",
                column: "SourceResponseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_Responses_SourceResponseId",
                table: "Responses",
                column: "SourceResponseId",
                principalTable: "Responses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Responses_Responses_SourceResponseId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_Responses_SourceResponseId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "IsPrePopulated",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "IsPrePopulatedAccepted",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "SourceResponseId",
                table: "Responses");
        }
    }
}
