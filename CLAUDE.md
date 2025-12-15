# CLAUDE.md - AI Assistant Guide for BobCRM

This document provides comprehensive guidance for AI assistants working with the BobCRM codebase.

## Project Overview

**BobCRM** is a customer relationship management (CRM) system built with .NET 8 and Blazor Server, featuring a dynamic entity definition system that allows runtime creation and management of custom entities without application restarts.

### Key Technologies

- **Backend**: .NET 8 Minimal APIs, ASP.NET Core
- **Frontend**: Blazor Server, Ant Design Blazor
- **Database**: PostgreSQL (with SQLite fallback)
- **ORM**: Entity Framework Core 8
- **Object Storage**: MinIO (S3-compatible)
- **Authentication**: JWT + ASP.NET Identity
- **Code Generation**: Roslyn (Microsoft.CodeAnalysis)
- **Logging**: Serilog
- **Development**: PowerShell 7+ scripts, Docker Compose

### Project Structure

```
bobcrm/
├── src/
│   ├── BobCrm.Api/          # Backend API (port 5200)
│   │   ├── Abstractions/    # Interface definitions
│   │   ├── Application/     # Application layer (queries, commands)
│   │   ├── Contracts/       # DTOs and data contracts
│   │   ├── Controllers/     # MVC controllers
│   │   ├── Core/            # Core business logic and persistence
│   │   ├── Base/            # Base models and aggregates
│   │   ├── Endpoints/       # Minimal API endpoints
│   │   ├── Infrastructure/  # EF configurations, database context
│   │   ├── Middleware/      # Custom middleware
│   │   ├── Migrations/      # EF migrations
│   │   ├── Services/        # Business services
│   │   └── Utils/           # Utility classes
│   │
│   └── BobCrm.App/          # Frontend Blazor Server (port 3000)
│       ├── Components/      # Blazor components
│       │   ├── Designer/    # Form designer components
│       │   ├── Layout/      # Layout components
│       │   ├── Pages/       # Page components
│       │   └── Shared/      # Shared components
│       ├── Models/          # Frontend models
│       ├── Services/        # Frontend services
│       └── wwwroot/         # Static files
│
├── tests/
│   └── BobCrm.Api.Tests/   # API integration tests (41 test files)
│
├── docs/                    # Comprehensive documentation
│   ├── design/             # Architecture and product design
│   ├── guides/             # Developer guides and manuals
│   ├── reference/          # API documentation
│   ├── history/            # Change history and gap reports
│   ├── process/            # Process guidelines (PR checklist, etc.)
│   └── examples/           # Example implementations
│
├── scripts/                # PowerShell automation scripts
├── database/               # Database initialization scripts
└── docker-compose.yml      # Infrastructure setup
```

## Core Architecture Concepts

### 1. Dynamic Entity System

BobCRM's most distinctive feature is its runtime entity definition system:

- **Entity Definitions**: Define custom entities via API without code changes
- **Runtime Code Generation**: Uses Roslyn to generate C# classes on-the-fly
- **Dynamic Compilation**: Compiles and loads entities into memory
- **Database Schema Sync**: Auto-generates and executes DDL scripts
- **Multi-language Support**: All entity/field names use i18n resource keys

**Key Files**:
- `src/BobCrm.Api/Base/Models/EntityDefinition.cs`
- `src/BobCrm.Api/Services/EntityDefinitionAggregateService.cs`
- `src/BobCrm.Api/Services/CodeGeneration/CSharpCodeGenerator.cs`
- `src/BobCrm.Api/Services/DynamicEntityService.cs`
- `src/BobCrm.Api/Services/EntityPublishingService.cs`

**Documentation**: `docs/design/ARCH-11-动态实体指南.md`

### 2. AggVO (Aggregate Value Object) System

Handles master-detail and master-detail-grandchild relationships:

- **Structure Types**: Single, MasterDetail (2-layer), MasterDetailGrandchild (3-layer)
- **Unified Operations**: Save, Load, Delete with transaction consistency
- **Cascade Management**: Automatic handling of sub-entities

