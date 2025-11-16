# Menu Workflow Integration Coverage

This document explains how the publish → menu generation → role assignment → runtime template flow is covered by automated tests and CI.

## Automated Scenario
- `tests/BobCrm.Api.Tests/MenuTemplateWorkflowTests.cs` provisions an in-memory application host via `MenuWorkflowAppFactory`.
- The test seeds a draft entity, publishes it, captures the generated menu nodes and template bindings, assigns a role, and validates the runtime `/api/templates/menu-bindings` output for a non-admin user.
- Multilingual menu titles, template binding persistence, and role-based authorization are asserted end-to-end.

## Supporting Infrastructure
- `AccessService.EnsureEntityMenuAsync` attaches template bindings to the generated menu nodes so the runtime template API can surface them after publishing.
- `TestFriendlyDDLExecutionService` (used only by the test factory) records the generated DDL scripts without touching an actual database.

## Continuous Integration
- `.github/workflows/ci.yml` restores dependencies and runs `dotnet test --configuration Release` on every push and pull request targeting `main` or `master`, ensuring the new integration coverage participates in CI.
