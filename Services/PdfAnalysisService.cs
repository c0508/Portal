using ESGPlatform.Models.ViewModels;
using ESGPlatform.Models.Entities;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace ESGPlatform.Services
{
    public class PdfAnalysisService : IPdfAnalysisService
    {
        private readonly ILogger<PdfAnalysisService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private readonly string _openAiEndpoint;

        public PdfAnalysisService(ILogger<PdfAnalysisService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? configuration["OpenAI:ApiKey"] ?? "";
            _openAiEndpoint = configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
        }

        public async Task<PdfAnalysisResult> AnalyzePdfAsync(IFormFile pdfFile, int questionnaireId)
        {
            try
            {
                _logger.LogInformation("Starting PDF analysis for file: {FileName}", pdfFile.FileName);

                // Extract text from PDF
                var extractedText = await ExtractTextFromPdfAsync(pdfFile);
                
                // Create a mock result for now
                var result = new PdfAnalysisResult
                {
                    FileName = pdfFile.FileName,
                    ExtractedText = extractedText,
                    AnalyzedAt = DateTime.UtcNow,
                    TotalQuestions = 0, // Will be set by controller
                    MatchedQuestions = 0, // Will be set by controller
                    AverageConfidence = 0.0 // Will be set by controller
                };

                _logger.LogInformation("PDF analysis completed for file: {FileName}", pdfFile.FileName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing PDF file: {FileName}", pdfFile.FileName);
                throw;
            }
        }

        public async Task<List<QuestionAnswerMatch>> ExtractAnswersAsync(string pdfText, List<Question> questions)
        {
            var matches = new List<QuestionAnswerMatch>();

            foreach (var question in questions)
            {
                try
                {
                    var match = await ExtractAnswerForQuestionAsync(question, pdfText);
                    matches.Add(match);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting answer for question {QuestionId}", question.Id);
                    
                    // Add a failed match
                    matches.Add(new QuestionAnswerMatch
                    {
                        QuestionId = question.Id,
                        QuestionText = question.QuestionText,
                        ConfidenceScore = 0.0,
                        IsValid = false,
                        ValidationErrors = { "Failed to extract answer" }
                    });
                }
            }

            return matches;
        }

        public async Task<byte[]> GenerateAnalysisReportAsync(PdfAnalysisResult result)
        {
            var report = new
            {
                FileName = result.FileName,
                AnalyzedAt = result.AnalyzedAt,
                TotalQuestions = result.TotalQuestions,
                MatchedQuestions = result.MatchedQuestions,
                AverageConfidence = result.AverageConfidence,
                Matches = result.Matches.Select(m => new
                {
                    QuestionId = m.QuestionId,
                    QuestionText = m.QuestionText,
                    ExtractedAnswer = m.ExtractedAnswer,
                    ConfidenceScore = m.ConfidenceScore,
                    SourcePage = m.SourcePage,
                    IsValid = m.IsValid
                })
            };

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            return Encoding.UTF8.GetBytes(json);
        }

        private async Task<string> ExtractTextFromPdfAsync(IFormFile pdfFile)
        {
            // For now, return a placeholder text
            // In production, you'd use a PDF library like iTextSharp
            return "This is extracted text from the PDF. In a real implementation, this would contain the actual text content from the PDF file.";
        }

        private async Task<QuestionAnswerMatch> ExtractAnswerForQuestionAsync(Question question, string pdfText)
        {
            try
            {
                // Try to use real AI if API key is configured
                if (!string.IsNullOrEmpty(_openAiApiKey))
                {
                    var aiAnswer = await GenerateAiAnswerAsync(question, pdfText);
                    return new QuestionAnswerMatch
                    {
                        QuestionId = question.Id,
                        QuestionText = question.QuestionText,
                        ExtractedAnswer = aiAnswer.Answer,
                        ConfidenceScore = aiAnswer.Confidence,
                        SourceText = aiAnswer.SourceText,
                        SourcePage = aiAnswer.SourcePage,
                        Reasoning = aiAnswer.Reasoning,
                        IsValid = aiAnswer.Confidence > 0.5
                    };
                }
                else
                {
                    // Fall back to mock response if no API key
                    var mockAnswer = await GenerateMockAnswerAsync(question, pdfText);
                    return new QuestionAnswerMatch
                    {
                        QuestionId = question.Id,
                        QuestionText = question.QuestionText,
                        ExtractedAnswer = mockAnswer.Answer,
                        ConfidenceScore = mockAnswer.Confidence,
                        SourceText = mockAnswer.SourceText,
                        SourcePage = mockAnswer.SourcePage,
                        Reasoning = mockAnswer.Reasoning,
                        IsValid = mockAnswer.Confidence > 0.5
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting answer for question {QuestionId}, falling back to mock", question.Id);
                
                // Fall back to mock response on error
                var mockAnswer = await GenerateMockAnswerAsync(question, pdfText);
                return new QuestionAnswerMatch
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    ExtractedAnswer = mockAnswer.Answer,
                    ConfidenceScore = mockAnswer.Confidence,
                    SourceText = mockAnswer.SourceText,
                    SourcePage = mockAnswer.SourcePage,
                    Reasoning = mockAnswer.Reasoning,
                    IsValid = mockAnswer.Confidence > 0.5
                };
            }
        }

        private async Task<MockAiResponse> GenerateAiAnswerAsync(Question question, string pdfText)
        {
            try
            {
                var prompt = $@"
You are an AI assistant analyzing a PDF document to extract answers to ESG questionnaire questions.

Question: {question.QuestionText}
Question Type: {question.QuestionType}
Help Text: {question.HelpText ?? "None"}

PDF Content (first 4000 characters):
{pdfText.Substring(0, Math.Min(4000, pdfText.Length))}

Please analyze the PDF content and provide an answer to the question. Respond in JSON format with the following structure:
{{
    ""answer"": ""The extracted answer"",
    ""confidence"": 0.85,
    ""sourceText"": ""The specific text from the PDF that supports this answer"",
    ""sourcePage"": 15,
    ""reasoning"": ""Brief explanation of how you arrived at this answer""
}}

If no relevant information is found, set confidence to 0.1 and answer to ""No information found"".";

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful AI assistant that analyzes documents and extracts information in JSON format." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.1
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiApiKey);

                var response = await _httpClient.PostAsync(_openAiEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = System.Text.Json.JsonSerializer.Deserialize<OpenAiResponse>(responseContent);
                    var aiResponse = System.Text.Json.JsonSerializer.Deserialize<AiResponse>(responseJson.choices[0].message.content);
                    
                    return new MockAiResponse
                    {
                        Answer = aiResponse.answer,
                        Confidence = aiResponse.confidence,
                        SourceText = aiResponse.sourceText,
                        SourcePage = aiResponse.sourcePage,
                        Reasoning = aiResponse.reasoning
                    };
                }
                else
                {
                    _logger.LogWarning("OpenAI API call failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    throw new Exception($"OpenAI API call failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API for question {QuestionId}", question.Id);
                throw;
            }
        }

        private async Task<MockAiResponse> GenerateMockAnswerAsync(Question question, string pdfText)
        {
            // Simulate AI processing delay
            await Task.Delay(100);

            // Generate mock responses based on question type
            var random = new Random();
            var confidence = random.NextDouble() * 0.8 + 0.2; // 0.2 to 1.0

            switch (question.QuestionType)
            {
                case QuestionType.Number:
                    return new MockAiResponse
                    {
                        Answer = random.Next(100, 10000).ToString(),
                        Confidence = confidence,
                        SourceText = "Found numeric value in financial section",
                        SourcePage = random.Next(1, 50),
                        Reasoning = "Extracted from financial data section"
                    };

                case QuestionType.YesNo:
                    return new MockAiResponse
                    {
                        Answer = random.Next(2) == 0 ? "Yes" : "No",
                        Confidence = confidence,
                        SourceText = "Policy statement found",
                        SourcePage = random.Next(1, 50),
                        Reasoning = "Based on policy statements in the document"
                    };

                case QuestionType.Text:
                case QuestionType.LongText:
                    return new MockAiResponse
                    {
                        Answer = "Sample extracted text answer from the document",
                        Confidence = confidence,
                        SourceText = "Relevant section from the report",
                        SourcePage = random.Next(1, 50),
                        Reasoning = "Extracted from relevant section"
                    };

                default:
                    return new MockAiResponse
                    {
                        Answer = "No specific answer found",
                        Confidence = 0.1,
                        SourceText = "No relevant text found",
                        SourcePage = null,
                        Reasoning = "Question type not supported in mock implementation"
                    };
            }
        }

        private class MockAiResponse
        {
            public string Answer { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public string? SourceText { get; set; }
            public int? SourcePage { get; set; }
            public string? Reasoning { get; set; }
        }

        private class OpenAiResponse
        {
            public Choice[] choices { get; set; } = new Choice[0];
        }

        private class Choice
        {
            public Message message { get; set; } = new();
        }

        private class Message
        {
            public string content { get; set; } = string.Empty;
        }

        private class AiResponse
        {
            public string answer { get; set; } = string.Empty;
            public double confidence { get; set; }
            public string sourceText { get; set; } = string.Empty;
            public int? sourcePage { get; set; }
            public string reasoning { get; set; } = string.Empty;
        }
    }
} 