using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure.Ef;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Core.DomainCommon.Validation;
using BobCrm.Api.Application.Queries;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Domain;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Endpoints;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置日志到文件 - 存储在项目根目录的logs文件夹
var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "logs");
Directory.CreateDirectory(logsDir);
var logFilePath = Path.Combine(logsDir, $"api_{timestamp}.log");

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Infinite)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

// DbContext (SQLite default; PostgreSQL via config)
var dbProvider = builder.Configuration["Db:Provider"] ?? "sqlite";
var conn = builder.Configuration.GetConnectionString("Default") ?? "Data Source=./data/app.db";
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (dbProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
    {
        opt.UseNpgsql(conn, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "public"));
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
    else
    {
        opt.UseSqlite(conn);
    }
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

// Persist DataProtection Keys (session reconnect, stable cookies/tokens after restart)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();

// JWT
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "dev-secret-change-in-prod-1234567890");
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BobCRM API",
        Version = "v1",
        Description = "客户信息管理系统 - 动态实体定义、表单设计、AggVO聚合根管理",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "BobCRM Team",
            Email = "support@bobcrm.com"
        }
    });

    // 包含XML注释文档
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // JWT Bearer认证配置
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "请输入JWT令牌，格式：Bearer {token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // API分组：按标签分组
    options.TagActionsBy(api => new[] { api.GroupName ?? "Default" });
    options.DocInclusionPredicate((name, api) => true);
});

// Memory cache for cross-request caching (localization, etc.)
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
builder.Services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// ILocalization as Singleton with IMemoryCache for cross-request caching
builder.Services.AddSingleton<ILocalization, EfLocalization>();

// Entity Publishing Services (实体自定义与发布)
builder.Services.AddScoped<BobCrm.Api.Services.PostgreSQLDDLGenerator>();
builder.Services.AddScoped<BobCrm.Api.Services.DDLExecutionService>();
builder.Services.AddScoped<BobCrm.Api.Services.IEntityPublishingService, BobCrm.Api.Services.EntityPublishingService>();

// Dynamic Entity Services (代码生成与动态编译)
builder.Services.AddScoped<BobCrm.Api.Services.CSharpCodeGenerator>();
builder.Services.AddScoped<BobCrm.Api.Services.RoslynCompiler>();
builder.Services.AddScoped<BobCrm.Api.Services.DynamicEntityService>();
builder.Services.AddScoped<BobCrm.Api.Services.ReflectionPersistenceService>();

// Advanced Features Services (高级功能：AggVO、数据迁移评估、实体锁定) - 使用接口注册遵循DIP原则
builder.Services.AddScoped<BobCrm.Api.Services.CodeGeneration.IAggVOCodeGenerator, BobCrm.Api.Services.CodeGeneration.AggVOCodeGenerator>();
builder.Services.AddScoped<BobCrm.Api.Services.Aggregates.IAggVOService, BobCrm.Api.Services.Aggregates.AggVOService>();
builder.Services.AddScoped<BobCrm.Api.Services.DataMigration.IDataMigrationEvaluator, BobCrm.Api.Services.DataMigration.DataMigrationEvaluator>();
builder.Services.AddScoped<BobCrm.Api.Services.IEntityLockService, BobCrm.Api.Services.EntityLockService>();

// Map base DbContext to AppDbContext for generic repositories/UoW
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IValidationPipeline, ValidationPipeline>();
builder.Services.AddScoped<IBusinessValidator<UpdateCustomerDto>, UpdateCustomerBusinessValidator>();
builder.Services.AddScoped<ICommonValidator<UpdateCustomerDto>, UpdateCustomerCommonValidator>();
builder.Services.AddScoped<IPersistenceValidator<UpdateCustomerDto>, UpdateCustomerPersistenceValidator>();
builder.Services.AddScoped<ICustomerQueries, CustomerQueries>();
builder.Services.AddScoped<IFieldQueries, FieldQueries>();
builder.Services.AddScoped<ILayoutQueries, LayoutQueries>();

// CORS (dev friendly; tighten in production)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// S3/MinIO file storage
builder.Services.Configure<BobCrm.Api.Services.Storage.S3Options>(builder.Configuration.GetSection("S3"));
builder.Services.AddSingleton<BobCrm.Api.Services.Storage.IFileStorageService, BobCrm.Api.Services.Storage.S3FileStorageService>();

var app = builder.Build();

// 配置详细日志
app.Logger.LogInformation("============================================");
app.Logger.LogInformation("Application starting at {Time}, log file: {LogFile}", DateTime.Now, logFilePath);
app.Logger.LogInformation("============================================");

// 全局异常处理（放在最前面）
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

// Auto-initialize database on startup (dev-friendly, idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseInitializer.InitializeAsync(db);

    // Sync system entities (IBizEntity implementations) to EntityDefinition table
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<EntityDefinitionSynchronizer>>();
    var synchronizer = new EntityDefinitionSynchronizer(db, logger);
    await synchronizer.SyncSystemEntitiesAsync();

    // Seed test data (development only)
    try
    {
        var customerCount = await db.Set<Customer>().IgnoreQueryFilters().CountAsync();
        app.Logger.LogInformation("[TestData] Current customer count: {Count}", customerCount);

        await TestDataSeeder.SeedTestDataAsync(db);

        var newCount = await db.Set<Customer>().IgnoreQueryFilters().CountAsync();
        app.Logger.LogInformation("[TestData] After seeding, customer count: {Count}", newCount);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[TestData] Failed to seed test data");
    }

    // Seed admin user/role and grant access to all customers
    try
    {
        var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await rm.RoleExistsAsync("admin"))
        {
            await rm.CreateAsync(new IdentityRole("admin"));
        }
        var admin = await um.FindByNameAsync("admin");
        if (admin == null)
        {
            admin = new IdentityUser { UserName = "admin", Email = "admin@local", EmailConfirmed = true };
            await um.CreateAsync(admin, "Admin@12345");
            await um.AddToRoleAsync(admin, "admin");
        }
        else
        {
            if (!await um.IsInRoleAsync(admin, "admin")) await um.AddToRoleAsync(admin, "admin");
            if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true; await um.UpdateAsync(admin);
            }
        }
        // Grant admin access to all customers
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<CustomerAccess>>();
        // 禁用查询过滤器，初始化时需要访问所有客户
        var custIds = db.Customers.IgnoreQueryFilters().Select(c => c.Id).ToList();
        foreach (var cid in custIds)
        {
            var exists = repo.Query(a => a.CustomerId == cid && a.UserId == admin.Id).Any();
            if (!exists)
            {
                await repo.AddAsync(new CustomerAccess { CustomerId = cid, UserId = admin.Id, CanEdit = true });
            }
        }
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[Init] Failed to grant admin access to customers");
    }
}

// ========================================
// 端点注册 - 使用模块化扩展方法
// ========================================
app.MapSetupEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapI18nEndpoints();
app.MapCustomerEndpoints();
app.MapLayoutEndpoints();
app.MapTemplateEndpoints();
app.MapEntityDefinitionEndpoints();
app.MapDynamicEntityEndpoints();
app.MapFieldActionEndpoints();
app.MapFileEndpoints();

app.MapControllers();

// 管理和调试端点（仅开发环境）
if (app.Environment.IsDevelopment())
{
    app.MapAdminEndpoints();
}

app.Run();

// Enable WebApplicationFactory<Program> from test project
public partial class Program { }

