import subprocess
import re


def _escape_regex_prefix(prefix: str) -> str:
    """Escape a prefix for usage inside a PostgreSQL regex literal."""
    return re.escape(prefix)

class DbHelper:
    def __init__(self, container_name="bobcrm-pg", db_name="bobcrm", user="postgres"):
        self.container_name = container_name
        self.db_name = db_name
        self.user = user

    def execute_query(self, query, strict: bool = False):
        """Executes a SQL query/command via docker exec psql."""
        cmd = [
            "docker", "exec", "-i", self.container_name, 
            "psql", "-U", self.user, "-d", self.db_name, "-c", query
        ]
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, check=True)
            return result.stdout
        except subprocess.CalledProcessError as e:
            print(f"DB Error: {e.stderr}")
            if strict:
                raise
            # Back-compat: keep cleanup helpers from crashing tests unless explicitly strict
            return None

    def execute_scalar(self, query, strict: bool = False):
        """Executes a scalar query and returns the first value string."""
        # psql -t -A (tuples only, no align) returns just the value
        cmd = [
            "docker", "exec", "-i", self.container_name, 
            "psql", "-U", self.user, "-d", self.db_name, "-t", "-A", "-c", query
        ]
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, check=True)
            return result.stdout.strip() if result.stdout else None
        except subprocess.CalledProcessError as e:
            print(f"DB Error: {e.stderr}")
            if strict:
                raise
            return None

    def execute_rows(self, query, separator="|", strict: bool = False):
        """
        Executes a query and returns rows parsed by a field separator.

        Uses psql -t -A -F to produce machine-friendly output:
        -t: tuples only
        -A: unaligned
        -F: field separator
        """
        cmd = [
            "docker", "exec", "-i", self.container_name,
            "psql", "-U", self.user, "-d", self.db_name,
            "-t", "-A", "-F", separator,
            "-c", query
        ]
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, check=True)
            out = result.stdout.strip() if result.stdout else ""
            if not out:
                return []
            lines = [ln for ln in out.splitlines() if ln.strip() != ""]
            return [ln.split(separator) for ln in lines]
        except subprocess.CalledProcessError as e:
            print(f"DB Error: {e.stderr}")
            if strict:
                raise
            return []

    def table_exists(self, table_name):
        """
        Checks whether a table exists in the public schema.

        Supports both quoted (PascalCase) and unquoted (lowercase) identifiers.
        """
        quoted = f'public."{table_name}"'
        unquoted = f"public.{table_name}"
        query = f"SELECT COALESCE(to_regclass('{quoted}'), to_regclass('{unquoted}'))"
        val = self.execute_scalar(query)
        return bool(val and val.lower() != "null")

db_helper = DbHelper()


def drop_all_dynamic_content(
    prefixes: tuple[str, ...] = ("Test_", "Perf_"),
    strict: bool = True,
):
    """
    Batch6 - Global Teardown Hardening

    - Hard drop all physical tables in public schema that start with Test_/Perf_ (case-insensitive).
    - Delete corresponding dynamic metadata:
      - EntityDefinitions + FieldMetadatas
      - FormTemplates (+ related bindings/state bindings/function-node refs) by entity route

    Returns a dict summary for reporting/debugging.
    """
    if not prefixes:
        return {"dropped_tables": [], "entity_routes": [], "entity_definition_ids": []}

    # Use regex (~*) to avoid LIKE '_' escaping pitfalls across Postgres settings.
    table_where_parts = []
    for p in prefixes:
        esc = _escape_regex_prefix(p)
        table_where_parts.append(f"tablename ~* '^{esc}'")
    table_where_sql = " OR ".join(table_where_parts)

    # 1) Discover candidate tables (for observability)
    discover_tables_sql = (
        "SELECT tablename FROM pg_tables "
        f"WHERE schemaname='public' AND ({table_where_sql}) "
        "ORDER BY tablename;"
    )
    table_rows = db_helper.execute_rows(discover_tables_sql, strict=strict)
    candidate_tables = [r[0] for r in table_rows if r and r[0]]

    # 2) Hard drop with a single dynamic DO block (recursive via CASCADE)
    drop_sql = f"""
DO $$
DECLARE r record;
BEGIN
  FOR r IN
    SELECT tablename FROM pg_tables
    WHERE schemaname='public' AND ({table_where_sql})
  LOOP
    EXECUTE format('DROP TABLE IF EXISTS %I CASCADE', r.tablename);
  END LOOP;
END $$;
""".strip()
    db_helper.execute_query(drop_sql, strict=strict)

    # 3) Gather entity definition ids/routes for metadata cleanup
    ed_where_parts = []
    for p in prefixes:
        esc = _escape_regex_prefix(p)
        ed_where_parts.append(f"\"EntityName\" ~* '^{esc}'")
    ed_where_sql = " OR ".join(ed_where_parts)

    ed_rows = db_helper.execute_rows(
        "SELECT \"Id\", \"EntityRoute\" FROM \"EntityDefinitions\" "
        f"WHERE ({ed_where_sql}) "
        "ORDER BY \"EntityRoute\";",
        strict=strict,
    )
    ed_ids: list[str] = []
    entity_routes: list[str] = []
    for row in ed_rows:
        if not row:
            continue
        if len(row) >= 1 and row[0]:
            ed_ids.append(row[0])
        if len(row) >= 2 and row[1]:
            entity_routes.append(row[1])

    # 4) Delete metadata (order matters due to FK constraints)
    if ed_ids:
        id_list = ", ".join("'" + x.replace("'", "''") + "'" for x in ed_ids)
        db_helper.execute_query(
            f"DELETE FROM \"FieldMetadatas\" WHERE \"EntityDefinitionId\" IN ({id_list});",
            strict=strict,
        )
        db_helper.execute_query(
            f"DELETE FROM \"EntityDefinitions\" WHERE \"Id\" IN ({id_list});",
            strict=strict,
        )

    if entity_routes:
        route_list = ", ".join("'" + r.replace("'", "''") + "'" for r in entity_routes)

        # Detach FunctionNodes -> TemplateStateBindings (avoid FK violations)
        db_helper.execute_query(
            f"""
UPDATE "FunctionNodes"
SET "TemplateStateBindingId" = NULL
WHERE "TemplateStateBindingId" IN (
  SELECT "Id" FROM "TemplateStateBindings"
  WHERE "EntityType" IN ({route_list})
);
""".strip(),
            strict=strict,
        )

        # Remove bindings/state bindings/templates for those entity routes
        db_helper.execute_query(
            f"DELETE FROM \"TemplateBindings\" WHERE \"EntityType\" IN ({route_list});",
            strict=strict,
        )
        db_helper.execute_query(
            f"DELETE FROM \"TemplateStateBindings\" WHERE \"EntityType\" IN ({route_list});",
            strict=strict,
        )
        db_helper.execute_query(
            f"DELETE FROM \"FormTemplates\" WHERE \"EntityType\" IN ({route_list});",
            strict=strict,
        )

    # 5) Post-check: ensure no test/perf tables remain
    remaining_rows = db_helper.execute_rows(discover_tables_sql, strict=strict)
    remaining_tables = [r[0] for r in remaining_rows if r and r[0]]
    if strict and remaining_tables:
        raise RuntimeError(f"Global cleanup failed: remaining dynamic tables: {remaining_tables}")

    return {
        "dropped_tables": candidate_tables,
        "entity_routes": entity_routes,
        "entity_definition_ids": ed_ids,
        "remaining_tables": remaining_tables,
    }