**Key Files**:
- `src/BobCrm.Api/Base/Aggregates/`
- `src/BobCrm.Api/Services/Aggregates/`

**Documentation**: `docs/design/ARCH-10-AggVO系统指南.md`

### 3. Entity Interfaces System

Entities can implement standard interfaces to gain built-in functionality:

- **Base**: `Id`, `Code`, `Name`, `Version`
- **Archive**: `IsArchived`, `ArchivedAt`, `ArchivedBy`
- **Audit**: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **Organizational**: `OrganizationId` (integrates with permission system)
- **Locking**: Optimistic concurrency control

**Key Files**:
- `src/BobCrm.Api/Base/Models/EntityInterface.cs`
- `src/BobCrm.Api/Abstractions/` (IBaseEntity, IAuditableEntity, etc.)

### 4. Multi-language (i18n) System

Comprehensive internationalization support:

- **Languages**: Chinese (zh-CN), Japanese (ja-JP), English (en-US)
- **Resource Management**: JSON-based resources stored in database
- **Backend**: `I18nService` for API responses
- **Frontend**: `MultilingualInput` component for editing
- **Entity Metadata**: All entity/field names use resource keys

**Key Files**:
- `src/BobCrm.Api/Services/I18nService.cs`
- `src/BobCrm.Api/Resources/i18n-resources.json`
- `src/BobCrm.App/Components/Shared/MultilingualInput.razor`

**Documentation**:
- `docs/guides/I18N-01-多语机制设计文档.md`
- `docs/guides/I18N-02-元数据多语机制设计文档.md`

### 5. Role-Based Access Control (RBAC)

Hierarchical permission system:

- **Function Nodes**: Tree structure for features (domain → sub-features)
- **Roles**: `RoleProfile` with function permissions and data scopes
- **User Assignment**: Many-to-many user-role relationships
- **Default Role**: `SYS.ADMIN` with full access
- **Permission Filtering**: Automatic API filtering via `FunctionPermissionFilter`

**Key Files**:
- `src/BobCrm.Api/Base/Models/RoleProfile.cs`
- `src/BobCrm.Api/Base/Models/FunctionNode.cs`
- `src/BobCrm.Api/Services/AccessService.cs`
- `src/BobCrm.Api/Endpoints/AccessEndpoints.cs`

**Documentation**: `docs/design/ARCH-21-组织与权限体系设计.md`

### 6. Form Template System

Runtime form layout management:

- **Templates**: Multiple named templates per entity
- **Designers**: Visual form designer for creating layouts
- **Binding**: Template binding to entities with usage types
- **Runtime Loading**: Dynamic template rendering in frontend

**Key Files**:
- `src/BobCrm.Api/Base/Models/FormTemplate.cs`
- `src/BobCrm.Api/Services/FormTemplateService.cs`
- `src/BobCrm.App/Components/Designer/`

### 7. Global Toast Notification System

Unified user feedback mechanism for operations:

- **Toast Service**: Centralized notification service with 4 message types
  - `Success()`: Green background, for successful operations
  - `Error()`: Red background, for failures and errors
  - `Warning()`: Yellow background, for warnings
  - `Info()`: Blue background, for informational messages
- **Auto-hide**: Messages automatically disappear after 3 seconds
- **Queue Management**: Maximum 3 visible messages, oldest removed first
- **Theme Support**: Adapts to light/dark themes
- **Animation**: Slide-down and fade-in effects

**Key Files**:
- `src/BobCrm.App/Services/ToastService.cs` - Toast service implementation
- `src/BobCrm.App/Components/Shared/GlobalToast.razor` - Toast UI component
- `src/BobCrm.App/Components/Shared/GlobalToast.razor.css` - Toast styling

**Usage Example**:
```csharp
@inject ToastService ToastService

private async Task HandleSave()
{
    try
    {
        await EntityDefService.UpdateAsync(Id.Value, request);
        ToastService.Success(I18n.T("MSG_UPDATE_SUCCESS"));
    }
    catch (Exception ex)
    {
        ToastService.Error($"{I18n.T("MSG_SAVE_FAILED")}: {ex.Message}");
    }
}
```

