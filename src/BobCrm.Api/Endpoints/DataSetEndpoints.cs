using BobCrm.Api.Contracts.DTOs.DataSet;
using BobCrm.Api.Contracts.Requests.DataSet;
using BobCrm.Api.Services;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// Data set endpoints
/// </summary>
public static class DataSetEndpoints
{
    public static IEndpointRouteBuilder MapDataSetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/datasets")
            .WithTags("DataSets")
            .WithOpenApi()
            .RequireAuthorization();

        // Get all data sets
        group.MapGet("/", async ([FromServices] DataSetService service, CancellationToken ct) =>
        {
            var dataSets = await service.GetAllAsync(ct);
            return Results.Ok(new SuccessResponse<List<DataSetDto>>(dataSets));
        })
        .WithName("GetAllDataSets")
        .WithSummary("获取所有数据集")
        .WithDescription("获取系统重定义的所有数据集列表，支持权限过滤")
        .Produces<SuccessResponse<List<DataSetDto>>>(StatusCodes.Status200OK);

        // Get data set by id
        group.MapGet("/{id:int}", async (
            [FromRoute] int id,
            [FromServices] DataSetService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var dataSet = await service.GetByIdAsync(id, ct);
            if (dataSet == null)
            {
                return Results.NotFound(new ErrorResponse(loc.T("ERR_DATASET_NOT_FOUND_BY_ID", lang), "DATASET_NOT_FOUND"));
            }
            return Results.Ok(new SuccessResponse<DataSetDto>(dataSet));
        })
        .WithName("GetDataSetById")
        .WithSummary("获取数据集详情")
        .WithDescription("根据ID获取数据集的详细配置信息的查询定义")
        .Produces<SuccessResponse<DataSetDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // Get data set by code
        group.MapGet("/by-code/{code}", async (
            [FromRoute] string code,
            [FromServices] DataSetService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var dataSet = await service.GetByCodeAsync(code, ct);
            if (dataSet == null)
            {
                return Results.NotFound(new ErrorResponse(loc.T("ERR_DATASET_NOT_FOUND_BY_CODE", lang), "DATASET_NOT_FOUND"));
            }
            return Results.Ok(new SuccessResponse<DataSetDto>(dataSet));
        })
        .WithName("GetDataSetByCode")
        .WithSummary("根据编码获取数据集")
        .Produces<SuccessResponse<DataSetDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // Create data set
        group.MapPost("/", async ([FromBody] CreateDataSetRequest request, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            var dataSet = await service.CreateAsync(request, ct);
            return Results.Created($"/api/datasets/{dataSet.Id}", new SuccessResponse<DataSetDto>(dataSet));
        })
        .WithName("CreateDataSet")
        .WithSummary("创建数据集")
        .Produces<SuccessResponse<DataSetDto>>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Update data set
        group.MapPut("/{id:int}", async (
            [FromRoute] int id,
            [FromBody] UpdateDataSetRequest request,
            [FromServices] DataSetService service,
            CancellationToken ct) =>
        {
            var dataSet = await service.UpdateAsync(id, request, ct);
            return Results.Ok(new SuccessResponse<DataSetDto>(dataSet));
        })
        .WithName("UpdateDataSet")
        .WithSummary("更新数据集")
        .Produces<SuccessResponse<DataSetDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Delete data set
        group.MapDelete("/{id:int}", async ([FromRoute] int id, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.Ok(ApiResponseExtensions.SuccessResponse());
        })
        .WithName("DeleteDataSet")
        .WithSummary("删除数据集")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Execute data set query
        group.MapPost("/{id:int}/execute", async (
            [FromRoute] int id,
            [FromBody] DataSetExecutionRequest request,
            [FromServices] DataSetService service,
            CancellationToken ct) =>
        {
            var result = await service.ExecuteAsync(id, request, ct);
            return Results.Ok(new SuccessResponse<DataSetExecutionResponse>(result));
        })
        .WithName("ExecuteDataSet")
        .WithSummary("执行数据集查询")
        .Produces<SuccessResponse<DataSetExecutionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Get data set field metadata
        group.MapGet("/{id:int}/fields", async ([FromRoute] int id, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            var fields = await service.GetFieldsAsync(id, ct);
            return Results.Ok(new SuccessResponse<List<DataSourceFieldMetadata>>(fields));
        })
        .WithName("GetDataSetFields")
        .WithSummary("获取数据集字段元数据")
        .Produces<SuccessResponse<List<DataSourceFieldMetadata>>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        return app;
    }
}
