# Development Prompts for v0.9.5 Release & v1.0 Preparation

Based on the completion of v0.9.5 and `PLAN-15`, here are the recommended prompts for the next steps.

## Prompt 1: Release v0.9.5

**Context**: All tasks for v0.9.5 (System Governance, Advanced Settings, Business Core) are verified complete.

**Objective**: Finalize the version release.

**Prompt**:
```markdown
# Context
We have verified and completed all tasks for v0.9.5 (TASK-01 to TASK-06).

# Objective
Finalize the v0.9.5 release.

# Tasks
1.  **Version Bump**:
    -   Update `<Version>` in all `.csproj` files (`BobCrm.Api`, `BobCrm.App`, etc.) to `0.9.5`.
2.  **Documentation**:
    -   Update `CHANGELOG.md` to record the key features delivered in v0.9.5:
        -   **System Governance**: Audit Log UI, Background Job Monitor.
        -   **Advanced Settings**: I18n Resource Editor, SMTP & Notification Settings.
        -   **Business Core**: Customer 360 View (Aggregated Tabs), Lookup UI Enhancement (Friendly Names).
    -   Create a release record `docs/history/RELEASE-NOTE-v0.9.5.md` summarizing these changes.

# Verification
-   Confirm the application builds with the new version number.
-   Verify `CHANGELOG.md` is updated.
```

---

## Prompt 2: API Contract Governance (PLAN-15)

**Context**: Prerequisite for v1.0.0 stability. Standardizing API responses.

**Objective**: Execute Batch 1 & 2 of PLAN-15.

**Prompt**:
```markdown
# Context
We are preparing for v1.0.0. Following `PLAN-15`, we need to standardize all API responses to ensure strict OpenAPI compliance and strong typing.

# Objective
Implement `PLAN-15: API Response Contract Governance` (Batch 1 & 2).

# Tasks
1.  **Infrastructure (Batch 1)**:
    -   Clean up `src/BobCrm.Api/Contracts/DTOs/ApiResponse/` (Remove redundant records).
    -   Refactor `ApiResponseExtensions.cs` to return standard `SuccessResponse<T>` instead of raw objects or mixed types.
2.  **DTO Standardization (Batch 2)**:
    -   Create standard DTOs in `src/BobCrm.Api/Contracts/Responses/` for loose anonymous returns:
        -   `EntityRouteValidationResponse`
        -   `EntityReferenceCheckResponse`
        -   `EntityCountResponse`
        -   `UserRolesUpdateResponse`

# Constraints
-   Strictly follow `docs/plans/PLAN-15-API响应契约治理实施方案.md`.
-   Run `dotnet build` frequently to catch breaking changes.
```
