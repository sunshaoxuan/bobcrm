using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Core.DomainCommon.Validation;
using BobCrm.Api.Application.Queries;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 客户管理相关端点
/// </summary>
public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("客户管理")
            .WithOpenApi()
            .RequireAuthorization();

        // 获取客户列表
        group.MapGet("", (ICustomerQueries q) =>
            Results.Json(q.GetList())
        )
        .WithName("GetCustomers")
        .WithSummary("获取客户列表")
        .WithDescription("获取当前用户可访问的所有客户列表");

        // 获取客户详情
        group.MapGet("/{id:int}", (int id, ICustomerQueries q) =>
        {
            var detail = q.GetDetail(id);
            return detail is null ? Results.NotFound() : Results.Json(detail);
        })
        .WithName("GetCustomerDetail")
        .WithSummary("获取客户详情")
        .WithDescription("获取指定客户的详细信息，包括字段值");

        // 创建新客户
        group.MapPost("", async (
            CreateCustomerDto dto,
            IRepository<Customer> repoCustomer,
            IRepository<CustomerAccess> repoAccess,
            IUnitOfWork uow,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            logger.LogInformation("[Customer] Creating new customer: code={Code}, name={Name}, userId={UserId}", 
                dto.Code, dto.Name, uid);

            // 验证必填字段
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                logger.LogWarning("[Customer] Validation failed: code is required");
                return ApiErrors.Validation(loc.T("ERR_CUSTOMER_CODE_REQUIRED", lang));
            }
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                logger.LogWarning("[Customer] Validation failed: name is required");
                return ApiErrors.Validation(loc.T("ERR_CUSTOMER_NAME_REQUIRED", lang));
            }

            // 检查编码是否已存在
            var exists = repoCustomer.Query(c => c.Code == dto.Code).Any();
            if (exists)
            {
                logger.LogWarning("[Customer] Validation failed: code already exists, code={Code}", dto.Code);
                return ApiErrors.Validation(loc.T("ERR_CUSTOMER_CODE_EXISTS", lang));
            }

            // 创建新客户
            var customer = new Customer
            {
                Code = dto.Code,
                Name = dto.Name,
                Version = 1,
                ExtData = "{}"
            };

            await repoCustomer.AddAsync(customer);
            await uow.SaveChangesAsync();

            // 授予创建者访问权限
            if (!string.IsNullOrEmpty(uid))
            {
                await repoAccess.AddAsync(new CustomerAccess
                {
                    CustomerId = customer.Id,
                    UserId = uid,
                    CanEdit = true
                });
                await uow.SaveChangesAsync();
            }

            logger.LogInformation("[Customer] Customer created successfully: id={Id}, code={Code}", customer.Id, customer.Code);
            return Results.Json(new { id = customer.Id, code = customer.Code, name = customer.Name });
        })
        .WithName("CreateCustomer")
        .WithSummary("创建客户")
        .WithDescription("创建新客户并自动授予创建者编辑权限");

        // 更新客户
        group.MapPut("/{id:int}", async (
            int id,
            UpdateCustomerDto dto,
            IRepository<Customer> repoCustomer,
            IRepository<FieldDefinition> repoDef,
            IRepository<FieldValue> repoVal,
            IRepository<CustomerAccess> repoAccess,
            IUnitOfWork uow,
            IValidationPipeline pipe,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            logger.LogInformation("[Customer] Updating customer: id={Id}, userId={UserId}", id, uid);

            var vr = await pipe.ValidateAsync(dto, http);
            if (vr is not null)
            {
                logger.LogWarning("[Customer] Validation failed for update: id={Id}", id);
                return vr;
            }

            var anyAccessDefined = repoAccess.Query(a => a.CustomerId == id).Any();
            if (anyAccessDefined)
            {
                var canEdit = repoAccess.Query(a => a.CustomerId == id && a.UserId == uid && a.CanEdit).Any();
                if (!canEdit)
                {
                    logger.LogWarning("[Customer] Access denied: user {UserId} cannot edit customer {Id}", uid, id);
                    return Results.StatusCode(403);
                }
            }

            var c = repoCustomer.Query(x => x.Id == id).FirstOrDefault();
            if (c == null)
            {
                logger.LogWarning("[Customer] Customer not found: id={Id}", id);
                return Results.NotFound();
            }

            // 乐观并发控制
            if (dto.ExpectedVersion.HasValue && dto.ExpectedVersion.Value != c.Version)
            {
                var lang = LangHelper.GetLang(http);
                logger.LogWarning("[Customer] Concurrency conflict: expected version {ExpectedVersion}, actual version {ActualVersion}", 
                    dto.ExpectedVersion.Value, c.Version);
                return ApiErrors.Concurrency(loc.T("ERR_CONCURRENCY", lang));
            }

            // 可选更新：Code 和 Name
            if (!string.IsNullOrWhiteSpace(dto.Code) && !string.Equals(dto.Code, c.Code, StringComparison.Ordinal))
            {
                // 确保唯一编码
                var dup = repoCustomer.Query(x => x.Code == dto.Code && x.Id != id).Any();
                if (dup)
                {
                    var lang = LangHelper.GetLang(http);
                    logger.LogWarning("[Customer] Code already exists: code={Code}", dto.Code);
                    return ApiErrors.Business(loc.T("ERR_CODE_EXISTS", lang));
                }
                c.Code = dto.Code!.Trim();
            }
            if (!string.IsNullOrWhiteSpace(dto.Name) && !string.Equals(dto.Name, c.Name, StringComparison.Ordinal))
            {
                c.Name = dto.Name!.Trim();
            }

            var defs = repoDef.Query().ToDictionary(d => d.Key, d => d);
            if (dto.Fields != null && dto.Fields.Count > 0)
            {
                foreach (var f in dto.Fields)
                {
                    if (string.IsNullOrWhiteSpace(f.Key))
                    {
                        var lang = LangHelper.GetLang(http);
                        logger.LogWarning("[Customer] Field key is required");
                        return ApiErrors.Validation(loc.T("ERR_FIELD_KEY_REQUIRED", lang));
                    }
                    if (!defs.TryGetValue(f.Key, out var def))
                    {
                        var lang = LangHelper.GetLang(http);
                        logger.LogWarning("[Customer] Unknown field: key={Key}", f.Key);
                        return ApiErrors.Business($"{loc.T("ERR_UNKNOWN_FIELD", lang)}: {f.Key}");
                    }

                    var json = System.Text.Json.JsonSerializer.Serialize(f.Value);
                    var val = new FieldValue { CustomerId = id, FieldDefinitionId = def.Id, Value = json, Version = c.Version + 1 };
                    await repoVal.AddAsync(val);
                }
            }

            // 版本号递增
            c.Version += 1;
            repoCustomer.Update(c);
            await uow.SaveChangesAsync();

            logger.LogInformation("[Customer] Customer updated successfully: id={Id}, newVersion={Version}", id, c.Version);
            return Results.Json(new { status = "success", newVersion = c.Version });
        })
        .WithName("UpdateCustomer")
        .WithSummary("更新客户")
        .WithDescription("更新客户信息和自定义字段值，支持乐观并发控制");

        // 客户访问权限管理（仅管理员）
        group.MapGet("/{id:int}/access", async (
            int id, 
            AppDbContext db, 
            ClaimsPrincipal user,
            ILogger<Program> logger) =>
        {
            var name = user.Identity?.Name ?? string.Empty;
            var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            
            if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("[Customer] Access denied: user {Name} tried to view access list for customer {Id}", name, id);
                return Results.StatusCode(403);
            }

            var list = await db.CustomerAccesses.AsNoTracking()
                .Where(a => a.CustomerId == id)
                .Select(a => new { a.UserId, a.CanEdit })
                .ToListAsync();
                
            logger.LogInformation("[Customer] Retrieved access list for customer {Id}: {Count} entries", id, list.Count);
            return Results.Json(list);
        })
        .WithName("GetCustomerAccess")
        .WithSummary("获取客户访问权限")
        .WithDescription("管理员查看指定客户的访问权限列表");

        group.MapPost("/{id:int}/access", async (
            int id,
            AppDbContext db,
            ClaimsPrincipal user,
            AccessUpsertDto body,
            ILogger<Program> logger) =>
        {
            var name = user.Identity?.Name ?? string.Empty;
            var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            
            if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("[Customer] Access denied: user {Name} tried to modify access for customer {Id}", name, id);
                return Results.StatusCode(403);
            }

            var entity = await db.CustomerAccesses.FirstOrDefaultAsync(a => a.CustomerId == id && a.UserId == body.UserId);
            if (entity == null)
            {
                db.CustomerAccesses.Add(new CustomerAccess { CustomerId = id, UserId = body.UserId, CanEdit = body.CanEdit });
                logger.LogInformation("[Customer] Access granted: customerId={CustomerId}, userId={UserId}, canEdit={CanEdit}", 
                    id, body.UserId, body.CanEdit);
            }
            else
            {
                entity.CanEdit = body.CanEdit;
                db.CustomerAccesses.Update(entity);
                logger.LogInformation("[Customer] Access updated: customerId={CustomerId}, userId={UserId}, canEdit={CanEdit}", 
                    id, body.UserId, body.CanEdit);
            }
            await db.SaveChangesAsync();
            return Results.Ok(ApiResponseExtensions.SuccessResponse("权限设置成功"));
        })
        .WithName("UpsertCustomerAccess")
        .WithSummary("设置客户访问权限")
        .WithDescription("管理员设置或更新用户对指定客户的访问权限");

        return app;
    }
}