### 8. Compact Top Menu Navigation

Space-efficient navigation system (v1.0):

- **Design Philosophy**: Maximize content area by replacing fixed left sidebar
- **Domain Selector**: Icon-only selector next to user profile
- **Menu Panel**: Collapsible 4-column grid layout (max 600px width)
- **Smart Positioning**: JavaScript-based dynamic alignment to trigger button
- **Auto-close**: Click outside to dismiss (transparent overlay)
- **Key Components**:
  - `MenuButton` - Encapsulates menu button and panel
  - `DomainSelector` - Domain switching interface
  - `MenuPanel` - Function menu display
  - `LayoutState` - Menu state management

**Key Files**:
- `src/BobCrm.App/Components/Layout/MenuButton.razor`
- `src/BobCrm.App/Components/Layout/DomainSelector.razor`
- `src/BobCrm.App/Components/Layout/MenuPanel.razor`
- `src/BobCrm.App/Services/LayoutState.cs`

**Documentation**: `docs/design/ARCH-24-紧凑型顶部菜单导航设计.md`

**Status**: First version released, basic functionality complete. Future enhancements will focus on visual polish and additional features.

### 9. Field Soft Delete Mechanism

Data integrity and traceability for entity field metadata:

- **Soft Delete Fields**:
  - `IsDeleted`: Boolean flag, defaults to `false`
  - `DeletedAt`: Deletion timestamp (nullable)
  - `DeletedBy`: User ID who deleted (nullable)
- **Deletion Rules**:
  - **Custom fields**: Marked as deleted, metadata retained
  - **Interface/System fields**: Must remove interface first or cannot delete
  - Required fields automatically converted to nullable in DDL when soft deleted
- **Query Filtering**: All field queries automatically filter `!IsDeleted`
- **Protection Levels** (Source-based):
  - **System fields**: Only DisplayName and SortOrder can be updated
  - **Interface fields**: DisplayName, SortOrder, and DefaultValue can be updated
  - **Custom fields**: Most properties can be updated

**Key Files**:
- `src/BobCrm.Api/Base/Models/FieldMetadata.cs` - Soft delete fields
- `src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs` - Field CRUD with protection

**Documentation**: `docs/design/ARCH-20-实体定义管理设计.md` (Section 5.3, 6.3)

## Development Workflows

### Environment Setup

```powershell
# 1. Verify environment (checks .NET, Docker, ports, etc.)
pwsh scripts/verify-setup.ps1

# 2. Start infrastructure (PostgreSQL + MinIO)
docker compose up -d

# 3. Start development servers
pwsh scripts/dev.ps1 -Action start

# Frontend: http://localhost:3000
# API: http://localhost:5200
# Swagger: http://localhost:5200/swagger
# MinIO Console: http://localhost:19101

# Default credentials: admin / Admin@12345
```

### Stopping Development Environment

```powershell
# Stop development servers
pwsh scripts/dev.ps1 -Action stop

# Stop infrastructure
docker compose down
```

### Database Management

```bash
# Create new migration
dotnet ef migrations add <MigrationName> --project src/BobCrm.Api

# Apply migrations
dotnet ef database update --project src/BobCrm.Api

# Reset database
docker compose down -v  # Remove volumes
docker compose up -d    # Restart with fresh DB
```

### Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageDirectory=../../coverage-report

# Run specific test
dotnet test --filter "FullyQualifiedName~EntityDefinitionTests"
```

See `docs/guides/TEST-01-测试指南.md` for comprehensive testing guide.

### Code Style Verification

```powershell
# Check for hardcoded style tokens (should use design tokens)
pwsh scripts/check-style-tokens.ps1

