using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;
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
            return Results.Ok(dataSets);
        })
        .WithName("GetAllDataSets")
        .WithSummary("Get all data sets");

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
            return Results.Ok(dataSet);
        })
        .WithName("GetDataSetById")
        .WithSummary("Get data set by id");

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
            return Results.Ok(dataSet);
        })
        .WithName("GetDataSetByCode")
        .WithSummary("Get data set by code");

        // Create data set
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
        .WithSummary("Create data set");

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
                return Results.Ok(dataSet);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UpdateDataSet")
        .WithSummary("Update data set");

        // Delete data set
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
        .WithSummary("Delete data set");

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
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("ExecuteDataSet")
        .WithSummary("Execute data set query");

        // Get data set field metadata
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
        .WithSummary("Get data set field metadata");

        return app;
    }
}
