using Microsoft.AspNetCore.Http;

namespace ESGPlatform.Services
{
    /// <summary>
    /// Interface for PDF text extraction to allow easy switching between different PDF libraries
    /// </summary>
    public interface IPdfTextExtractor
    {
        /// <summary>
        /// Extracts text content from a PDF file
        /// </summary>
        /// <param name="pdfFile">The PDF file to extract text from</param>
        /// <returns>Extracted text content with metadata</returns>
        Task<PdfTextExtractionResult> ExtractTextAsync(IFormFile pdfFile);
    }

    /// <summary>
    /// Result of PDF text extraction with metadata
    /// </summary>
    public class PdfTextExtractionResult
    {
        /// <summary>
        /// The extracted text content
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Number of pages in the PDF
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// Whether the extraction was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if extraction failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Text extraction method used (for debugging/logging)
        /// </summary>
        public string ExtractionMethod { get; set; } = string.Empty;

        /// <summary>
        /// Whether the PDF appears to be scanned/OCR required
        /// </summary>
        public bool RequiresOcr { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Extraction duration in milliseconds
        /// </summary>
        public long ExtractionTimeMs { get; set; }
    }
} 