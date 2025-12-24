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
        .WithSummary("Get all data sets")
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
        .WithSummary("Get data set by id")
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
        .WithSummary("Get data set by code")
        .Produces<SuccessResponse<DataSetDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // Create data set
        group.MapPost("/", async ([FromBody] CreateDataSetRequest request, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            try
            {
                var dataSet = await service.CreateAsync(request, ct);
                return Results.Created($"/api/datasets/{dataSet.Id}", new SuccessResponse<DataSetDto>(dataSet));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message, "INVALID_OPERATION"));
            }
        })
        .WithName("CreateDataSet")
        .WithSummary("Create data set")
        .Produces<SuccessResponse<DataSetDto>>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Update data set
        group.MapPut("/{id:int}", async (
            [FromRoute] int id,
            [FromBody] UpdateDataSetRequest request,
            [FromServices] DataSetService service,
            CancellationToken ct) =>
        {
            try
            {
                var dataSet = await service.UpdateAsync(id, request, ct);
                return Results.Ok(new SuccessResponse<DataSetDto>(dataSet));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message, "INVALID_OPERATION"));
            }
        })
        .WithName("UpdateDataSet")
        .WithSummary("Update data set")
        .Produces<SuccessResponse<DataSetDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Delete data set
        group.MapDelete("/{id:int}", async ([FromRoute] int id, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            try
            {
                await service.DeleteAsync(id, ct);
                return Results.Ok(ApiResponseExtensions.SuccessResponse());
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message, "INVALID_OPERATION"));
            }
        })
        .WithName("DeleteDataSet")
        .WithSummary("Delete data set")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Execute data set query
        group.MapPost("/{id:int}/execute", async (
            [FromRoute] int id,
            [FromBody] DataSetExecutionRequest request,
            [FromServices] DataSetService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.ExecuteAsync(id, request, ct);
                return Results.Ok(new SuccessResponse<DataSetExecutionResponse>(result));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message, "INVALID_OPERATION"));
            }
        })
        .WithName("ExecuteDataSet")
        .WithSummary("Execute data set query")
        .Produces<SuccessResponse<DataSetExecutionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Get data set field metadata
        group.MapGet("/{id:int}/fields", async ([FromRoute] int id, [FromServices] DataSetService service, CancellationToken ct) =>
        {
            try
            {
                var fields = await service.GetFieldsAsync(id, ct);
                return Results.Ok(new SuccessResponse<List<DataSourceFieldMetadata>>(fields));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message, "INVALID_OPERATION"));
            }
        })
        .WithName("GetDataSetFields")
        .WithSummary("Get data set field metadata")
        .Produces<SuccessResponse<List<DataSourceFieldMetadata>>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        return app;
    }
}
