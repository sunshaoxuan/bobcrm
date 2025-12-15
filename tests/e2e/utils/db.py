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

    def table_exists(self, table_name):
        # Postgres query for table existence
        query = f"SELECT to_regclass('public.\"{table_name}\"')"
        val = self.execute_scalar(query)
        return val and val.lower() != "null" and val != ""

db_helper = DbHelper()
