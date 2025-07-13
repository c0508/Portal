using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using System.Diagnostics;

namespace ESGPlatform.Services
{
    /// <summary>
    /// PdfPig implementation of PDF text extraction
    /// </summary>
    public class PdfPigTextExtractor : IPdfTextExtractor
    {
        private readonly ILogger<PdfPigTextExtractor> _logger;

        public PdfPigTextExtractor(ILogger<PdfPigTextExtractor> logger)
        {
            _logger = logger;
        }

        public async Task<PdfTextExtractionResult> ExtractTextAsync(IFormFile pdfFile)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new PdfTextExtractionResult
            {
                FileSizeBytes = pdfFile.Length,
                ExtractionMethod = "PdfPig",
                IsSuccess = false
            };

            try
            {
                _logger.LogInformation("Starting PDF text extraction for file: {FileName} ({Size} bytes)", 
                    pdfFile.FileName, pdfFile.Length);

                // Read the PDF file into memory
                using var stream = pdfFile.OpenReadStream();
                using var document = PdfDocument.Open(stream);

                result.PageCount = document.NumberOfPages;
                _logger.LogInformation("PDF has {PageCount} pages", result.PageCount);

                // Check if PDF appears to be scanned (no text content)
                var hasTextContent = false;
                var extractedText = new List<string>();

                // Extract text from each page
                for (int pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
                {
                    var page = document.GetPage(pageNumber);
                    var pageText = ExtractTextFromPage(page);
                    
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        hasTextContent = true;
                        extractedText.Add($"--- Page {pageNumber} ---");
                        extractedText.Add(pageText);
                        extractedText.Add(""); // Empty line between pages
                    }
                    else
                    {
                        _logger.LogWarning("Page {PageNumber} appears to have no extractable text", pageNumber);
                    }
                }

                result.Text = string.Join("\n", extractedText);
                result.RequiresOcr = !hasTextContent && result.PageCount > 0;
                result.IsSuccess = true;

                stopwatch.Stop();
                result.ExtractionTimeMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("PDF text extraction completed for {FileName}. Extracted {TextLength} characters from {PageCount} pages in {ElapsedMs}ms. Requires OCR: {RequiresOcr}", 
                    pdfFile.FileName, result.Text.Length, result.PageCount, result.ExtractionTimeMs, result.RequiresOcr);

                // Log warning if text seems too short
                if (result.Text.Length < 100 && result.PageCount > 0)
                {
                    _logger.LogWarning("Extracted text seems very short ({TextLength} chars) for a {PageCount}-page PDF. This might indicate a scanned document.", 
                        result.Text.Length, result.PageCount);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ExtractionTimeMs = stopwatch.ElapsedMilliseconds;
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(ex, "Error extracting text from PDF file: {FileName}", pdfFile.FileName);
                return result;
            }
        }

        private string ExtractTextFromPage(Page page)
        {
            var textBuilder = new List<string>();

            try
            {
                // Extract text from words (primary method)
                var words = page.GetWords();
                foreach (var word in words)
                {
                    textBuilder.Add(word.Text);
                }

                // Extract text from letters (fallback for some PDFs)
                if (textBuilder.Count == 0)
                {
                    var letters = page.Letters;
                    foreach (var letter in letters)
                    {
                        textBuilder.Add(letter.Value);
                    }
                }

                return string.Join(" ", textBuilder);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting text from page {PageNumber}", page.Number);
                return string.Empty;
            }
        }
    }
} 