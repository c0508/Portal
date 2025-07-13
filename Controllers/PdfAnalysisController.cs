using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using ESGPlatform.Services;
using ESGPlatform.Extensions;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace ESGPlatform.Controllers
{
    public class PdfAnalysisController : Controller
    {
        private const int ChunkSize = 15000;
        private const int overlapSize = 750;
        private readonly ILogger<PdfAnalysisController> _logger;
        private readonly string _onnxPath = "/Users/Charles.Kirby/Apps/Portal/Models/all-MiniLM-L12-v2-onnx/model.onnx";
        private readonly string _vocabPath = "/Users/Charles.Kirby/Apps/Portal/Models/all-MiniLM-L12-v2-onnx/vocab.txt";
        private readonly IPdfAnalysisService _pdfAnalysisService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IPdfTextCacheService _pdfTextCacheService;

        public PdfAnalysisController(ILogger<PdfAnalysisController> logger, IPdfAnalysisService pdfAnalysisService, IHttpClientFactory httpClientFactory, IConfiguration configuration, IPdfTextCacheService pdfTextCacheService)
        {
            _logger = logger;
            _pdfAnalysisService = pdfAnalysisService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _pdfTextCacheService = pdfTextCacheService;
        }

        [HttpGet]
        public IActionResult Analyze()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(IFormFile pdfFile, string question = null)
        {
            var swTotal = Stopwatch.StartNew();
            string extracted = "";
            string bestChunk = "";
            string error = null;
            string defaultQuestion = "What were the total direct emissions last year?";
            string finalQuestion = !string.IsNullOrWhiteSpace(question) ? question : defaultQuestion;
            string answer = null;

            try
            {
                if (pdfFile != null && pdfFile.Length > 0)
                {
                    // Get current organization ID for caching
                    var organizationId = HttpContext.GetCurrentUserOrganizationId();
                    
                    // Generate file hash for caching
                    var fileHash = _pdfTextCacheService.GenerateFileHash(pdfFile);
                    
                    // Check if text is already cached
                    var cachedText = await _pdfTextCacheService.GetCachedTextAsync(fileHash, organizationId);
                    
                    if (cachedText != null)
                    {
                        _logger.LogInformation("[PDF] Using cached text for organization {OrganizationId}, file hash: {FileHash}", 
                            organizationId, fileHash);
                        extracted = cachedText;
                    }
                    else
                    {
                        _logger.LogInformation("[PDF] Extraction started");
                        var sw = Stopwatch.StartNew();
                        
                        // iText7 extraction (new implementation)
                        using var stream = pdfFile.OpenReadStream();
                        using var pdfReader = new iText.Kernel.Pdf.PdfReader(stream);
                        using var pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader);
                        
                        var textBuilder = new StringBuilder();
                        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                        {
                            var page = pdfDocument.GetPage(i);
                            var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
                            var pageText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
                            textBuilder.AppendLine(pageText);
                        }
                        var text = textBuilder.ToString();
                        
                        // Original PdfPig implementation (commented out for easy rollback)
                        /*
                        using var stream = pdfFile.OpenReadStream();
                        using var doc = PdfDocument.Open(stream);
                        var text = string.Join(" ", doc.GetPages().Select(p => p.Text));
                        */

                        extracted = text;
                        
                        // Cache the extracted text
                        await _pdfTextCacheService.CacheTextAsync(fileHash, extracted, organizationId);
                        
                        await System.IO.File.WriteAllTextAsync("pdftext.txt", extracted);
                        sw.Stop();
                        _logger.LogInformation($"[PDF] Extraction finished in {sw.ElapsedMilliseconds} ms");
                    }

                    _logger.LogInformation("[Chunking] Started");
                    var swChunking = Stopwatch.StartNew();
                    var chunks = ChunkText(extracted, ChunkSize, overlapSize);
                    swChunking.Stop();
                    _logger.LogInformation($"[Chunking] Finished in {swChunking.ElapsedMilliseconds} ms. Total chunks: {chunks.Count}");

                    using var embedder = new MiniLmEmbeddingService(_onnxPath, _vocabPath, 128);

                    _logger.LogInformation("[Local] Getting embedding for question");
                    var swEmbedding = Stopwatch.StartNew();
                    var questionEmbedding = embedder.GetEmbedding(finalQuestion);
                    swEmbedding.Stop();
                    _logger.LogInformation($"[Local] Question embedding finished in {swEmbedding.ElapsedMilliseconds} ms");
                    if (questionEmbedding == null) throw new Exception("Failed to get question embedding");

                    var chunkEmbeddings = new List<(string chunk, float[] embedding)>();
                    int chunkIdx = 0;
                    foreach (var chunk in chunks)
                    {
                        var swChunkEmbedding = Stopwatch.StartNew();
                        var emb = embedder.GetEmbedding(chunk);
                        swChunkEmbedding.Stop();
                        if (emb != null)
                            chunkEmbeddings.Add((chunk, emb));
                        chunkIdx++;
                    }

                    _logger.LogInformation("[Similarity] Started");
                    var swSimilarity = Stopwatch.StartNew();
                    // Compute similarity for all chunks
                    var scoredChunks = new List<(string chunk, float score)>();
                    foreach (var (chunk, emb) in chunkEmbeddings)
                    {
                        float score = CosineSimilarity(questionEmbedding, emb);
                        scoredChunks.Add((chunk, score));
                    }
                    // Dynamically select chunks based on similarity threshold
                    const float similarityThreshold = 0.7f;
                    var relevantChunks = scoredChunks
                        .Where(x => x.score > similarityThreshold)
                        .OrderByDescending(x => x.score)
                        .Select(x => x.chunk)
                        .ToList();
                    
                    // If no chunks meet the threshold, take the top 3 as fallback
                    if (!relevantChunks.Any())
                    {
                        relevantChunks = scoredChunks
                            .OrderByDescending(x => x.score)
                            .Take(3)
                            .Select(x => x.chunk)
                            .ToList();
                        _logger.LogInformation($"[Similarity] No chunks met threshold {similarityThreshold}, using top 3 as fallback");
                    }
                    
                    var combinedChunks = string.Join("\n---\n", relevantChunks);
                    _logger.LogInformation($"[Similarity] Selected {relevantChunks.Count} chunks with score > {similarityThreshold}");
                    swSimilarity.Stop();
                    _logger.LogInformation($"[Similarity] Finished in {swSimilarity.ElapsedMilliseconds} ms");

                   

                    // Call OpenAI with the question and combined chunks
                    answer = await GetOpenAiAnswerAsync(finalQuestion, combinedChunks);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _logger.LogError(ex, "[Error] Exception in Analyze POST");
            }

            swTotal.Stop();
            _logger.LogInformation($"[Total] Analyze POST finished in {swTotal.ElapsedMilliseconds} ms");

            ViewBag.Question = finalQuestion;
            ViewBag.BestChunk = bestChunk;
            ViewBag.Answer = answer;
            ViewBag.Error = error;
            ViewBag.FileName = pdfFile?.FileName;
            return View();
        }

        private List<string> ChunkText(string text, int chunkSize, int overlapSize = 750)
        {
            var chunks = new List<string>();
            int stepSize = chunkSize - overlapSize;
            
            for (int i = 0; i < text.Length; i += stepSize)
            {
                int len = Math.Min(chunkSize, text.Length - i);
                chunks.Add(text.Substring(i, len));
            }
            return chunks;
        }

        private float CosineSimilarity(float[] a, float[] b)
        {
            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB));
        }

        // Helper to extract the first number (int or decimal) from text
        private string ExtractNumberFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(text, @"-?\d+(\.\d+)?");
            return match.Success ? match.Value : null;
        }

        // Helper to call OpenAI with question and chunk
        private async Task<string> GetOpenAiAnswerAsync(string question, string chunk)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _configuration["OpenAI:ApiKey"];
            var endpoint = _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            if (string.IsNullOrEmpty(apiKey))
                return "Not available (OpenAI API key not configured)";



            var prompt = $@"Answer the question based on the PDF content. Question:  {question}\n\nPDF Content :\n{chunk}\n\nRespond with only the answer as a string or 'Not available' if no answer is found.";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_tokens = 100
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return $"Not available (OpenAI error: {response.StatusCode})";

            try
            {
                var responseJson = System.Text.Json.JsonDocument.Parse(responseContent);
                var messageContent = responseJson.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                return messageContent?.Trim() ?? "Not available";
            }
            catch
            {
                return "Not available (could not parse OpenAI response)";
            }
        }
    }
} 