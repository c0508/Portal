using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionAssignmentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignAssignmentId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    SectionName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AssignedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAssignments", x => x.Id);
                    table.CheckConstraint("CK_QuestionAssignment_QuestionOrSection", "(QuestionId IS NOT NULL AND SectionName IS NULL) OR (QuestionId IS NULL AND SectionName IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_QuestionAssignments_CampaignAssignments_CampaignAssignmentId",
                        column: x => x.CampaignAssignmentId,
                        principalTable: "CampaignAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionAssignments_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionAssignments_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionAssignments_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponseOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResponseId = table.Column<int>(type: "int", nullable: false),
                    OverriddenById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OriginalResponderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OriginalValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverrideValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverrideReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OverriddenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseOverrides_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResponseOverrides_Users_OriginalResponderId",
                        column: x => x.OriginalResponderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseOverrides_Users_OverriddenById",
                        column: x => x.OverriddenById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignments_AssignedUserId",
                table: "QuestionAssignments",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignments_Assignment_Question",
                table: "QuestionAssignments",
                columns: new[] { "CampaignAssignmentId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignments_Assignment_Section",
                table: "QuestionAssignments",
                columns: new[] { "CampaignAssignmentId", "SectionName" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignments_CampaignAssignmentId",
                table: "QuestionAssignments",
                column: "CampaignAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignments_CreatedById",
                table: "QuestionAssignments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignments_QuestionId",
                table: "QuestionAssignments",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseOverrides_OriginalResponderId",
                table: "ResponseOverrides",
                column: "OriginalResponderId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseOverrides_OverriddenById",
                table: "ResponseOverrides",
                column: "OverriddenById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseOverrides_ResponseId",
                table: "ResponseOverrides",
                column: "ResponseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionAssignments");

            migrationBuilder.DropTable(
                name: "ResponseOverrides");
        }
    }
}
