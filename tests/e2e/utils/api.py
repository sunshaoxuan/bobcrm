import requests

BASE_URL = "http://localhost:3000"

class ApiHelper:
    def __init__(self, base_url=BASE_URL):
        self.base_url = base_url
        self.token = None

    def login_as_admin(self):
        """Logs in as admin to get a token for subsequent API calls."""
        # Using the direct API port or through proxy if needed. 
        # Here we assume the app is running and proxying correctly or we use 5200 directly if needed.
        # But E2E tests usually test against 3000.
        url = f"{self.base_url}/api/auth/login"
        payload = {"username": "admin", "password": "Admin@12345"}
        resp = requests.post(url, json=payload)
        if resp.status_code == 200:
            data = resp.json()
            self.token = data['data']['accessToken']
            return True
        return False

    def get_headers(self):
        headers = {"Content-Type": "application/json"}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        return headers

    def delete(self, endpoint):
        url = f"{self.base_url}{endpoint}"
        return requests.delete(url, headers=self.get_headers())
    
    def post(self, endpoint, data):
        url = f"{self.base_url}{endpoint}"
        return requests.post(url, json=data, headers=self.get_headers())

    def put(self, endpoint, data):
        url = f"{self.base_url}{endpoint}"
        return requests.put(url, json=data, headers=self.get_headers())

api_helper = ApiHelper()
