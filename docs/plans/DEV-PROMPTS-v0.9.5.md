# v0.9.5 Development Prompts

This document contains pre-defined prompts for each task in v0.9.5. Use these prompts to initiate the corresponding development tasks.

## Phase 1: System Governance

### TASK-01: Audit Log UI
**Prompt:**
```markdown
# Context
We have an existing `AuditLogs` table and a ChangeTracker-based auditing system (backend). We need a frontend UI to visualize these logs.

# Objective
Implement `AuditLogManagement.razor` page to search, filter, and view audit logs.

# Requirements
1.  **Page Location**: `/system/audit-logs` (MenuItem: System > Audit Logs).
2.  **Permissions**: `SYS.AUDIT`.
3.  **UI Components**:
    -   `AntDesign.Table` for listing logs.
    -   Filters: Entity Type (Select), User (Search), Date Range (DatePicker), Action Type (Create/Update/Delete).
    -   Detail View: A drawer or modal showing the "Before" and "After" values (JSON diff or unified diff view).
4.  **Backend**:
    -   Create `AuditLogService` (if not exists) with `SearchAsync` method supporting pagination and filtering.
    -   Ensure sensitive data is masked if applicable (check `SensitiveAttribute`).

# Constraints
-   Follow `STD-04` coding standards.
-   Use `AntDesign` components strictly.
-   Ensure I18n support.
```

### TASK-02: Job Monitor
**Prompt:**
```markdown
# Context
The system runs background jobs (e.g., entity publishing, database migration). We need a monitor to track their status.

# Objective
Implement `BackgroundJobMonitor.razor`.

# Requirements
1.  **Page Location**: `/system/jobs`.
2.  **Permissions**: `SYS.JOBS`.
3.  **UI**:
    -   List of recent jobs with status (Running, Completed, Failed).
    -   Log viewer for a specific job execution.
    -   Button to "Cancel" running jobs (if supported).
4.  **Integration**:
    -   Connect to the existing `IBackgroundJobClient` or equivalent job storage to fetch status.

# Constraints
-   Use `AntDesign` Progress bars and Status badges.
-   Auto-refresh capability (SignalR or polling).
```

## Phase 2: Advanced Settings

### TASK-03: I18n Resource Editor
**Prompt:**
```markdown
# Context
Localization resources are currently stored in `LocalizationResources` table. Administrators need a way to modify these without DB access.

# Objective
Enhance `Settings.razor` or create `I18nEditor.razor` to manage translations.

# Requirements
1.  **UI**:
    -   DataGrid showing Key, Culture, Value.
    -   Inline editing for 'Value'.
    -   Filter by Culture and Key (wildcard).
2.  **Functionality**:
    -   Save changes to DB.
    -   "Reload Cache" button to apply changes immediately across the app (invalidate `I18n` cache).
3.  **Safety**:
    -   Warn when modifying system keys.

# Constraints
-   Reuse `DataGridRuntime` or standard `Table` with inline edit.
```

### TASK-04: SMTP & Notification Settings
**Prompt:**
```markdown
# Context
System needs to send emails (password reset, notifications).

# Objective
Implement SMTP configuration UI in `Settings.razor`.

# Requirements
1.  **Fields**: Host, Port, Username, Password (Masked), Enable SSL, From Address, Display Name.
2.  **Actions**:
    -   "Save" (Encrypt password in DB).
    -   "Send Test Email": A button to verify config.
3.  **Backend**:
    -   Update `EmailSender` service to read config from DB/Settings instead of just `appsettings.json` (if dynamic config is required).

# Constraints
-   Security: Ensure password is never sent back to UI in plain text.
```

## Phase 3: Business Core

### TASK-05: Customer 360 View
**Prompt:**
```markdown
# Context
The `Customer` entity needs a comprehensive view aggregating related data (Contacts, Opportunities, Activities).

# Objective
Implement a "Master-Detail" or "Composite" template for Customer.

# Requirements
1.  **Template Design**:
    -   Header: Customer basic info (Card).
    -   Tabs: "Contacts", "opportunities", "Activity History".
2.  **Implementation**:
    -   Use `AggVO` (Aggregate Value Object) pattern to load/save Customer + Contacts in one go if needed.
    -   Or use lazy-loaded tabs for related lists.
3.  **Configuration**:
    -   Update `Customer` entity template to use this custom layout or configure the generic layout to support tabs/relations.

# Constraints
-   Must work within the `FormDesigner` ecosystem (or be a specialized custom page if the designer doesn't support it yet - prefer generic enhancement).
```

### TASK-06: Lookup UI Enhancement
**Prompt:**
```markdown
# Context
Foreign keys currently show UUIDs or raw IDs in some grids.

# Objective
Enhance `DataGrid` and specific `EntitySelector` to show friendly names.

# Requirements
1.  **DataGrid**:
    -   When a column is a `Lookup`, fetch and display the related entity's `Name` or `Title`.
2.  **EntitySelector**:
    -   Standardize the modal used to select related entities (e.g., picking a Customer for an Order).
    -   Support searching and pagination in the selector.

# Constraints
-   Performance: Avoid N+1 queries in DataGrid (use Include or Batch Load).
```
