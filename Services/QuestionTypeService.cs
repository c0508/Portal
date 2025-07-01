using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Services;

public interface IQuestionTypeService
{
    Task<List<QuestionTypeMaster>> GetAllQuestionTypesAsync();
    Task<List<QuestionTypeMaster>> GetActiveQuestionTypesAsync();
    Task<QuestionTypeMaster?> GetQuestionTypeByCodeAsync(string code);
    Task<QuestionTypeMaster?> GetQuestionTypeByIdAsync(int id);
    Task<QuestionTypeMaster?> GetQuestionTypeByEnumAsync(QuestionType questionType);
    Task<bool> RequiresOptionsAsync(int questionTypeId);
    Task<bool> RequiresOptionsAsync(string code);
    string GetEnumDisplayName(QuestionType questionType);
    QuestionType GetEnumFromCode(string code);
}

public class QuestionTypeService : IQuestionTypeService
{
    private readonly ApplicationDbContext _context;

    public QuestionTypeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<QuestionTypeMaster>> GetAllQuestionTypesAsync()
    {
        return await _context.QuestionTypes
            .OrderBy(qt => qt.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<QuestionTypeMaster>> GetActiveQuestionTypesAsync()
    {
        return await _context.QuestionTypes
            .Where(qt => qt.IsActive)
            .OrderBy(qt => qt.DisplayOrder)
            .ToListAsync();
    }

    public async Task<QuestionTypeMaster?> GetQuestionTypeByCodeAsync(string code)
    {
        return await _context.QuestionTypes
            .FirstOrDefaultAsync(qt => qt.Code == code);
    }

    public async Task<QuestionTypeMaster?> GetQuestionTypeByIdAsync(int id)
    {
        return await _context.QuestionTypes
            .FirstOrDefaultAsync(qt => qt.Id == id);
    }

    public async Task<QuestionTypeMaster?> GetQuestionTypeByEnumAsync(QuestionType questionType)
    {
        var code = questionType.ToString();
        return await GetQuestionTypeByCodeAsync(code);
    }

    public async Task<bool> RequiresOptionsAsync(int questionTypeId)
    {
        var questionType = await GetQuestionTypeByIdAsync(questionTypeId);
        return questionType?.RequiresOptions ?? false;
    }

    public async Task<bool> RequiresOptionsAsync(string code)
    {
        var questionType = await GetQuestionTypeByCodeAsync(code);
        return questionType?.RequiresOptions ?? false;
    }

    public string GetEnumDisplayName(QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.Text => "Text Input",
            QuestionType.LongText => "Long Text Area",
            QuestionType.Number => "Number Input",
            QuestionType.Date => "Date Picker",
            QuestionType.YesNo => "Yes/No Choice",
            QuestionType.Select => "Dropdown List",
            QuestionType.Radio => "Radio Buttons",
            QuestionType.MultiSelect => "Multi-Select List",
            QuestionType.Checkbox => "Checkbox Options",
            QuestionType.FileUpload => "File Upload",
            _ => questionType.ToString()
        };
    }

    public QuestionType GetEnumFromCode(string code)
    {
        return code switch
        {
            "Text" => QuestionType.Text,
            "LongText" => QuestionType.LongText,
            "Number" => QuestionType.Number,
            "Date" => QuestionType.Date,
            "YesNo" => QuestionType.YesNo,
            "Select" => QuestionType.Select,
            "Radio" => QuestionType.Radio,
            "MultiSelect" => QuestionType.MultiSelect,
            "Checkbox" => QuestionType.Checkbox,
            "FileUpload" => QuestionType.FileUpload,
            _ => QuestionType.Text
        };
    }
} 