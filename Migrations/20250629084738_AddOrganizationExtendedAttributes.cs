using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationExtendedAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_QuestionnaireVersions_QuestionnaireVersionId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_QuestionnaireVersionId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionnaireVersionId",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "ABCSegmentation",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);



            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SizeCategory",
                table: "Organizations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierClassification",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Organizations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ABCSegmentation",
                table: "Organizations");



            migrationBuilder.DropColumn(
                name: "Category",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SizeCategory",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SupplierClassification",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Organizations");

            migrationBuilder.AddColumn<int>(
                name: "QuestionnaireVersionId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionnaireVersionId",
                table: "Questions",
                column: "QuestionnaireVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_QuestionnaireVersions_QuestionnaireVersionId",
                table: "Questions",
                column: "QuestionnaireVersionId",
                principalTable: "QuestionnaireVersions",
                principalColumn: "Id");
        }
    }
}
