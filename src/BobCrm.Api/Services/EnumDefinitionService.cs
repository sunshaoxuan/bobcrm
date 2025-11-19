using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 枚举定义管理服务
/// </summary>
public class EnumDefinitionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EnumDefinitionService> _logger;

    public EnumDefinitionService(AppDbContext db, ILogger<EnumDefinitionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有枚举定义
    /// </summary>
    public async Task<List<EnumDefinitionDto>> GetAllAsync(bool includeDisabled = false)
    {
        var query = _db.EnumDefinitions
            .Include(e => e.Options)
            .AsQueryable();

        if (!includeDisabled)
        {
            query = query.Where(e => e.IsEnabled);
        }

        var enums = await query.OrderBy(e => e.Code).ToListAsync();
        return enums.Select(MapToDto).ToList();
    }

    /// <summary>
    /// 根据ID获取枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> GetByIdAsync(Guid id)
    {
        var enumDef = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Id == id);

        return enumDef == null ? null : MapToDto(enumDef);
    }

    /// <summary>
    /// 根据Code获取枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> GetByCodeAsync(string code)
    {
        var enumDef = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == code);

        return enumDef == null ? null : MapToDto(enumDef);
    }

    /// <summary>
    /// 获取枚举的所有选项
    /// </summary>
    public async Task<List<EnumOptionDto>> GetOptionsAsync(Guid enumId, bool includeDisabled = false)
    {
        var query = _db.EnumOptions
            .Where(o => o.EnumDefinitionId == enumId);

        if (!includeDisabled)
        {
            query = query.Where(o => o.IsEnabled);
        }

        var options = await query.OrderBy(o => o.SortOrder).ToListAsync();
        return options.Select(MapOptionToDto).ToList();
    }

    /// <summary>
    /// 创建枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto> CreateAsync(CreateEnumDefinitionRequest request)
    {
        // 检查Code是否已存在
        if (await _db.EnumDefinitions.AnyAsync(e => e.Code == request.Code))
        {
            throw new InvalidOperationException($"枚举代码 '{request.Code}' 已存在");
        }

        var enumDef = new EnumDefinition
        {
            Code = request.Code,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsSystem = false,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 添加选项
        foreach (var optReq in request.Options)
        {
            enumDef.Options.Add(new EnumOption
            {
                Value = optReq.Value,
                DisplayName = optReq.DisplayName,
                Description = optReq.Description,
                SortOrder = optReq.SortOrder,
                IsEnabled = true,
                ColorTag = optReq.ColorTag,
                Icon = optReq.Icon
            });
        }

        _db.EnumDefinitions.Add(enumDef);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created enum definition: {Code}", request.Code);
        return MapToDto(enumDef);
    }

    /// <summary>
    /// 更新枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> UpdateAsync(Guid id, UpdateEnumDefinitionRequest request)
    {
        var enumDef = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enumDef == null)
        {
            return null;
        }

        // 系统枚举不允许修改某些属性
        if (enumDef.IsSystem)
        {
            _logger.LogWarning("Attempted to modify system enum: {Code}", enumDef.Code);
            throw new InvalidOperationException("系统枚举不可修改");
        }

        enumDef.DisplayName = request.DisplayName;
        enumDef.Description = request.Description;
        enumDef.IsEnabled = request.IsEnabled;
        enumDef.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated enum definition: {Code}", enumDef.Code);
        return MapToDto(enumDef);
    }

    /// <summary>
    /// 删除枚举定义
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var enumDef = await _db.EnumDefinitions.FindAsync(id);
        if (enumDef == null)
        {
            return false;
        }

        // 系统枚举不可删除
        if (enumDef.IsSystem)
        {
            throw new InvalidOperationException("系统枚举不可删除");
        }

        // 检查是否被字段引用
        var isReferenced = await _db.FieldMetadatas.AnyAsync(f => f.EnumDefinitionId == id);
        if (isReferenced)
        {
            throw new InvalidOperationException("该枚举正在被字段引用，无法删除");
        }

        _db.EnumDefinitions.Remove(enumDef);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted enum definition: {Code}", enumDef.Code);
        return true;
    }

    /// <summary>
    /// 批量更新枚举选项
    /// </summary>
    public async Task<List<EnumOptionDto>> UpdateOptionsAsync(Guid enumId, UpdateEnumOptionsRequest request)
    {
        var enumDef = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Id == enumId);

        if (enumDef == null)
        {
            throw new InvalidOperationException("枚举定义不存在");
        }

        if (enumDef.IsSystem)
        {
            throw new InvalidOperationException("系统枚举的选项不可修改");
        }

        // 更新现有选项
        foreach (var optReq in request.Options)
        {
            var existing = enumDef.Options.FirstOrDefault(o => o.Id == optReq.Id);
            if (existing != null)
            {
                existing.DisplayName = optReq.DisplayName;
                existing.Description = optReq.Description;
                existing.SortOrder = optReq.SortOrder;
                existing.IsEnabled = optReq.IsEnabled;
                existing.ColorTag = optReq.ColorTag;
                existing.Icon = optReq.Icon;
            }
        }

        enumDef.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated options for enum: {Code}", enumDef.Code);
        return enumDef.Options.Select(MapOptionToDto).ToList();
    }

    // 映射方法
    private static EnumDefinitionDto MapToDto(EnumDefinition entity)
    {
        return new EnumDefinitionDto
        {
            Id = entity.Id,
            Code = entity.Code,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            IsSystem = entity.IsSystem,
            IsEnabled = entity.IsEnabled,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Options = entity.Options.Select(MapOptionToDto).ToList()
        };
    }

    private static EnumOptionDto MapOptionToDto(EnumOption entity)
    {
        return new EnumOptionDto
        {
            Id = entity.Id,
            Value = entity.Value,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            SortOrder = entity.SortOrder,
            IsEnabled = entity.IsEnabled,
            ColorTag = entity.ColorTag,
            Icon = entity.Icon
        };
    }
}
