using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class MoveReportingPeriodFromQuestionnairesToCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportingPeriodEnd",
                table: "Questionnaires");

            migrationBuilder.DropColumn(
                name: "ReportingPeriodStart",
                table: "Questionnaires");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportingPeriodEnd",
                table: "Campaigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportingPeriodStart",
                table: "Campaigns",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportingPeriodEnd",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "ReportingPeriodStart",
                table: "Campaigns");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportingPeriodEnd",
                table: "Questionnaires",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportingPeriodStart",
                table: "Questionnaires",
                type: "datetime2",
                nullable: true);
        }
    }
}
