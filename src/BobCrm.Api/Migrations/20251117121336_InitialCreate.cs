using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Target = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CanEdit = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ExtData = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataSourceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    HandlerType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ConfigSchema = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "General"),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullTypeName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityRoute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    ApiEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StructureType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    ParentEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentEntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ParentForeignKeyField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ParentCollectionProperty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CascadeDeleteBehavior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AutoCascadeSave = table.Column<bool>(type: "boolean", nullable: false),
                    IsRootEntity = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "jsonb", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityDomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldDataTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ClrType = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldDataTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DataType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Actions = table.Column<string>(type: "text", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Validation = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DefaultValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    IsUserDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false),
                    LayoutJson = table.Column<string>(type: "text", nullable: true),
                    UsageType = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: true),
                    RequiredFunctionCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsInUse = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocalizationLanguages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    NativeName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalizationLanguages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocalizationResources",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Translations = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalizationResources", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PathCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationNodes_OrganizationNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OrganizationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissionFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    RequiredFunctionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DataScopeTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FilterRulesJson = table.Column<string>(type: "text", nullable: true),
                    EnableFieldLevelPermissions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FieldPermissionsJson = table.Column<string>(type: "text", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionFilters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueryDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    ConditionsJson = table.Column<string>(type: "text", nullable: true),
                    ParametersJson = table.Column<string>(type: "text", nullable: true),
                    AggregationsJson = table.Column<string>(type: "text", nullable: true),
                    GroupByFields = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DefaultTheme = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DefaultPrimaryColor = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    DefaultLanguage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DefaultHomeRoute = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefaultNavMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "icon-text"),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AllowSelfRegistration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    LayoutJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Theme = table.Column<string>(type: "text", nullable: true),
                    PrimaryColor = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    HomeRoute = table.Column<string>(type: "text", nullable: true),
                    NavDisplayMode = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerLocalizations",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLocalizations", x => new { x.CustomerId, x.Language });
                    table.ForeignKey(
                        name: "FK_CustomerLocalizations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DDLScripts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SqlScript = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DDLScripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DDLScripts_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityInterfaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterfaceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityInterfaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityInterfaces_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubEntityDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DefaultSortField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDescending = table.Column<bool>(type: "boolean", nullable: false),
                    ForeignKeyField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CollectionPropertyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CascadeDeleteBehavior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubEntityDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubEntityDefinitions_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateBindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UsageType = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredFunctionCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateBindings_FormTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    DataSourceTypeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfigJson = table.Column<string>(type: "text", nullable: true),
                    FieldsJson = table.Column<string>(type: "text", nullable: true),
                    SupportsPaging = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SupportsSorting = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DefaultSortField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultSortDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "asc"),
                    DefaultPageSize = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    QueryDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    PermissionFilterId = table.Column<int>(type: "integer", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSets_PermissionFilters_PermissionFilterId",
                        column: x => x.PermissionFilterId,
                        principalTable: "PermissionFilters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DataSets_QueryDefinitions_QueryDefinitionId",
                        column: x => x.QueryDefinitionId,
                        principalTable: "QueryDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAssignments_RoleProfiles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleDataScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FilterExpression = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleDataScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleDataScopes_RoleProfiles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FieldMetadatas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentFieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubEntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: true),
                    Precision = table.Column<int>(type: "integer", nullable: true),
                    Scale = table.Column<int>(type: "integer", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsEntityRef = table.Column<bool>(type: "boolean", nullable: false),
                    ReferencedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    TableName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidationRules = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMetadatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldMetadatas_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldMetadatas_EntityDefinitions_ReferencedEntityId",
                        column: x => x.ReferencedEntityId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldMetadatas_FieldMetadatas_ParentFieldId",
                        column: x => x.ParentFieldId,
                        principalTable: "FieldMetadatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldMetadatas_SubEntityDefinitions_SubEntityDefinitionId",
                        column: x => x.SubEntityDefinitionId,
                        principalTable: "SubEntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FunctionNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    DisplayNameKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Route = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsMenu = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: true),
                    TemplateBindingId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FunctionNodes_FormTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FunctionNodes_FunctionNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FunctionNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FunctionNodes_TemplateBindings_TemplateBindingId",
                        column: x => x.TemplateBindingId,
                        principalTable: "TemplateBindings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoleFunctionPermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FunctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateBindingId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleFunctionPermissions", x => new { x.RoleId, x.FunctionId });
                    table.ForeignKey(
                        name: "FK_RoleFunctionPermissions_FunctionNodes_FunctionId",
                        column: x => x.FunctionId,
                        principalTable: "FunctionNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleFunctionPermissions_RoleProfiles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleFunctionPermissions_TemplateBindings_TemplateBindingId",
                        column: x => x.TemplateBindingId,
                        principalTable: "TemplateBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category_Action",
                table: "AuditLogs",
                columns: new[] { "Category", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccesses_UserId_CustomerId",
                table: "CustomerAccesses",
                columns: new[] { "UserId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_Code",
                table: "DataSets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_DataSourceTypeCode",
                table: "DataSets",
                column: "DataSourceTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_IsEnabled",
                table: "DataSets",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_IsSystem",
                table: "DataSets",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_PermissionFilterId",
                table: "DataSets",
                column: "PermissionFilterId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_QueryDefinitionId",
                table: "DataSets",
                column: "QueryDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_Category",
                table: "DataSourceTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_Code",
                table: "DataSourceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_IsEnabled",
                table: "DataSourceTypes",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_IsSystem",
                table: "DataSourceTypes",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_SortOrder",
                table: "DataSourceTypes",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_DDLScripts_EntityDefinitionId",
                table: "DDLScripts",
                column: "EntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DDLScripts_Status",
                table: "DDLScripts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_IsLocked",
                table: "EntityDefinitions",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_Namespace_EntityName",
                table: "EntityDefinitions",
                columns: new[] { "Namespace", "EntityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_Status",
                table: "EntityDefinitions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EntityDomains_Code",
                table: "EntityDomains",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityDomains_SortOrder",
                table: "EntityDomains",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_EntityInterfaces_EntityDefinitionId_InterfaceType",
                table: "EntityInterfaces",
                columns: new[] { "EntityDefinitionId", "InterfaceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldDataTypes_Code",
                table: "FieldDataTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldDefinitions_Key",
                table: "FieldDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_EntityDefinitionId",
                table: "FieldMetadatas",
                column: "EntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_EntityDefinitionId_PropertyName",
                table: "FieldMetadatas",
                columns: new[] { "EntityDefinitionId", "PropertyName" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_ParentFieldId",
                table: "FieldMetadatas",
                column: "ParentFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_ReferencedEntityId",
                table: "FieldMetadatas",
                column: "ReferencedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_SubEntityDefinitionId",
                table: "FieldMetadatas",
                column: "SubEntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldSources_Code",
                table: "FieldSources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_CustomerId_FieldDefinitionId",
                table: "FieldValues",
                columns: new[] { "CustomerId", "FieldDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_EntityType_IsSystemDefault",
                table: "FormTemplates",
                columns: new[] { "EntityType", "IsSystemDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_UserId_EntityType",
                table: "FormTemplates",
                columns: new[] { "UserId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_UserId_EntityType_IsUserDefault",
                table: "FormTemplates",
                columns: new[] { "UserId", "EntityType", "IsUserDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_Code",
                table: "FunctionNodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_ParentId",
                table: "FunctionNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_TemplateBindingId",
                table: "FunctionNodes",
                column: "TemplateBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_TemplateId",
                table: "FunctionNodes",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationLanguages_Code",
                table: "LocalizationLanguages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationResources_Key",
                table: "LocalizationResources",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationNodes_ParentId_Code",
                table: "OrganizationNodes",
                columns: new[] { "ParentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationNodes_PathCode",
                table: "OrganizationNodes",
                column: "PathCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_Code",
                table: "PermissionFilters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_DataScopeTag",
                table: "PermissionFilters",
                column: "DataScopeTag");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_EntityType",
                table: "PermissionFilters",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_IsEnabled",
                table: "PermissionFilters",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_IsSystem",
                table: "PermissionFilters",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_RequiredFunctionCode",
                table: "PermissionFilters",
                column: "RequiredFunctionCode");

            migrationBuilder.CreateIndex(
                name: "IX_QueryDefinitions_Code",
                table: "QueryDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueryDefinitions_IsEnabled",
                table: "QueryDefinitions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_RoleId",
                table: "RoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_UserId_RoleId_OrganizationId",
                table: "RoleAssignments",
                columns: new[] { "UserId", "RoleId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleDataScopes_RoleId",
                table: "RoleDataScopes",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleFunctionPermissions_FunctionId",
                table: "RoleFunctionPermissions",
                column: "FunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleFunctionPermissions_TemplateBindingId",
                table: "RoleFunctionPermissions",
                column: "TemplateBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleProfiles_Code_OrganizationId",
                table: "RoleProfiles",
                columns: new[] { "Code", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubEntityDefinitions_EntityDefinitionId",
                table: "SubEntityDefinitions",
                column: "EntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubEntityDefinitions_EntityDefinitionId_Code",
                table: "SubEntityDefinitions",
                columns: new[] { "EntityDefinitionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubEntityDefinitions_SortOrder",
                table: "SubEntityDefinitions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBindings_EntityType_UsageType_IsSystem",
                table: "TemplateBindings",
                columns: new[] { "EntityType", "UsageType", "IsSystem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBindings_TemplateId",
                table: "TemplateBindings",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLayouts_UserId_EntityType",
                table: "UserLayouts",
                columns: new[] { "UserId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CustomerAccesses");

            migrationBuilder.DropTable(
                name: "CustomerLocalizations");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "DataSets");

            migrationBuilder.DropTable(
                name: "DataSourceTypes");

            migrationBuilder.DropTable(
                name: "DDLScripts");

            migrationBuilder.DropTable(
                name: "EntityDomains");

            migrationBuilder.DropTable(
                name: "EntityInterfaces");

            migrationBuilder.DropTable(
                name: "FieldDataTypes");

            migrationBuilder.DropTable(
                name: "FieldDefinitions");

            migrationBuilder.DropTable(
                name: "FieldMetadatas");

            migrationBuilder.DropTable(
                name: "FieldSources");

            migrationBuilder.DropTable(
                name: "FieldValues");

            migrationBuilder.DropTable(
                name: "LocalizationLanguages");

            migrationBuilder.DropTable(
                name: "LocalizationResources");

            migrationBuilder.DropTable(
                name: "OrganizationNodes");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoleAssignments");

            migrationBuilder.DropTable(
                name: "RoleDataScopes");

            migrationBuilder.DropTable(
                name: "RoleFunctionPermissions");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "UserLayouts");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "PermissionFilters");

            migrationBuilder.DropTable(
                name: "QueryDefinitions");

            migrationBuilder.DropTable(
                name: "SubEntityDefinitions");

            migrationBuilder.DropTable(
                name: "FunctionNodes");

            migrationBuilder.DropTable(
                name: "RoleProfiles");

            migrationBuilder.DropTable(
                name: "EntityDefinitions");

            migrationBuilder.DropTable(
                name: "TemplateBindings");

            migrationBuilder.DropTable(
                name: "FormTemplates");
        }
    }
}
