import subprocess
import os

class DbHelper:
    def __init__(self, container_name="bobcrm-pg", db_name="bobcrm", user="postgres"):
        self.container_name = container_name
        self.db_name = db_name
        self.user = user

    def execute_query(self, query):
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
            # Don't raise for now to avoid crashing tests on cleanup failure, but log it
            return None

    def execute_scalar(self, query):
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
            return None

    def execute_rows(self, query, separator="|"):
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
