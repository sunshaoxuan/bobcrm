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
using BobCrm.Api.Services.Settings;
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
builder.Services.AddScoped<SettingsService>();

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
var skipDbInit = builder.Configuration.GetValue<bool>("Db:SkipInit");
if (!skipDbInit)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseInitializer.InitializeAsync(db);

    // Upgrade login i18n resources to latest copy (idempotent)
    try
    {
        void Upsert(string key, string zh, string ja, string en)
        {
            var set = db.Set<LocalizationResource>();
            var r = set.FirstOrDefault(x => x.Key == key);
            if (r is null) set.Add(new LocalizationResource { Key = key, ZH = zh, JA = ja, EN = en });
            else { r.ZH = zh; r.JA = ja; r.EN = en; }
        }

        // Short hero copy to avoid wrapping in JA/EN
        Upsert("TXT_AUTH_HERO_TITLE", "智能连接 · 体验合一", "インテリジェントにつながり、体験をひとつに", "Smart links, unified experience");
        Upsert("TXT_AUTH_HERO_SUBTITLE", "在一个平台洞察、协作、成长，让客户关系更高效。", "ひとつのプラットフォームで洞察・協働・成長を実現し、顧客関係をしなやかに。", "One platform for insight, collaboration, and growth.");
        Upsert("TXT_AUTH_HERO_POINT1", "统一视图 — 打通客户、项目与数据的全局视角", "統一ビュー — 顧客・プロジェクト・データを横断する全体視点", "Unified view — A global perspective across customers, projects, and data");
        Upsert("TXT_AUTH_HERO_POINT2", "智能协作 — 实时共享信息，让决策更快一步", "スマートな協働 — 情報を即時共有し、意思決定を一歩先へ", "Intelligent collaboration — Share in real time and decide faster");
        Upsert("TXT_AUTH_HERO_POINT3", "体验一致 — 无论何处登录，体验始终如一", "一貫した体験 — どこからログインしても変わらない体験", "Consistent experience — The same experience wherever you sign in");
        Upsert("TXT_AUTH_HERO_POINT4", "多语言支持 — 为全球团队打造无边界协作空间", "多言語対応 — グローバルチームのための境界のない協働空間", "Multilingual support — A boundaryless workspace for global teams");
        Upsert("TXT_AUTH_TAGLINE", "让关系更智能，让协作更自然。", "関係をもっとスマートに、協働をもっと自然に。", "Make relationships smarter, collaboration more natural.");
        Upsert("LBL_SECURE", "智能 · 稳定 · 开放", "スマート・堅牢・オープン", "Smart · Resilient · Open");
        Upsert("LBL_WELCOME_BACK", "欢迎回来", "おかえりなさい", "Welcome back");
        await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "i18n login hero upgrade skipped due to error");
    }

    // Sync system entities (IBizEntity implementations) to EntityDefinition table
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<EntityDefinitionSynchronizer>>();
    var synchronizer = new EntityDefinitionSynchronizer(db, logger);
    await synchronizer.SyncSystemEntitiesAsync();

    // Sync internationalization resources (add missing i18n keys to database)
    try
    {
        var i18nLogger = scope.ServiceProvider.GetRequiredService<ILogger<I18nResourceSynchronizer>>();
        var i18nSynchronizer = new I18nResourceSynchronizer(db, i18nLogger);
        await i18nSynchronizer.SyncResourcesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "[Init] Failed to sync i18n resources");
    }

    // Seed test data (development only)
        try
        {
            await TestDataSeeder.SeedTestDataAsync(db);
        }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[Init] Failed to seed test data");
    }
}
// ========================================
// 端点注册 - 使用模块化扩展方法
// ========================================
app.MapSetupEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapSettingsEndpoints();
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