# Fail build on violations (use in CI)
pwsh scripts/check-style-tokens.ps1 -FailOnMatch
```

## AI Assistant Conventions

### 1. Language Usage

- **Code Comments**: Primarily Chinese (existing convention)
- **Documentation**: Chinese for design docs, mixed for technical docs
- **Commit Messages**: English or Chinese, be consistent
- **User-facing Text**: Use i18n resource keys, never hardcode

### 2. Naming Conventions

**C# Code**:
- **Classes/Interfaces**: PascalCase (e.g., `EntityDefinition`, `IEntityService`)
- **Methods/Properties**: PascalCase (e.g., `GetEntityById`, `CreatedAt`)
- **Private Fields**: camelCase with underscore (e.g., `_dbContext`)
- **Parameters/Variables**: camelCase (e.g., `entityId`, `userName`)

**Database**:
- **Tables**: PascalCase (e.g., `EntityDefinitions`)
- **Columns**: PascalCase (e.g., `DisplayNameKey`)
- **Constraints**: Use EF conventions

**Frontend**:
- **Components**: PascalCase (e.g., `MultilingualInput.razor`)
- **CSS Classes**: kebab-case (e.g., `menu-panel`, `domain-selector`)
- **JavaScript**: camelCase for functions/variables

**API Endpoints**:
- **Routes**: kebab-case (e.g., `/api/entity-definitions`, `/api/access/functions`)

### 3. Code Organization

**Backend Service Pattern**:
```csharp
// 1. Interface in Abstractions/
public interface IEntityService
{
    Task<EntityDto> GetByIdAsync(int id);
}

// 2. Implementation in Services/
public class EntityService : IEntityService
{
    private readonly AppDbContext _dbContext;

    public EntityService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EntityDto> GetByIdAsync(int id)
    {
        // Implementation
    }
}

// 3. Register in Program.cs
builder.Services.AddScoped<IEntityService, EntityService>();

// 4. Endpoint in Endpoints/
public static class EntityEndpoints
{
    public static void MapEntityEndpoints(this WebApplication app)
    {
        app.MapGet("/api/entities/{id}", async (int id, IEntityService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(result);
        });
    }
}
```

**Frontend Component Pattern**:
```razor
@* Components should be self-contained with clear responsibilities *@
@inject IEntityService EntityService

<div class="entity-list">
    @foreach (var entity in entities)
    {
        <EntityCard Entity="entity" OnUpdate="HandleUpdate" />
    }
</div>

@code {
    private List<EntityDto> entities = new();

    protected override async Task OnInitializedAsync()
    {
        entities = await EntityService.GetAllAsync();
    }

    private async Task HandleUpdate(EntityDto updated)
    {
        // Handle update
    }
}
```

### 4. Error Handling

```csharp
// Use standardized error responses
try
{
    var result = await service.PerformOperationAsync();
    return Results.Ok(result);
}
catch (ValidationException ex)
{
    return Results.BadRequest(new { error = ex.Message });
}
catch (NotFoundException ex)
{
    return Results.NotFound(new { error = ex.Message });
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error in operation");
    return Results.Problem("Internal server error");
}
```

### 5. Database Queries

```csharp
// Always use async operations
// Use Include for eager loading
// Project to DTOs to avoid over-fetching

var entities = await _dbContext.EntityDefinitions
    .Include(e => e.Fields)
    .Include(e => e.Interfaces)
    .Where(e => !e.IsArchived)
    .OrderBy(e => e.DisplayNameKey)
    .Select(e => new EntityDefinitionDto
    {
        Id = e.Id,
        Name = e.EntityName,
        FieldCount = e.Fields.Count
        // Only select what you need
    })
    .ToListAsync();
```

### 6. Multi-language Text

```csharp
// Backend: Use resource keys
public class CreateEntityRequest
{
    public string DisplayNameKey { get; set; } // e.g., "ENTITY_CUSTOMER"
    public string DescriptionKey { get; set; }  // e.g., "ENTITY_CUSTOMER_DESC"
}

// Frontend: Use I18nService
@inject I18nService I18n

