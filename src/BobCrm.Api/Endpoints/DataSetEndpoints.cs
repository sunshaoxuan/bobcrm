using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 数据集相关接口
/// </summary>
public static class DataSetEndpoints
{
    public static IEndpointRouteBuilder MapDataSetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/datasets")
            .WithTags("DataSets")
            .WithOpenApi()
            .RequireAuthorization();

        // 获取所有数据集
        group.MapGet("/", async ([FromServices] DataSetService service, CancellationToken ct) =>
        {
            var dataSets = await service.GetAllAsync(ct);
            return Results.Ok(dataSets);
        })
        .WithName("GetAllDataSets")
        .WithSummary("获取所有数据集");

        // 根据ID获取数据集
        group.MapGet("/{id:int}", async ([FromRoute] int id, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            var dataSet = await service.GetByIdAsync(id, ct);
            if (dataSet == null)
            {
                return Results.NotFound(new { message = $"DataSet {id} not found" });
            }
            return Results.Ok(dataSet);
        })
        .WithName("GetDataSetById")
        .WithSummary("根据ID获取数据集");

        // 根据Code获取数据集
        group.MapGet("/by-code/{code}", async ([FromRoute] string code, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            var dataSet = await service.GetByCodeAsync(code, ct);
            if (dataSet == null)
            {
                return Results.NotFound(new { message = $"DataSet '{code}' not found" });
            }
            return Results.Ok(dataSet);
        })
        .WithName("GetDataSetByCode")
        .WithSummary("根据Code获取数据集");

        // 创建数据集
        group.MapPost("/", async ([FromBody] CreateDataSetRequest request, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            try
            {
                var dataSet = await service.CreateAsync(request, ct);
                return Results.Created($"/api/datasets/{dataSet.Id}", dataSet);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("CreateDataSet")
        .WithSummary("创建数据集");

        // 更新数据集
        group.MapPut("/{id:int}", async (
            [FromRoute] int id,
            [FromBody] UpdateDataSetRequest request,
            [FromServices] DataSetService service,
            CancellationToken ct) =>
        {
            try
            {
                var dataSet = await service.UpdateAsync(id, request, ct);
                return Results.Ok(dataSet);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UpdateDataSet")
        .WithSummary("更新数据集");

        // 删除数据集
        group.MapDelete("/{id:int}", async ([FromRoute] int id, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            try
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("DeleteDataSet")
        .WithSummary("删除数据集");

        // 执行数据集查询
        group.MapPost("/{id:int}/execute", async (
            [FromRoute] int id,
            [FromBody] DataSetExecutionRequest request,
            [FromServices] DataSetService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.ExecuteAsync(id, request, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("ExecuteDataSet")
        .WithSummary("执行数据集查询");

        // 获取数据集字段元数据
        group.MapGet("/{id:int}/fields", async ([FromRoute] int id, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            try
            {
                var fields = await service.GetFieldsAsync(id, ct);
                return Results.Ok(fields);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("GetDataSetFields")
        .WithSummary("获取数据集字段元数据");

        return app;
    }
}
