import requests
import os

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")

class ApiHelper:
    def __init__(self, base_url=BASE_URL, api_base=API_BASE):
        self.base_url = base_url
        self.api_base = api_base
        self.token = None
        self.refresh_token = None

    def login(self, username: str, password: str):
        """Logs in with provided credentials to get a token for subsequent API calls."""
        url = f"{self.api_base}/api/auth/login"
        payload = {"username": username, "password": password}
        resp = requests.post(url, json=payload)
        if resp.status_code == 200:
            data = resp.json()
            self.token = data["data"]["accessToken"]
            self.refresh_token = data["data"].get("refreshToken")
            return True
        return False

    def login_as_admin(self):
        """Logs in as admin to get a token for subsequent API calls."""
        return self.login("admin", "Admin@12345")

    def get_headers(self):
        headers = {"Content-Type": "application/json"}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        return headers

    def delete(self, endpoint):
        url = f"{self.api_base}{endpoint}"
        return requests.delete(url, headers=self.get_headers())
    
    def post(self, endpoint, data):
        url = f"{self.api_base}{endpoint}"
        return requests.post(url, json=data, headers=self.get_headers())

    def put(self, endpoint, data):
        url = f"{self.api_base}{endpoint}"
        return requests.put(url, json=data, headers=self.get_headers())

api_helper = ApiHelper()
