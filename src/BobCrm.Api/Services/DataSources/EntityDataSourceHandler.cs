using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Abstractions;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services.DataSources;

/// <summary>
/// 实体数据源处理器 - 处理基于实体的数据源查询
/// 通过 DynamicEntityService 加载实体数据
/// </summary>
public class EntityDataSourceHandler : IDataSourceHandler
{
    private readonly ILogger<EntityDataSourceHandler> _logger;
    // private readonly IDynamicEntityService _dynamicEntityService; // 后续注入

    public EntityDataSourceHandler(ILogger<EntityDataSourceHandler> logger)
    {
        _logger = logger;
    }

    public string TypeCode => "entity";

    public Task<DataSourceExecutionResult> ExecuteAsync(
        DataSourceExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("执行实体数据源查询: {TypeCode}, Page: {Page}, PageSize: {PageSize}",
            request.TypeCode, request.Page, request.PageSize);

        // 解析配置
        var config = ParseConfig(request.ConfigJson);

        // TODO: 通过 DynamicEntityService 查询实体数据
        // var data = await _dynamicEntityService.QueryAsync(
        //     config.EntityType,
        //     page: request.Page,
        //     pageSize: request.PageSize,
        //     sortField: request.SortField,
        //     sortDirection: request.SortDirection,
        //     runtimeContext: request.RuntimeContext
        // );

        // 模拟数据(待实现)
        var mockData = new
        {
            items = Array.Empty<object>(),
            totalCount = 0
        };

        var dataJson = JsonSerializer.Serialize(mockData);

        return Task.FromResult(new DataSourceExecutionResult
        {
            DataJson = dataJson,
            TotalCount = 0,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = 0,
            AppliedScopes = new[] { "OrganizationScope" }, // TODO: 从权限过滤器获取
            ExecutionTimeMs = 10,
            IsFromCache = false
        });
    }

    public Task<DataSourceValidationResult> ValidateConfigAsync(string configJson)
    {
        try
        {
            var config = ParseConfig(configJson);

            if (string.IsNullOrWhiteSpace(config.EntityType))
            {
                return Task.FromResult(DataSourceValidationResult.Failure("实体类型不能为空"));
            }

            // TODO: 验证实体类型是否存在

            return Task.FromResult(DataSourceValidationResult.Success());
        }
        catch (JsonException ex)
        {
            return Task.FromResult(DataSourceValidationResult.Failure($"配置JSON格式错误: {ex.Message}"));
        }
    }

    public Task<List<DataSourceFieldMetadata>> GetFieldsAsync(
        string configJson,
        CancellationToken cancellationToken = default)
    {
        var config = ParseConfig(configJson);

        // TODO: 从实体定义中获取字段元数据
        // var entityDef = await _entityDefinitionService.GetByTypeAsync(config.EntityType);
        // return entityDef.Fields.Select(f => new DataSourceFieldMetadata
        // {
        //     Name = f.PropertyName,
        //     DataType = f.DataType,
        //     DisplayName = f.DisplayName,
        //     Sortable = true,
        //     Filterable = true
        // }).ToList();

        // 模拟返回(待实现)
        return Task.FromResult(new List<DataSourceFieldMetadata>());
    }

    private EntityDataSourceConfig ParseConfig(string configJson)
    {
        return JsonSerializer.Deserialize<EntityDataSourceConfig>(configJson)
            ?? throw new JsonException("无法解析实体数据源配置");
    }
}
