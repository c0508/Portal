using ESGPlatform.Models.ViewModels;
using ESGPlatform.Models.Entities;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace ESGPlatform.Services
{
    public class PdfAnalysisService : IPdfAnalysisService
    {
        private readonly ILogger<PdfAnalysisService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private readonly string _openAiEndpoint;
        private readonly IPdfTextExtractor _pdfTextExtractor;

        public PdfAnalysisService(
            ILogger<PdfAnalysisService> logger, 
            HttpClient httpClient, 
            IConfiguration configuration,
            IPdfTextExtractor pdfTextExtractor)
        {
            _logger = logger;
            _httpClient = httpClient;
            _pdfTextExtractor = pdfTextExtractor;
            _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? configuration["OpenAI:ApiKey"] ?? "";
            _openAiEndpoint = configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            
            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                _logger.LogWarning("OpenAI API key is not configured. PDF analysis will use mock responses.");
            }
            else
            {
                _logger.LogInformation("OpenAI API key is configured. API key length: {Length}", _openAiApiKey.Length);
            }
        }

        public async Task<PdfAnalysisResult> AnalyzePdfAsync(IFormFile pdfFile, int questionnaireId)
        {
            try
            {
                _logger.LogInformation("Starting PDF analysis for file: {FileName}", pdfFile.FileName);

                // Extract text from PDF using the new text extractor
                var extractionResult = await _pdfTextExtractor.ExtractTextAsync(pdfFile);
                
                if (!extractionResult.IsSuccess)
                {
                    _logger.LogError("PDF text extraction failed for file: {FileName}. Error: {Error}", 
                        pdfFile.FileName, extractionResult.ErrorMessage);
                    
                    return new PdfAnalysisResult
                    {
                        FileName = pdfFile.FileName,
                        ExtractedText = "",
                        AnalyzedAt = DateTime.UtcNow,
                        TotalQuestions = 0,
                        MatchedQuestions = 0,
                        AverageConfidence = 0
                    };
                }

                if (string.IsNullOrWhiteSpace(extractionResult.Text))
                {
                    _logger.LogWarning("Extracted text is empty for file: {FileName}", pdfFile.FileName);
                    return new PdfAnalysisResult
                    {
                        FileName = pdfFile.FileName,
                        ExtractedText = "",
                        AnalyzedAt = DateTime.UtcNow,
                        TotalQuestions = 0,
                        MatchedQuestions = 0,
                        AverageConfidence = 0
                    };
                }

                // Log extraction details
                _logger.LogInformation("PDF text extraction completed for {FileName}. Extracted {TextLength} characters from {PageCount} pages in {ElapsedMs}ms. Requires OCR: {RequiresOcr}", 
                    pdfFile.FileName, extractionResult.Text.Length, extractionResult.PageCount, extractionResult.ExtractionTimeMs, extractionResult.RequiresOcr);

                // Check if extracted text is suspiciously short
                if (extractionResult.Text.Length < 50)
                {
                    _logger.LogWarning("Extracted text is very short ({Length} chars) for file: {FileName}", extractionResult.Text.Length, pdfFile.FileName);
                }

                return new PdfAnalysisResult
                {
                    FileName = pdfFile.FileName,
                    ExtractedText = extractionResult.Text,
                    AnalyzedAt = DateTime.UtcNow,
                    TotalQuestions = 0, // Will be set by controller
                    MatchedQuestions = 0, // Will be set by controller
                    AverageConfidence = 0 // Will be set by controller
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing PDF file: {FileName}", pdfFile.FileName);
                return new PdfAnalysisResult
                {
                    FileName = pdfFile.FileName,
                    ExtractedText = "",
                    AnalyzedAt = DateTime.UtcNow,
                    TotalQuestions = 0,
                    MatchedQuestions = 0,
                    AverageConfidence = 0
                };
            }
        }

        public async Task<List<QuestionAnswerMatch>> ExtractAnswersAsync(string pdfText, List<Question> questions)
        {
            var matches = new List<QuestionAnswerMatch>();
            
            // _logger.LogInformation("Starting answer extraction for {QuestionCount} questions.", questions.Count);
            
            foreach (var question in questions)
            {
                try
                {
                    // _logger.LogInformation("Extracting answer for question {QuestionId} ({QuestionType})", question.Id, question.QuestionType);
                    
                    var match = await ExtractAnswerForQuestionAsync(question, pdfText);
                    matches.Add(match);
                    
                    // _logger.LogInformation("Extracted answer for question {QuestionId}: Confidence={Confidence}, IsValid={IsValid}", 
                    //     question.Id, match.ConfidenceScore, match.IsValid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting answer for question {QuestionId}", question.Id);
                    matches.Add(new QuestionAnswerMatch
                    {
                        QuestionId = question.Id,
                        QuestionText = question.QuestionText,
                        ExtractedAnswer = "Error processing question",
                        ConfidenceScore = 0,
                        IsValid = false,
                        ValidationErrors = new List<string> { ex.Message }
                    });
                }
            }
            
            var validMatches = matches.Count(m => m.IsValid);
            var averageConfidence = matches.Any() ? matches.Average(m => m.ConfidenceScore) : 0;
            
            // _logger.LogInformation("Answer extraction complete. Valid matches: {ValidCount}/{TotalCount}, Average confidence: {AverageConfidence:F2}", 
            //     validMatches, matches.Count, averageConfidence);
            
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

        private async Task<QuestionAnswerMatch> ExtractAnswerForQuestionAsync(Question question, string pdfText)
        {
            try
            {
                // _logger.LogInformation("Calling OpenAI for question {QuestionId}", question.Id);
                
                var aiResponse = await GenerateAiAnswerAsync(question, pdfText);
                
                // _logger.LogInformation("OpenAI response for question {QuestionId}: Confidence={Confidence}", question.Id, aiResponse.Confidence);
                
                return new QuestionAnswerMatch
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    ExtractedAnswer = aiResponse.Answer,
                    ConfidenceScore = aiResponse.Confidence,
                    SourceText = aiResponse.SourceText,
                    SourcePage = aiResponse.SourcePage,
                    Reasoning = aiResponse.Reasoning,
                    IsValid = aiResponse.Confidence > 0.3, // Minimum confidence threshold
                    ValidationErrors = new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API for question {QuestionId}", question.Id);
                
                // Fallback to mock response
                return new QuestionAnswerMatch
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    ExtractedAnswer = "No information found",
                    ConfidenceScore = 0.1,
                    SourceText = "",
                    SourcePage = 0,
                    Reasoning = "Failed to process question due to API error",
                    IsValid = false,
                    ValidationErrors = new List<string> { ex.Message }
                };
            }
        }

        private async Task<MockAiResponse> GenerateAiAnswerAsync(Question question, string pdfText)
        {
            // If no API key is configured, use mock responses
            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                _logger.LogInformation("No OpenAI API key configured, using mock response for question {QuestionId}", question.Id);
                return await GenerateMockAnswerAsync(question, pdfText);
            }

            try
            {
                var prompt = $@"
You are an AI assistant analyzing a PDF document to extract answers to ESG questionnaire questions.

Question: {question.QuestionText}
Question Type: {question.QuestionType}
Help Text: {question.HelpText ?? "None"}

PDF Content (first 4000 characters):
{pdfText.Substring(0, Math.Min(4000, pdfText.Length))}

Please analyze the PDF content and provide an answer to the question. 

IMPORTANT: Respond with ONLY valid JSON, no markdown formatting, no code blocks, no additional text.

Required JSON structure:
{{
    ""answer"": ""The extracted answer"",
    ""confidence"": 0.85,
    ""sourceText"": ""The specific text from the PDF that supports this answer"",
    ""sourcePage"": 15,
    ""reasoning"": ""Brief explanation of how you arrived at this answer""
}}";

                // _logger.LogInformation("Sending OpenAI API request for question {QuestionId}. Prompt sample: {PromptSample}", 
                //     question.Id, prompt.Substring(0, Math.Min(200, prompt.Length)));

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.1,
                    max_tokens = 500
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Add Authorization header
                if (!string.IsNullOrEmpty(_openAiApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiApiKey);
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                stopwatch.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

                // _logger.LogInformation("OpenAI API response for question {QuestionId} received in {ElapsedMs:F2}ms. Status: {Status}. Response sample: {ResponseSample}", 
                //     question.Id, stopwatch.ElapsedMilliseconds, response.StatusCode, responseContent.Substring(0, Math.Min(200, responseContent.Length)));

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = System.Text.Json.JsonSerializer.Deserialize<OpenAiResponse>(responseContent);
                    var messageContent = responseJson.choices[0].message.content;
                    
                    // Clean the message content - remove markdown code blocks and trim whitespace
                    var cleanedContent = messageContent.Trim();
                    
                    // Remove markdown code blocks if present
                    if (cleanedContent.StartsWith("```json"))
                    {
                        cleanedContent = cleanedContent.Substring(7); // Remove ```json
                    }
                    if (cleanedContent.StartsWith("```"))
                    {
                        cleanedContent = cleanedContent.Substring(3); // Remove ```
                    }
                    if (cleanedContent.EndsWith("```"))
                    {
                        cleanedContent = cleanedContent.Substring(0, cleanedContent.Length - 3); // Remove ```
                    }
                    
                    cleanedContent = cleanedContent.Trim();

                    // _logger.LogInformation("Cleaned OpenAI response content for question {QuestionId}: {CleanedContent}", 
                    //     question.Id, cleanedContent.Substring(0, Math.Min(200, cleanedContent.Length)));

                    try
                    {
                        var aiResponse = System.Text.Json.JsonSerializer.Deserialize<AiResponse>(cleanedContent);
                        return new MockAiResponse
                        {
                            Answer = aiResponse.answer,
                            Confidence = aiResponse.confidence,
                            SourceText = aiResponse.sourceText,
                            SourcePage = aiResponse.sourcePage,
                            Reasoning = aiResponse.reasoning
                        };
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error parsing OpenAI JSON response for question {QuestionId}. Raw content: {RawContent}", 
                            question.Id, cleanedContent);
                        
                        // Fallback to mock response
                        return new MockAiResponse
                        {
                            Answer = "No information found",
                            Confidence = 0.1,
                            SourceText = "",
                            SourcePage = 0,
                            Reasoning = "Failed to parse AI response"
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("OpenAI API call failed for question {QuestionId}. Status: {Status}, Response: {Response}", 
                        question.Id, response.StatusCode, responseContent);
                    
                    // Fallback to mock response
                    return new MockAiResponse
                    {
                        Answer = "No information found",
                        Confidence = 0.1,
                        SourceText = "",
                        SourcePage = 0,
                        Reasoning = "API call failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API for question {QuestionId}", question.Id);
                
                // Fallback to mock response
                return new MockAiResponse
                {
                    Answer = "No information found",
                    Confidence = 0.1,
                    SourceText = "",
                    SourcePage = 0,
                    Reasoning = "Exception occurred during API call"
                };
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