using ESGPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Text;

namespace ESGPlatform.Services
{
    public class ExcelImportService : IExcelImportService
    {
        private readonly Dictionary<string, string> _questionTypeMapping = new()
        {
            { "text", "Text" },
            { "longtext", "LongText" },
            { "number", "Number" },
            { "date", "Date" },
            { "yesno", "YesNo" },
            { "radio", "Radio" },
            { "select", "Select" },
            { "dropdown", "Select" },
            { "multiselect", "MultiSelect" },
            { "checkbox", "Checkbox" },
            { "fileupload", "FileUpload" }
        };

        public async Task<List<ExcelQuestionPreviewViewModel>> ParseExcelFileAsync(IFormFile file)
        {
            var questions = new List<ExcelQuestionPreviewViewModel>();

            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                throw new ArgumentException("Excel file contains no worksheets");
            }

            // Expected columns: Question Text, Help Text, Section, Question Type, Required, Options
            var headerRow = 1;
            var startRow = 2;

            // Validate headers
            var expectedHeaders = new[] { "Question Text", "Help Text", "Section", "Question Type", "Required", "Options" };
            var actualHeaders = new List<string>();

            for (int col = 1; col <= expectedHeaders.Length; col++)
            {
                var headerCell = worksheet.Cells[headerRow, col];
                actualHeaders.Add(headerCell.Value?.ToString()?.Trim() ?? "");
            }

            // Check if headers match (allow for some flexibility)
            var missingHeaders = expectedHeaders.Where(h => !actualHeaders.Any(a => 
                string.Equals(a, h, StringComparison.OrdinalIgnoreCase))).ToList();

            if (missingHeaders.Any())
            {
                throw new ArgumentException($"Missing required headers: {string.Join(", ", missingHeaders)}");
            }

            // Parse data rows
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            for (int row = startRow; row <= rowCount; row++)
            {
                var questionText = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                
                // Skip empty rows
                if (string.IsNullOrWhiteSpace(questionText))
                    continue;

                var question = new ExcelQuestionPreviewViewModel
                {
                    RowNumber = row,
                    QuestionText = questionText,
                    HelpText = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                    Section = worksheet.Cells[row, 3].Value?.ToString()?.Trim(),
                    QuestionType = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "",
                    IsRequired = ParseBooleanValue(worksheet.Cells[row, 5].Value?.ToString()),
                    Options = NormalizeExcelLineBreaks(worksheet.Cells[row, 6].Value?.ToString()?.Trim()),
                    IsPercentage = ParseBooleanValue(worksheet.Cells[row, 7].Value?.ToString()),
                    Unit = worksheet.Cells[row, 8].Value?.ToString()?.Trim()
                };

                // Validate the question
                ValidateQuestion(question);
                questions.Add(question);
            }

            return questions;
        }

        public byte[] GenerateQuestionnaireTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Questions");

