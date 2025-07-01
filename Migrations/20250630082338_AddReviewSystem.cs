using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResponseWorkflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResponseId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    SubmittedForReviewAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentReviewerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RevisionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseWorkflows_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResponseWorkflows_Users_CurrentReviewerId",
                        column: x => x.CurrentReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignAssignmentId = table.Column<int>(type: "int", nullable: false),
                    ReviewerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    SectionName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewAssignments", x => x.Id);
                    table.CheckConstraint("CK_ReviewAssignment_Scope", "(Scope = 2 AND QuestionId IS NULL AND SectionName IS NULL) OR (Scope = 0 AND QuestionId IS NOT NULL AND SectionName IS NULL) OR (Scope = 1 AND QuestionId IS NULL AND SectionName IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ReviewAssignments_CampaignAssignments_CampaignAssignmentId",
                        column: x => x.CampaignAssignmentId,
                        principalTable: "CampaignAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewAssignments_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewAssignments_Users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewAssignments_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignAssignmentId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    ResponseId = table.Column<int>(type: "int", nullable: true),
                    ReviewAssignmentId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewAuditLogs_CampaignAssignments_CampaignAssignmentId",
                        column: x => x.CampaignAssignmentId,
                        principalTable: "CampaignAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewAuditLogs_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewAuditLogs_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewAuditLogs_ReviewAssignments_ReviewAssignmentId",
                        column: x => x.ReviewAssignmentId,
                        principalTable: "ReviewAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewAssignmentId = table.Column<int>(type: "int", nullable: false),
                    ResponseId = table.Column<int>(type: "int", nullable: false),
                    ReviewerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionTaken = table.Column<int>(type: "int", nullable: false),
                    RequiresChange = table.Column<bool>(type: "bit", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewComments_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewComments_ReviewAssignments_ReviewAssignmentId",
                        column: x => x.ReviewAssignmentId,
                        principalTable: "ReviewAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewComments_Users_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewComments_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResponseWorkflow_Response_Unique",
                table: "ResponseWorkflows",
                column: "ResponseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseWorkflows_CurrentReviewerId",
                table: "ResponseWorkflows",
                column: "CurrentReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAssignments_AssignedById",
                table: "ReviewAssignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAssignments_CampaignAssignmentId",
                table: "ReviewAssignments",
                column: "CampaignAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAssignments_QuestionId",
                table: "ReviewAssignments",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAssignments_ReviewerId",
                table: "ReviewAssignments",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAuditLogs_CampaignAssignmentId",
                table: "ReviewAuditLogs",
                column: "CampaignAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAuditLogs_QuestionId",
                table: "ReviewAuditLogs",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAuditLogs_ResponseId",
                table: "ReviewAuditLogs",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAuditLogs_ReviewAssignmentId",
                table: "ReviewAuditLogs",
                column: "ReviewAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAuditLogs_UserId",
                table: "ReviewAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_ResolvedById",
                table: "ReviewComments",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_ResponseId",
                table: "ReviewComments",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_ReviewAssignmentId",
                table: "ReviewComments",
                column: "ReviewAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_ReviewerId",
                table: "ReviewComments",
                column: "ReviewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResponseWorkflows");

            migrationBuilder.DropTable(
                name: "ReviewAuditLogs");

            migrationBuilder.DropTable(
                name: "ReviewComments");

            migrationBuilder.DropTable(
                name: "ReviewAssignments");
        }
    }
}
