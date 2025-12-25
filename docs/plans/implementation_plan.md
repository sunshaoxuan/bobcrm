# TASK-05: Customer 360 View Implementation Plan

## Goal Description
Implement a "Customer 360" view by enabling Master-Detail (Parent-Child) relationships in the generic Form Engine. This allows displaying related lists (e.g., Contacts, Opportunities) within a Customer's detail page using Tabs and DataGrids.

## User Review Required
> [!IMPORTANT]
> This change introduces "Context Filtering" to `DataGridWidget`, allowing it to behave as a "Related List".

## Proposed Changes

### Frontend Core (BobCrm.App)
#### [MODIFY] [DataGridWidget.cs](file:///c:/workspace/bobcrm/src/BobCrm.App/Models/Widgets/DataGridWidget.cs)
- Add properties for context filtering:
    - `bool FilterByContext` (Enable filtering by parent ID)
    - `string? ContextKey` (The key in the parent context, e.g., "Id")
    - `string? TargetField` (The foreign key field in the target entity, e.g., "CustomerId")

#### [MODIFY] [DataGridWidgetComponent.razor](file:///c:/workspace/bobcrm/src/BobCrm.App/Components/Widgets/Runtime/DataGridWidgetComponent.razor)
- Inject `FormRuntimeContext`.
- In `LoadDataAsync`, check `FilterByContext`.
- If enabled, retrieve the `ContextKey` value from `FormRuntimeContext.Data` (the parent record).
- Append a filter condition (`TargetField == ContextValue`) to the data query.

#### [MODIFY] [FormDesigner.razor](file:///c:/workspace/bobcrm/src/BobCrm.App/Components/Pages/FormDesigner.razor)
- (Or `WidgetRegistry`) Update `DataGridWidget` property metadata to expose the new properties in the Property Editor.

### Configuration / Templates
#### [MODIFY] [DefaultTemplateGenerator.cs](file:///c:/workspace/bobcrm/src/BobCrm.App/Services/Designer/DefaultTemplateGenerator.cs)
- Update `GenerateDefaultTemplate` for `Customer` entity (if special handling desired) OR create a **Manual Seed** for "Customer 360".
- **Design**:
    - **Header**: Basic Info (Card).
    - **Tabs**:
        - Tab 1: **Contacts** (DataGrid: Entity=Contact, FilterByContext=True, TargetField=CustomerId).
        - Tab 2: **Opportunities** (DataGrid: Entity=Opportunity, FilterByContext=True, TargetField=CustomerId).

## Verification Plan

### Manual Verification
1.  **Configure Template**:
    - Open Form Designer for `Customer`.
    - Add a `TabContainer`.
    - Add a `DataGrid` in a tab.
    - Set DataGrid Entity to `Contact`.
    - Enable "Filter by Context". Set Target Field to `CustomerId`.
2.  **Runtime Test**:
    - Open a Customer record (e.g., `CUST-001`).
    - Verify the Contacts grid only shows contacts linked to `CUST-001`.
    - Create a new Contact via the grid (if "Create" supported) and verify it auto-links (this might be a future enhancement, for now just verifying display).