            // Add headers
            var headers = new[] { "Question Text", "Help Text", "Section", "Question Type", "Required", "Options", "Is Percentage", "Unit" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Add sample data
            worksheet.Cells[2, 1].Value = "What is your company's annual revenue?";
            worksheet.Cells[2, 2].Value = "Please provide the most recent annual revenue figure";
            worksheet.Cells[2, 3].Value = "Financial";
            worksheet.Cells[2, 4].Value = "Number";
            worksheet.Cells[2, 5].Value = "Yes";
            worksheet.Cells[2, 6].Value = "";
            worksheet.Cells[2, 7].Value = "No";
            worksheet.Cells[2, 8].Value = "";

            worksheet.Cells[3, 1].Value = "What percentage of your energy comes from renewable sources?";
            worksheet.Cells[3, 2].Value = "Enter the percentage of renewable energy in your total energy mix";
            worksheet.Cells[3, 3].Value = "Environmental";
            worksheet.Cells[3, 4].Value = "Number";
            worksheet.Cells[3, 5].Value = "Yes";
            worksheet.Cells[3, 6].Value = "";
            worksheet.Cells[3, 7].Value = "Yes";
            worksheet.Cells[3, 8].Value = "";

            worksheet.Cells[4, 1].Value = "What is your total energy consumption?";
            worksheet.Cells[4, 2].Value = "Please provide your annual energy consumption";
            worksheet.Cells[4, 3].Value = "Environmental";
            worksheet.Cells[4, 4].Value = "Number";
            worksheet.Cells[4, 5].Value = "Yes";
            worksheet.Cells[4, 6].Value = "";
            worksheet.Cells[4, 7].Value = "No";
            worksheet.Cells[4, 8].Value = "MWh";

            worksheet.Cells[5, 1].Value = "Does your company have a sustainability policy?";
            worksheet.Cells[5, 2].Value = "Please indicate if you have a formal sustainability policy";
            worksheet.Cells[5, 3].Value = "Governance";
            worksheet.Cells[5, 4].Value = "YesNo";
            worksheet.Cells[5, 5].Value = "Yes";
            worksheet.Cells[5, 6].Value = "";
            worksheet.Cells[5, 7].Value = "No";
            worksheet.Cells[5, 8].Value = "";

            worksheet.Cells[6, 1].Value = "What type of organization best describes your company?";
            worksheet.Cells[6, 2].Value = "Select the category that best fits your organization";
            worksheet.Cells[6, 3].Value = "General";
            worksheet.Cells[6, 4].Value = "Select";
            worksheet.Cells[6, 5].Value = "Yes";
            worksheet.Cells[6, 6].Value = "Private Company\nPublic Company\nNon-Profit\nGovernment\nOther";
            worksheet.Cells[6, 7].Value = "No";
            worksheet.Cells[6, 8].Value = "";
            
            // Add an example with radio buttons
            worksheet.Cells[7, 1].Value = "How would you rate your sustainability maturity?";
            worksheet.Cells[7, 2].Value = "Select your current sustainability program maturity level";
            worksheet.Cells[7, 3].Value = "Assessment";
            worksheet.Cells[7, 4].Value = "Radio";
            worksheet.Cells[7, 5].Value = "Yes";
            worksheet.Cells[7, 6].Value = "Beginner\nDeveloping\nMature\nAdvanced\nIndustry Leading";
            worksheet.Cells[7, 7].Value = "No";
            worksheet.Cells[7, 8].Value = "";

            // Add instructions worksheet
            var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
            instructionsSheet.Cells[1, 1].Value = "Questionnaire Import Template Instructions";
            instructionsSheet.Cells[1, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[1, 1].Style.Font.Size = 16;

            var instructions = new[]
            {
                "",
                "Column Descriptions:",
                "• Question Text: The main question text (required)",
                "• Help Text: Additional help or guidance for the question (optional)",
                "• Section: Group questions by section (optional, will default to 'Other')",
                "• Question Type: The type of input field (required)",
                "• Required: Whether the question is mandatory (Yes/No)",
                "• Options: For Select/Radio/MultiSelect questions, enter options separated by new lines",
                "• Is Percentage: For Number questions only - makes it a percentage question (Yes/No)",
                "• Unit: For Number questions only - adds a unit suffix (e.g., MWh, kg, km)",
                "",
                "Supported Question Types:",
                "• Text: Single line text input",
                "• LongText: Multi-line text area",
                "• Number: Numeric input",
                "• Date: Date picker",
                "• YesNo: Yes/No radio buttons",
                "• Radio: Single selection from multiple options",
                "• Select: Dropdown selection",
                "• MultiSelect: Multiple selections allowed",
                "• Checkbox: Single checkbox",
                "• FileUpload: File upload field",
                "",
                "Tips:",
                "• Fill out the sample rows in the Questions sheet as examples",
                "• Delete sample rows and add your own questions",
                "• For option-based questions (Select, Radio, MultiSelect), use the Options column",
                "• In Excel, press Alt+Enter to create new lines within a cell for multiple options",
                "• Each option should be on a separate line within the same cell",
                "• Required column accepts: Yes, No, True, False, 1, 0",
                "• For Number questions: use either percentage OR unit, not both",
                "• Is Percentage and Unit columns only apply to Number questions",
                "• Common units: MWh, kWh, kg, tonnes, km, miles, tCO2e, etc.",
                "",
                "Example for Options column:",
                "• Option 1",
                "• Option 2",
                "• Option 3",
                "(Use Alt+Enter between each option in the same cell)"
            };

            for (int i = 0; i < instructions.Length; i++)
            {
                instructionsSheet.Cells[i + 2, 1].Value = instructions[i];
                if (instructions[i].StartsWith("•"))
                {
                    instructionsSheet.Cells[i + 2, 1].Style.Indent = 1;
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            instructionsSheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        private void ValidateQuestion(ExcelQuestionPreviewViewModel question)
        {
            // Validate question text
            if (string.IsNullOrWhiteSpace(question.QuestionText))
            {
                question.ValidationErrors.Add("Question text is required");
            }
            else if (question.QuestionText.Length > 500)
            {
                question.ValidationErrors.Add("Question text must be 500 characters or less");
            }

            // Validate question type
            if (string.IsNullOrWhiteSpace(question.QuestionType))
            {
                question.ValidationErrors.Add("Question type is required");
            }
            else if (!_questionTypeMapping.ContainsKey(question.QuestionType.ToLower()))
            {
                question.ValidationErrors.Add($"Invalid question type '{question.QuestionType}'. Supported types: {string.Join(", ", _questionTypeMapping.Keys)}");
            }
            else
            {
                // Normalize the question type
                question.QuestionType = _questionTypeMapping[question.QuestionType.ToLower()];
            }

            // Validate options for option-based question types
            var optionBasedTypes = new[] { "Radio", "Select", "MultiSelect" };
            if (optionBasedTypes.Contains(question.QuestionType) && string.IsNullOrWhiteSpace(question.Options))
            {
                question.ValidationErrors.Add($"Options are required for {question.QuestionType} questions");
            }

            // Validate help text length
            if (!string.IsNullOrEmpty(question.HelpText) && question.HelpText.Length > 1000)
            {
                question.ValidationErrors.Add("Help text must be 1000 characters or less");
            }

            // Validate section length
            if (!string.IsNullOrEmpty(question.Section) && question.Section.Length > 100)
            {
                question.ValidationErrors.Add("Section name must be 100 characters or less");
            }

            // Validate numeric enhancements
            if (question.QuestionType == "Number")
            {
                // Validate mutual exclusivity of percentage and unit
                if (question.IsPercentage && !string.IsNullOrEmpty(question.Unit))
                {
                    question.ValidationErrors.Add("A question cannot be both percentage and have a unit");
                }

                // Validate unit length
                if (!string.IsNullOrEmpty(question.Unit) && question.Unit.Length > 50)
                {
                    question.ValidationErrors.Add("Unit must be 50 characters or less");
                }
            }
            else
            {
                // Clear percentage and unit for non-numeric questions
                if (question.IsPercentage)
                {
                    question.ValidationErrors.Add("Only Number questions can be percentage questions");
                }
                if (!string.IsNullOrEmpty(question.Unit))
                {
                    question.ValidationErrors.Add("Only Number questions can have units");
                }
            }
        }

        private static bool ParseBooleanValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalizedValue = value.Trim().ToLower();
            return normalizedValue == "yes" || normalizedValue == "true" || normalizedValue == "1";
        }

        private static string? NormalizeExcelLineBreaks(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            // Excel stores multi-line content with actual line breaks (\r\n, \n, or \r)
            // We need to normalize them to \n for consistent processing
            return value.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        }
    }
} 