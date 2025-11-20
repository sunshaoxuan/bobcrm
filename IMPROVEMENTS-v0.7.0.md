# v0.7.0 Template System - Remaining Improvements

**Date**: 2025-11-20
**Status**: Documented for Future Implementation

---

## Executive Summary

This document tracks the remaining architectural and UX improvements identified in code reviews REVIEW-01 and REVIEW-02 for the v0.7.0 template system. These improvements are **non-blocking** for the current release but should be prioritized for v0.7.1 or v0.8.0.

---

## 1. UI/UX Modernization (High Priority)

### 1.1 Replace Native Browser Dialogs

**Current State**: Both `Templates.razor` and `FormDesigner.razor` use native browser dialogs:
- `window.alert()` for success/error messages
- `window.confirm()` for delete confirmations
- `window.prompt()` for template name input

**Target State**: Replace with Ant Design Blazor components for a professional "Premium" feel:
- Use `ModalService` for confirmations
- Use `MessageService` for success/error notifications
- Use `Modal.Show()` with custom forms for text input

**Files to Modify**:
- `src/BobCrm.App/Components/Pages/Templates.razor` (Lines 398, 437, 498)
- `src/BobCrm.App/Components/Pages/FormDesigner.razor` (Lines 618, 624, 664, 701)

**Effort**: ~4 hours
**Impact**: High - significantly improves user experience

**Example Implementation**:
```csharp
// Before
var newName = await JS.InvokeAsync<string>("prompt", I18n.T("TPL_PROMPT_COPY_NAME"), ...);

// After
var modalRef = await ModalService.CreateModalAsync<TemplateNameInputModal>(new ModalOptions
{
    Title = I18n.T("TPL_PROMPT_COPY_NAME"),
    Footer = null
});
var result = await modalRef.OnOk;
```

---

## 2. Architecture Refactoring (Medium Priority)

### 2.1 Extract Business Logic from TemplateEndpoints.cs

**Current State**: `TemplateEndpoints.cs` contains significant business logic directly in endpoint definitions (Lines 28-720), violating Single Responsibility Principle.

**Target State**: Introduce `TemplateService` to encapsulate:
- Template CRUD operations
- Permission checking logic
- Default template resolution
- Template binding management

**Files to Create/Modify**:
- NEW: `src/BobCrm.Api/Services/TemplateService.cs`
- MODIFY: `src/BobCrm.Api/Endpoints/TemplateEndpoints.cs` (inject `TemplateService`)

**Effort**: ~6 hours
**Impact**: Medium - improves testability and maintainability

**Example**:
```csharp
// NEW: Services/TemplateService.cs
public class TemplateService : ITemplateService
{
    public async Task<FormTemplate?> GetEffectiveTemplateAsync(
        string entityType,
        string userId,
        CancellationToken ct = default)
    {
        // Business logic extracted from endpoint
        var userDefault = await _repo.GetUserDefaultAsync(userId, entityType, ct);
        if (userDefault != null) return userDefault;

        var systemDefault = await _repo.GetSystemDefaultAsync(entityType, ct);
        return systemDefault ?? await _repo.GetFirstByEntityTypeAsync(entityType, ct);
    }
}

// MODIFIED: Endpoints/TemplateEndpoints.cs
group.MapGet("/effective/{entityType}", async (
    string entityType,
    ClaimsPrincipal user,
    TemplateService templateService) =>  // Injected service
{
    var uid = user.GetUserId();
    var template = await templateService.GetEffectiveTemplateAsync(entityType, uid);
    return template == null
        ? Results.NotFound(new { error = "Template not found" })
        : Results.Ok(template);
});
```

---

## 3. Container Rendering Refactoring (Low Priority, Optional)

### 3.1 Replace switch Statement with Dynamic Component

**Current State**: `FormDesigner.razor` uses a `switch` statement to render container widgets (Lines 861-904), violating the Open/Closed Principle.

**Target State**: Use `DynamicComponent` for all container types, matching the approach for leaf widgets.

**Implementation Approach**:
1. Add `DesignComponentType` property to `ContainerWidget` base class
2. Create dedicated Design components:
   - `SectionDesign.razor`
   - `CardDesign.razor`
   - `TabContainerDesign.razor`
3. Replace `switch` with:
   ```csharp
   <DynamicComponent Type="@widget.DesignComponentType" Parameters="@GetDesignParams(widget)" />
   ```

**Effort**: ~8 hours
**Impact**: Low - improves extensibility but current code is acceptable

---

## 4. Additional I18n Improvements (Low Priority)

### 4.1 Entity Display Names

**Current State**: `Templates.razor` hardcodes entity display names (Lines 655-663):
```csharp
return entityType switch
{
    "customer" => "客户",
    "product" => "产品",
    ...
};
```

**Target State**: Load entity display names dynamically from `EntityDefinition` metadata or i18n resources.

**Effort**: ~2 hours
**Impact**: Low - edge case for custom entities

---

## 5. Testing Recommendations

### 5.1 Add Integration Tests

Create integration tests for:
- Template CRUD operations
- Default template resolution logic
- Template binding and menu intersection queries

**Test File**: `tests/BobCrm.Api.Tests/TemplateEndpointsTests.cs`

**Effort**: ~4 hours
**Impact**: High - prevents regressions

---

## 6. Prioritization Summary

| Item | Priority | Effort | Impact | Release Target |
|------|----------|--------|--------|----------------|
| Native dialog replacement | **High** | 4h | High | v0.7.1 |
| TemplateService refactoring | Medium | 6h | Medium | v0.7.1 |
| Integration tests | **High** | 4h | High | v0.7.1 |
| Entity display name i18n | Low | 2h | Low | v0.8.0 |
| Container rendering refactor | Low | 8h | Low | v0.8.0 |

**Recommended v0.7.1 Scope**:
1. Replace native browser dialogs (4h)
2. Add integration tests (4h)
3. **Total**: ~8 hours development time

---

## 7. References

- **REVIEW-01**: Initial code review feedback on i18n and hardcoded styles
- **REVIEW-02**: Follow-up review on FormDesigner i18n and architecture
- **CLAUDE.md**: Project development guidelines

---

**Maintained by**: BobCRM Development Team
**Last Updated**: 2025-11-20
