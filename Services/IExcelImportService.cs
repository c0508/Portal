using ESGPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace ESGPlatform.Services
{
    public interface IExcelImportService
    {
        Task<List<ExcelQuestionPreviewViewModel>> ParseExcelFileAsync(IFormFile file);
        byte[] GenerateQuestionnaireTemplate();
    }
} 