<h1>@I18n.T("ENTITY_LIST_TITLE")</h1>
```

### 7. Git Commit Guidelines

When making commits on behalf of user requests:

```bash
# Format: <type>: <description>
# Types: feat, fix, refactor, docs, test, chore

# Good examples:
git commit -m "feat: add dynamic entity validation service"
git commit -m "fix: correct API response structure for entity definitions"
git commit -m "refactor: simplify form template loading logic"
git commit -m "docs: update CLAUDE.md with testing guidelines"
git commit -m "test: add integration tests for role permissions"

# Include issue/PR reference if applicable
git commit -m "fix: resolve entity field source marking (#123)"
```

### 8. Pull Request Requirements

Before creating a PR, ensure compliance with `docs/process/PROC-01-PR检查清单.md`:

1. **Design Reference**: Link to relevant design docs
2. **Theme Screenshots**: Include light/dark theme screenshots
3. **Regression Testing**: Document affected components
4. **Accessibility**: Verify tab order, focus states, contrast ratios
5. **Automated Checks**:
   - Run `pwsh scripts/check-style-tokens.ps1 -FailOnMatch`
   - Ensure all tests pass: `dotnet test`

### 9. Documentation Updates

When modifying features:

1. **Update CHANGELOG.md**: Add entry under `[未发布] - 进行中` section
2. **Update Design Docs**: If architecture changes, update relevant `docs/design/ARCH-*.md`
3. **Update API Docs**: Update `docs/reference/API-01-接口文档.md` if endpoints change
4. **Add Examples**: For new features, consider adding to `docs/examples/`

See `docs/process/PROC-02-文档同步规范.md` for detailed guidelines.

### 10. Common Patterns to Follow

**System Entities** (Customer, OrganizationNode, RoleProfile):
- Always mark fields with `Source = FieldSource.System`
- Cannot be deleted via UI
- Core to application functionality

**Custom Entities**:
- Created via entity definition API
- Fields marked with `Source = FieldSource.Custom`
- Can be archived/deleted by users

**Entity Publishing Flow**:
1. Create/Update EntityDefinition
2. Call `/api/entity-definitions/{id}/publish`
3. System generates C# code
4. Compiles code dynamically
5. Generates and executes DDL
6. Entity ready for use

**AggVO Usage**:
1. Define sub-entities in EntityDefinition
2. System generates appropriate AggVO class
3. Use AggVO for all CRUD operations on master-detail data
4. Never manipulate sub-entities independently

## Critical Files to Understand

### Backend Core

| File | Purpose |
|------|---------|
| `src/BobCrm.Api/Program.cs` | Application startup, DI registration |
| `src/BobCrm.Api/Infrastructure/Ef/AppDbContext.cs` | EF Core DbContext |
| `src/BobCrm.Api/Core/Persistence/IRepository.cs` | Repository pattern |
| `src/BobCrm.Api/Services/EntityPublishingService.cs` | Entity publishing orchestration |
| `src/BobCrm.Api/Services/CodeGeneration/CSharpCodeGenerator.cs` | Roslyn code generation |
| `src/BobCrm.Api/Services/DynamicEntityService.cs` | Dynamic entity CRUD |
| `src/BobCrm.Api/Services/AccessService.cs` | Permission checking |
| `src/BobCrm.Api/Middleware/JwtMiddleware.cs` | JWT authentication |

### Frontend Core

| File | Purpose |
|------|---------|
| `src/BobCrm.App/Program.cs` | Blazor application startup |
| `src/BobCrm.App/Components/Layout/MainLayout.razor` | Application layout |
| `src/BobCrm.App/Components/Layout/MenuButton.razor` | Compact menu navigation |
| `src/BobCrm.App/Components/Pages/EntityDefinitions.razor` | Entity definition management |
| `src/BobCrm.App/Components/Pages/EntityDefinitionEdit.razor` | Entity definition editor |
| `src/BobCrm.App/Components/Designer/FormDesigner.razor` | Form template designer |
| `src/BobCrm.App/Components/Shared/MultilingualInput.razor` | i18n input component |
| `src/BobCrm.App/Components/Shared/GlobalToast.razor` | Toast notification component |
| `src/BobCrm.App/Services/LayoutState.cs` | Layout state management |
| `src/BobCrm.App/Services/ToastService.cs` | Toast notification service |

### Configuration

| File | Purpose |
|------|---------|
| `docker-compose.yml` | PostgreSQL + MinIO setup |
| `src/BobCrm.Api/appsettings.json` | API configuration |
| `src/BobCrm.App/appsettings.json` | Frontend configuration |
| `.gitignore` | Git ignore rules |

## Common Tasks and Solutions

### Adding a New Entity Field Type

1. Update `FieldDataType` enum in `Base/Models/FieldDefinition.cs`
2. Update `CSharpCodeGenerator.MapDataTypeToCSharp()` for C# type mapping
3. Update `DDLGenerator.MapDataTypeToSql()` for SQL type mapping
4. Update frontend field type selector in entity definition editor
5. Add tests in `tests/BobCrm.Api.Tests/`

### Adding a New API Endpoint

1. Create endpoint method in appropriate `Endpoints/*Endpoints.cs` file
2. Register endpoint in `Program.cs` via `Map*Endpoints()` call
3. Add authorization attribute if needed: `[Authorize]` or custom filter
4. Add corresponding DTO in `Contracts/DTOs/`
5. Document in `docs/reference/API-01-接口文档.md`
6. Add integration test in `tests/BobCrm.Api.Tests/`

### Adding a New Blazor Component

1. Create `.razor` file in appropriate `Components/` subdirectory
2. Add code-behind if complex: `@code { }` block or `.razor.cs` file
3. Use dependency injection for services: `@inject IMyService MyService`
4. Follow naming convention: PascalCase for component, parameters, and methods
5. Add i18n support: use `@inject I18nService I18n` and resource keys
6. Test in both light and dark themes

### Adding i18n Resources

1. Add keys to `src/BobCrm.Api/Resources/i18n-resources.json`
2. Provide translations for all three languages: zh-CN, ja-JP, en-US
3. Use descriptive key names: `{CONTEXT}_{ELEMENT}_{DESCRIPTION}`
   - Example: `ENTITY_CUSTOMER_NAME`, `BTN_SAVE`, `MSG_DELETE_CONFIRM`
4. Run SQL script if adding to database: `add-missing-i18n-resources.sql`
5. Reference: `UPDATE-I18N-RESOURCES.md`

### Troubleshooting

**Database Connection Issues**:
```bash
# Check container status
docker ps | grep bobcrm-pg

# Check logs
docker logs bobcrm-pg

# Restart container
docker compose restart postgres
```

**Code Generation Errors**:
- Check `logs/api_*.log` for Roslyn compilation errors
- Verify entity definition JSON structure
- Ensure all referenced interfaces exist

**Frontend Build Issues**:
```bash
# Clear Blazor cache
rm -rf src/BobCrm.App/bin src/BobCrm.App/obj

# Rebuild
dotnet build src/BobCrm.App
```

**Migration Conflicts**:
```bash
# Remove last migration
dotnet ef migrations remove --project src/BobCrm.Api

# Regenerate
dotnet ef migrations add <NewName> --project src/BobCrm.Api
```

## Key Architectural Decisions

### Why Dynamic Entities?

- **Business Agility**: Create new entities without developer involvement
- **Rapid Prototyping**: Test business models quickly
- **SaaS Scalability**: Different tenants can have different entity structures
- **No Deployment**: Changes take effect immediately

### Why AggVO Pattern?

- **Transaction Consistency**: Master-detail saved atomically
- **Business Logic Encapsulation**: Related entities managed together
- **API Simplification**: Single endpoint for complex structures
- **DDD Alignment**: Follows aggregate root pattern

### Why Blazor Server?

- **Rich Interactions**: Full .NET capabilities in browser
- **Code Sharing**: Share models/logic between frontend and backend
- **Real-time Updates**: SignalR built-in
- **Performance**: No JavaScript framework overhead

### Why PostgreSQL?

- **JSONB Support**: Excellent for ExtData fields
- **Full-text Search**: Built-in search capabilities
- **Production Ready**: Mature, reliable, open source
- **EF Core Support**: First-class EF Core integration

## Security Considerations

### Authentication Flow

1. User submits credentials to `/api/auth/login`
2. Backend validates against ASP.NET Identity
3. JWT token generated with user claims
4. Frontend stores token, includes in all API requests
5. Backend validates token on each request

### Authorization

- **Function-based**: Each endpoint can require specific function codes
- **Role-based**: Users assigned to roles with function permissions
- **Data Scopes**: Role-based data filtering (all/organization/self)
- **Organizational**: Entities with `IOrganizational` filter by `OrganizationId`

### Common Vulnerabilities to Avoid

- **SQL Injection**: Always use parameterized queries (EF Core handles this)
- **XSS**: Blazor auto-escapes HTML; be careful with `@((MarkupString)html)`
- **CSRF**: Use `[ValidateAntiForgeryToken]` for state-changing operations
- **Mass Assignment**: Use explicit DTOs, never bind directly to entities
- **Sensitive Data**: Never log passwords, tokens, or PII
- **Injection in Dynamic Code**: Validate entity/field names before code generation

## Performance Best Practices

### Database

- Use `AsNoTracking()` for read-only queries
- Batch operations when possible
- Use indexes for frequently queried columns
- Avoid N+1 queries with `Include()`
- Project to DTOs to reduce data transfer

### Caching

- Cache i18n resources (already implemented)
- Cache entity definitions after publish
- Use `IMemoryCache` for frequently accessed data
- Consider distributed cache for multi-instance deployments

### Blazor

- Use `@key` directive for list items
- Virtualize long lists with `<Virtualize>`
- Debounce user input events
- Minimize StateHasChanged() calls
- Use event callbacks instead of two-way binding when possible

## References

### Essential Documentation

- **Entry Point**: `docs/PROC-00-文档索引.md`
- **API Reference**: `docs/reference/API-01-接口文档.md`
- **PR Checklist**: `docs/process/PROC-01-PR检查清单.md`
- **Testing Guide**: `docs/guides/TEST-01-测试指南.md`
- **Entity Guide**: `docs/guides/GUIDE-11-实体定义与动态实体操作指南.md`

### Design Documents

- Architecture: `docs/design/ARCH-*.md`
- Product: `docs/design/PROD-*.md`
- UI/UX: `docs/design/UI-*.md`

### External Resources

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Ant Design Blazor](https://antblazor.com/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

## Changelog Maintenance

Always update `CHANGELOG.md` when making changes:

```markdown
## [未发布] - 进行中

### Added
- New features go here

### Changed
- Modified features go here

### Fixed
- Bug fixes go here
```

Follow [Keep a Changelog](https://keepachangelog.com/) format.

---

## Quick Start for AI Assistants

1. **Read this entire document** to understand architecture and conventions
2. **Check `CHANGELOG.md`** to see recent changes and project direction
3. **Review relevant design docs** in `docs/design/` for the feature area
4. **Run tests** before making changes: `dotnet test`
5. **Follow naming conventions** and code organization patterns
6. **Update documentation** when modifying features
7. **Test in development environment** before committing
8. **Create meaningful commits** following git guidelines
9. **Ensure PR requirements** are met before finalizing

## Questions or Clarifications?

When uncertain about:

- **Architecture decisions**: Check `docs/design/ARCH-*.md`
- **API contracts**: Check `docs/reference/API-01-接口文档.md`
- **Process requirements**: Check `docs/process/PROC-*.md`
- **Implementation examples**: Check `docs/examples/EX-*.md`
- **Testing approach**: Check `docs/guides/TEST-*.md`

If still unclear, ask the user for clarification rather than making assumptions.

---

**Last Updated**: 2025-11-15
**Version**: 1.1.0
**Maintainer**: BobCRM Development Team
