using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionAssignmentChangeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionAssignmentChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignAssignmentId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    SectionName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChangedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OldAssignedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NewAssignedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OldInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NewInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAssignmentChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionAssignmentChanges_CampaignAssignments_CampaignAssignmentId",
                        column: x => x.CampaignAssignmentId,
                        principalTable: "CampaignAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionAssignmentChanges_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionAssignmentChanges_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionAssignmentChanges_Users_NewAssignedUserId",
                        column: x => x.NewAssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionAssignmentChanges_Users_OldAssignedUserId",
                        column: x => x.OldAssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignmentChanges_CampaignAssignmentId",
                table: "QuestionAssignmentChanges",
                column: "CampaignAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignmentChanges_ChangedById",
                table: "QuestionAssignmentChanges",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignmentChanges_NewAssignedUserId",
                table: "QuestionAssignmentChanges",
                column: "NewAssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignmentChanges_OldAssignedUserId",
                table: "QuestionAssignmentChanges",
                column: "OldAssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAssignmentChanges_QuestionId",
                table: "QuestionAssignmentChanges",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionAssignmentChanges");
        }
    }
}
