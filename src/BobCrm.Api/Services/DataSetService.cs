using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs.DataSet;
using BobCrm.Api.Contracts.Requests.DataSet;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

/// <summary>
/// 数据集服务 - 管理数据集定义和执行
/// </summary>
public class DataSetService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DataSetService> _logger;
    private readonly Dictionary<string, IDataSourceHandler> _handlers;

    public DataSetService(
        AppDbContext db,
        ILogger<DataSetService> logger,
        IEnumerable<IDataSourceHandler> handlers)
    {
        _db = db;
        _logger = logger;

        // 注册所有 IDataSourceHandler 实现，按 TypeCode 索引
        _handlers = handlers.ToDictionary(h => h.TypeCode, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("DataSetService initialized with {Count} handlers: {Handlers}",
            _handlers.Count, string.Join(", ", _handlers.Keys));
    }

    /// <summary>
    /// 获取所有数据集
    /// </summary>
    public async Task<List<DataSetDto>> GetAllAsync(CancellationToken ct = default)
    {
        var dataSets = await _db.DataSets
            .AsNoTracking()
            .Include(d => d.QueryDefinition)
            .Include(d => d.PermissionFilter)
            .Where(d => d.IsEnabled)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        return dataSets.Select(MapToDto).ToList();
    }

    /// <summary>
    /// 根据ID获取数据集
    /// </summary>
    public async Task<DataSetDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dataSet = await _db.DataSets
            .AsNoTracking()
            .Include(d => d.QueryDefinition)
            .Include(d => d.PermissionFilter)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        return dataSet == null ? null : MapToDto(dataSet);
    }

    /// <summary>
    /// 根据Code获取数据集
    /// </summary>
    public async Task<DataSetDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var dataSet = await _db.DataSets
            .AsNoTracking()
            .Include(d => d.QueryDefinition)
            .Include(d => d.PermissionFilter)
            .FirstOrDefaultAsync(d => d.Code == code, ct);

        return dataSet == null ? null : MapToDto(dataSet);
    }

    /// <summary>
    /// 创建数据集
    /// </summary>
    public async Task<DataSetDto> CreateAsync(CreateDataSetRequest request, CancellationToken ct = default)
    {
        // 验证 Code 唯一性
        var exists = await _db.DataSets.AnyAsync(d => d.Code == request.Code, ct);
        if (exists)
        {
            throw new InvalidOperationException($"DataSet with code '{request.Code}' already exists");
        }

        // 验证数据源类型
        var typeExists = await _db.DataSourceTypes.AnyAsync(t => t.Code == request.DataSourceTypeCode, ct);
        if (!typeExists)
        {
            throw new InvalidOperationException($"DataSourceType '{request.DataSourceTypeCode}' not found");
        }

        // 验证配置
        if (!string.IsNullOrWhiteSpace(request.ConfigJson))
        {
            var handler = GetHandler(request.DataSourceTypeCode);
            var validationResult = await handler.ValidateConfigAsync(request.ConfigJson);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
            }
        }

        var dataSet = new DataSet
        {
            Code = request.Code,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            DataSourceTypeCode = request.DataSourceTypeCode,
            ConfigJson = request.ConfigJson,
            FieldsJson = request.FieldsJson,
            SupportsPaging = request.SupportsPaging,
            SupportsSorting = request.SupportsSorting,
            DefaultSortField = request.DefaultSortField,
            DefaultSortDirection = request.DefaultSortDirection,
            DefaultPageSize = request.DefaultPageSize,
            QueryDefinitionId = request.QueryDefinitionId,
            PermissionFilterId = request.PermissionFilterId,
            IsSystem = request.IsSystem,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = request.CreatedBy
        };

        _db.DataSets.Add(dataSet);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created DataSet {Code} (ID: {Id})", dataSet.Code, dataSet.Id);

        return MapToDto(dataSet);
    }

    /// <summary>
    /// 更新数据集
    /// </summary>
    public async Task<DataSetDto> UpdateAsync(int id, UpdateDataSetRequest request, CancellationToken ct = default)
    {
        var dataSet = await _db.DataSets.FindAsync(new object[] { id }, ct);
        if (dataSet == null)
        {
            throw new InvalidOperationException($"DataSet {id} not found");
        }

        // 验证配置
        if (!string.IsNullOrWhiteSpace(request.ConfigJson))
        {
            var handler = GetHandler(dataSet.DataSourceTypeCode);
            var validationResult = await handler.ValidateConfigAsync(request.ConfigJson);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
            }
        }

        dataSet.Name = request.Name;
        dataSet.DisplayName = request.DisplayName;
        dataSet.Description = request.Description;
        dataSet.ConfigJson = request.ConfigJson;
        dataSet.FieldsJson = request.FieldsJson;
        dataSet.SupportsPaging = request.SupportsPaging;
        dataSet.SupportsSorting = request.SupportsSorting;
        dataSet.DefaultSortField = request.DefaultSortField;
        dataSet.DefaultSortDirection = request.DefaultSortDirection;
        dataSet.DefaultPageSize = request.DefaultPageSize;
        dataSet.QueryDefinitionId = request.QueryDefinitionId;
        dataSet.PermissionFilterId = request.PermissionFilterId;
        dataSet.IsEnabled = request.IsEnabled;
        dataSet.UpdatedAt = DateTime.UtcNow;
        dataSet.UpdatedBy = request.UpdatedBy;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated DataSet {Code} (ID: {Id})", dataSet.Code, dataSet.Id);

        return MapToDto(dataSet);
    }

    /// <summary>
    /// 删除数据集
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var dataSet = await _db.DataSets.FindAsync(new object[] { id }, ct);
        if (dataSet == null)
        {
            throw new InvalidOperationException($"DataSet {id} not found");
        }

        if (dataSet.IsSystem)
        {
            throw new InvalidOperationException("Cannot delete system DataSet");
        }

        _db.DataSets.Remove(dataSet);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted DataSet {Code} (ID: {Id})", dataSet.Code, dataSet.Id);
    }

    /// <summary>
    /// 执行数据集查询
    /// </summary>
    public async Task<DataSetExecutionResponse> ExecuteAsync(
        int id,
        DataSetExecutionRequest request,
        CancellationToken ct = default)
    {
        var dataSet = await _db.DataSets
            .AsNoTracking()
            .Include(d => d.QueryDefinition)
            .Include(d => d.PermissionFilter)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (dataSet == null)
        {
            throw new InvalidOperationException($"DataSet {id} not found");
        }

        if (!dataSet.IsEnabled)
        {
            throw new InvalidOperationException($"DataSet {id} is disabled");
        }

        // 获取对应的 handler
        var handler = GetHandler(dataSet.DataSourceTypeCode);

        // 构建执行请求
        var handlerRequest = new DataSourceExecutionRequest
        {
            TypeCode = dataSet.DataSourceTypeCode,
            ConfigJson = dataSet.ConfigJson ?? "{}",
            Page = request.Page,
            PageSize = request.PageSize.HasValue ? request.PageSize.Value : dataSet.DefaultPageSize,
            SortField = request.SortField ?? dataSet.DefaultSortField,
            SortDirection = request.SortDirection ?? dataSet.DefaultSortDirection,
            RuntimeParametersJson = request.RuntimeParametersJson,
            RuntimeContext = request.RuntimeContext
        };

        // TODO: 应用 QueryDefinition 条件
        // TODO: 应用 PermissionFilter 数据权限

        // 执行查询
        var startTime = DateTime.UtcNow;
        var result = await handler.ExecuteAsync(handlerRequest, ct);
        var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation(
            "Executed DataSet {Code} in {Ms}ms, returned {Count} records",
            dataSet.Code, executionTime, result.TotalCount);

        return new DataSetExecutionResponse
        {
            DataSetId = dataSet.Id,
            DataSetCode = dataSet.Code,
            DataJson = result.DataJson,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            AppliedScopes = result.AppliedScopes,
            ExecutionTimeMs = (long)executionTime
        };
    }

    /// <summary>
    /// 获取数据集字段元数据
    /// </summary>
    public async Task<List<DataSourceFieldMetadata>> GetFieldsAsync(int id, CancellationToken ct = default)
    {
        var dataSet = await _db.DataSets
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (dataSet == null)
        {
            throw new InvalidOperationException($"DataSet {id} not found");
        }

        var handler = GetHandler(dataSet.DataSourceTypeCode);
        return await handler.GetFieldsAsync(dataSet.ConfigJson ?? "{}", ct);
    }

    /// <summary>
    /// 获取指定类型的 handler
    /// </summary>
    private IDataSourceHandler GetHandler(string typeCode)
    {
        if (!_handlers.TryGetValue(typeCode, out var handler))
        {
            throw new InvalidOperationException(
                $"No handler registered for data source type '{typeCode}'");
        }
        return handler;
    }

    /// <summary>
    /// 映射到 DTO
    /// </summary>
    private static DataSetDto MapToDto(DataSet dataSet)
    {
        return new DataSetDto
        {
            Id = dataSet.Id,
            Code = dataSet.Code,
            Name = dataSet.Name,
            DisplayName = dataSet.DisplayName,
            Description = dataSet.Description,
            DataSourceTypeCode = dataSet.DataSourceTypeCode,
            ConfigJson = dataSet.ConfigJson,
            FieldsJson = dataSet.FieldsJson,
            SupportsPaging = dataSet.SupportsPaging,
            SupportsSorting = dataSet.SupportsSorting,
            DefaultSortField = dataSet.DefaultSortField,
            DefaultSortDirection = dataSet.DefaultSortDirection,
            DefaultPageSize = dataSet.DefaultPageSize,
            QueryDefinitionId = dataSet.QueryDefinitionId,
            QueryDefinitionCode = dataSet.QueryDefinition?.Code,
            PermissionFilterId = dataSet.PermissionFilterId,
            PermissionFilterCode = dataSet.PermissionFilter?.Code,
            IsSystem = dataSet.IsSystem,
            IsEnabled = dataSet.IsEnabled,
            CreatedAt = dataSet.CreatedAt,
            CreatedBy = dataSet.CreatedBy,
            UpdatedAt = dataSet.UpdatedAt,
            UpdatedBy = dataSet.UpdatedBy
        };
    }
}
