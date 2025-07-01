using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Services;

public interface IUnitService
{
    Task<List<Unit>> GetAllUnitsAsync();
    Task<List<Unit>> GetActiveUnitsAsync();
    Task<List<Unit>> GetUnitsByCategoryAsync(string category);
    Task<Dictionary<string, List<Unit>>> GetUnitsGroupedByCategoryAsync();
    Task<Unit?> GetUnitByCodeAsync(string code);
    Task<Unit?> GetUnitByIdAsync(int id);
}

public class UnitService : IUnitService
{
    private readonly ApplicationDbContext _context;

    public UnitService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Unit>> GetAllUnitsAsync()
    {
        return await _context.Units
            .OrderBy(u => u.Category)
            .ThenBy(u => u.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<Unit>> GetActiveUnitsAsync()
    {
        return await _context.Units
            .Where(u => u.IsActive)
            .OrderBy(u => u.Category)
            .ThenBy(u => u.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<Unit>> GetUnitsByCategoryAsync(string category)
    {
        return await _context.Units
            .Where(u => u.IsActive && u.Category == category)
            .OrderBy(u => u.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<Unit>>> GetUnitsGroupedByCategoryAsync()
    {
        var units = await GetActiveUnitsAsync();
        return units.GroupBy(u => u.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<Unit?> GetUnitByCodeAsync(string code)
    {
        return await _context.Units
            .FirstOrDefaultAsync(u => u.Code == code);
    }

    public async Task<Unit?> GetUnitByIdAsync(int id)
    {
        return await _context.Units
            .FirstOrDefaultAsync(u => u.Id == id);
    }
} 