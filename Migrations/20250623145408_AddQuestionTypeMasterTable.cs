using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTypeMasterTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuestionTypeMasterId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InputType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequiresOptions = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionTypeMasterId",
                table: "Questions",
                column: "QuestionTypeMasterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_QuestionTypes_QuestionTypeMasterId",
                table: "Questions",
                column: "QuestionTypeMasterId",
                principalTable: "QuestionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Seed QuestionTypes data
            migrationBuilder.InsertData(
                table: "QuestionTypes",
                columns: new[] { "Code", "Name", "Description", "InputType", "RequiresOptions", "IsActive", "DisplayOrder", "CreatedAt" },
                values: new object[,]
                {
                    { "Text", "Text Input", "Single line text input for short answers", "text", false, true, 1, DateTime.UtcNow },
                    { "LongText", "Long Text Area", "Multi-line text area for detailed responses", "textarea", false, true, 2, DateTime.UtcNow },
                    { "Number", "Number Input", "Numeric input for quantities, percentages, or calculations", "number", false, true, 3, DateTime.UtcNow },
                    { "Date", "Date Picker", "Date selection for deadlines, milestones, or time-based data", "date", false, true, 4, DateTime.UtcNow },
                    { "YesNo", "Yes/No Choice", "Simple binary choice for compliance or boolean questions", "radio", false, true, 5, DateTime.UtcNow },
                    { "Select", "Dropdown List", "Single selection from a predefined list of options", "select", true, true, 6, DateTime.UtcNow },
                    { "Radio", "Radio Buttons", "Single selection displayed as radio buttons", "radio", true, true, 7, DateTime.UtcNow },
                    { "MultiSelect", "Multi-Select List", "Multiple selections from a predefined list", "multiselect", true, true, 8, DateTime.UtcNow },
                    { "Checkbox", "Checkbox Options", "Multiple checkbox selections", "checkbox", true, true, 9, DateTime.UtcNow },
                    { "FileUpload", "File Upload", "Document or file attachment for evidence or supporting materials", "file", false, true, 10, DateTime.UtcNow }
                });

            // Update existing Questions to reference the QuestionTypes table
            // Map enum values to master table IDs
            migrationBuilder.Sql(@"
                UPDATE Questions 
                SET QuestionTypeMasterId = CASE QuestionType
                    WHEN 0 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'Text')
                    WHEN 1 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'LongText')
                    WHEN 2 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'Number')
                    WHEN 3 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'Select')
                    WHEN 4 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'MultiSelect')
                    WHEN 5 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'Radio')
                    WHEN 6 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'Checkbox')
                    WHEN 7 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'YesNo')
                    WHEN 8 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'Date')
                    WHEN 9 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'FileUpload')
                    ELSE (SELECT Id FROM QuestionTypes WHERE Code = 'Text')
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Clear the foreign key reference before dropping the table
            migrationBuilder.Sql("UPDATE Questions SET QuestionTypeMasterId = NULL");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_QuestionTypes_QuestionTypeMasterId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "QuestionTypes");

            migrationBuilder.DropIndex(
                name: "IX_Questions_QuestionTypeMasterId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionTypeMasterId",
                table: "Questions");
        }
    }
}
