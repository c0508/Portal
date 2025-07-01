using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingToOrganizationRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "NumericValue",
                table: "Responses",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            // Add missing branding columns to Organizations
            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Organizations",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByOrganizationId",
                table: "OrganizationRelationships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimaryRelationship",
                table: "OrganizationRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRelationships_CreatedByOrganizationId",
                table: "OrganizationRelationships",
                column: "CreatedByOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationRelationships_Organizations_CreatedByOrganizationId",
                table: "OrganizationRelationships",
                column: "CreatedByOrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationRelationships_Organizations_CreatedByOrganizationId",
                table: "OrganizationRelationships");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationRelationships_CreatedByOrganizationId",
                table: "OrganizationRelationships");

            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CreatedByOrganizationId",
                table: "OrganizationRelationships");

            migrationBuilder.DropColumn(
                name: "IsPrimaryRelationship",
                table: "OrganizationRelationships");

            migrationBuilder.AlterColumn<decimal>(
                name: "NumericValue",
                table: "Responses",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldNullable: true);
        }
    }
}
