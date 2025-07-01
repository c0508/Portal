using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationRelationshipId",
                table: "CampaignAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlatformOrganizationId = table.Column<int>(type: "int", nullable: false),
                    SupplierOrganizationId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRelationships", x => x.Id);
                    table.CheckConstraint("CK_OrganizationRelationship_NoSelfReference", "PlatformOrganizationId != SupplierOrganizationId");
                    table.ForeignKey(
                        name: "FK_OrganizationRelationships_Organizations_PlatformOrganizationId",
                        column: x => x.PlatformOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRelationships_Organizations_SupplierOrganizationId",
                        column: x => x.SupplierOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRelationships_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserContexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    ActivePlatformOrganizationId = table.Column<int>(type: "int", nullable: true),
                    LastSwitched = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserContexts_Organizations_ActivePlatformOrganizationId",
                        column: x => x.ActivePlatformOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserContexts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserContexts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignAssignments_OrganizationRelationshipId",
                table: "CampaignAssignments",
                column: "OrganizationRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRelationships_CreatedByUserId",
                table: "OrganizationRelationships",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRelationships_Platform_Supplier_Unique",
                table: "OrganizationRelationships",
                columns: new[] { "PlatformOrganizationId", "SupplierOrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRelationships_PlatformOrganizationId",
                table: "OrganizationRelationships",
                column: "PlatformOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRelationships_SupplierOrganizationId",
                table: "OrganizationRelationships",
                column: "SupplierOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserContexts_ActivePlatformOrganizationId",
                table: "UserContexts",
                column: "ActivePlatformOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserContexts_OrganizationId",
                table: "UserContexts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserContexts_User_Organization_Unique",
                table: "UserContexts",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignAssignments_OrganizationRelationships_OrganizationRelationshipId",
                table: "CampaignAssignments",
                column: "OrganizationRelationshipId",
                principalTable: "OrganizationRelationships",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CampaignAssignments_OrganizationRelationships_OrganizationRelationshipId",
                table: "CampaignAssignments");

            migrationBuilder.DropTable(
                name: "OrganizationRelationships");

            migrationBuilder.DropTable(
                name: "UserContexts");

            migrationBuilder.DropIndex(
                name: "IX_CampaignAssignments_OrganizationRelationshipId",
                table: "CampaignAssignments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "OrganizationRelationshipId",
                table: "CampaignAssignments");
        }
    }
}
