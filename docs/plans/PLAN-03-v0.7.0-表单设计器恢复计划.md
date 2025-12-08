# PLAN-03: FormDesigner Restoration & Implementation

## Goal
Restore and fully implement `src/BobCrm.App/Components/Pages/FormDesigner.razor`, which is currently empty. This component is critical for the "Template System Loop" (v0.7.0). It will provide a visual interface for editing `FormTemplate` layouts.

## User Review Required
> [!IMPORTANT]
> Since the original file content was lost/empty, this plan proposes a **new implementation** based on the project's architectural patterns (AntDesign Blazor, `ITemplateService`, Native Blazor DnD).

## Proposed Design

### 1. Layout Structure
Use `AntDesign.Layout`:
-   **Left Sider (250px)**: **Toolbox**. Displays available fields (`FieldMetadata`) for the entity. Valid drop source.
-   **Content (Auto)**: **Canvas**. Renders the current layout widgets. Valid drop target. Supports reordering.
-   **Right Sider (300px)**: **Properties Details**. Shows properties of the selected widget (Label, Width, Visibility, etc.).

### 2. Data Model
-   **State**: `FormTemplate _template` (Loaded via `TemplateService`).
-   **Widgets**: `List<Dictionary<string, object>> _widgets` (Parsed from `_template.LayoutJson`).
-   **Available Fields**: `List<FieldMetadata> _fields` (Loaded via `EntityDefService` or parsed from entity definition).

### 3. Drag and Drop Logic (Native Blazor)
Adopting the pattern from `MenuManagement.razor`:
-   **Draggable Items**: Toolbox items and Canvas widgets.
-   **Events**:
    -   `@ondragstart`: Capture `draggingId` and source type (Toolbox vs Canvas).
    -   `@ondragover`: `event.PreventDefault()` to allow drop.
    -   `@ondrop`: Handle dropping:
        -   **New Field**: Add widget to list.
        -   **Reorder**: Move widget within list.
    -   `draggable="true"` attribute on items.

### 4. Integration
-   **Service**: `[Inject] ITemplateService TemplateService`
-   **Service**: `[Inject] IEntityDefinitionService EntityService` (to get available fields).
-   **Persistence**: Serialize `_widgets` back to JSON and call `TemplateService.UpdateTemplateAsync`.

### 5. Components to Implement (Internal or Shared)
-   `FormDesignerUtils.cs`: Helper for widget serialization/deserialization logic (reuse `DefaultTemplateGenerator` rules if possible).
-   `PropertyEditor.razor`: (Optional separate component or inline) Renders inputs based on selected widget type.

## Proposed Changes

### [BobCrm.App]

#### [NEW] [FormDesigner.razor](file:///c:/workspace/bobcrm/src/BobCrm.App/Components/Pages/FormDesigner.razor)
-   Implement full Razor component with `AntDesign` UI.
-   Logic:
    -   `OnInitializedAsync`: Load template by `Id` (from query param).
    -   `SaveAsync`: JSON serialize -> Update Service.
    -   `RenderWidget`: Helper to render preview of widgets (Input, Select, etc.) in the canvas (using read-only mode or dummy placeholders).

#### [MODIFY] [Templates.razor](file:///c:/workspace/bobcrm/src/BobCrm.App/Components/Pages/Templates.razor)
-   Ensure the "Edit" button navigates to `/form-designer/{TemplateId}`.

## Verification Plan

### Automated Tests
-   Verify build success: `dotnet build`.
-   Verify I18n compliance: `pwsh scripts/check-i18n.ps1`.

### Manual Verification
1.  **Navigation**: Go to "Templates" -> Click "Design" (or Edit) on a template -> Landing on Form Designer.
2.  **Rendering**: Confirm Left (Fields), Center (Form Preview), Right (Properties) panels appear.
3.  **Interaction**:
    -   Drag a field from Left to Center -> new widget appears.
    -   Drag a widget in Center to reorder.
    -   Click a widget -> Right panel shows properties.
    -   Change label in Right panel -> Center preview updates.
4.  **Persistence**: Click "Save" -> Reload page -> Changes persist.
