# Template Defaults & Designer Updates

- System entity sync now always runs `GetInitialDefinition` to hydrate metadata before saving and regenerates default templates/bindings safely.
- Default template generator uses i18n keys for template names (pattern `TEMPLATE_NAME_{USAGE}_{ENTITY}`), adds a Back button to new Edit templates, and builds richer List DataGrid defaults (search, refresh, pagination, bulk/row actions, enum column hints).
- Form Designer loads entities/fields on init with retry, shows a Cancel/Return button in the header, and auto-applies a default DataGrid when opening an empty List template.
- DataGrid runtime falls back to default/placeholder columns when none are defined and translates header keys when possible.
- Template list UI now attempts to translate template names and entity tags using i18n keys.
