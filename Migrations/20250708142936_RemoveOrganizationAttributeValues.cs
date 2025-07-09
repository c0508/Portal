using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrganizationAttributeValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Option A: Single Source Strategy - Remove OrganizationAttributeValues table
            // All attribute values are now stored in OrganizationRelationshipAttributes
            
            migrationBuilder.Sql(@"
                -- Drop OrganizationAttributeValues table (deprecated in favor of relationship-specific attributes)
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrganizationAttributeValues]') AND type in (N'U'))
                BEGIN
                    PRINT 'Dropping OrganizationAttributeValues table...'
                    DROP TABLE [dbo].[OrganizationAttributeValues]
                    PRINT 'OrganizationAttributeValues table dropped successfully'
                END
                ELSE
                BEGIN
                    PRINT 'OrganizationAttributeValues table not found - already removed'
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate OrganizationAttributeValues table if migration is rolled back
            migrationBuilder.CreateTable(
                name: "OrganizationAttributeValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeTypeId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationAttributeValues_OrganizationAttributeTypes_AttributeTypeId",
                        column: x => x.AttributeTypeId,
                        principalTable: "OrganizationAttributeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAttributeValue_Type_Code_Unique",
                table: "OrganizationAttributeValues",
                columns: new[] { "AttributeTypeId", "Code" },
                unique: true);
        }
    }
}
