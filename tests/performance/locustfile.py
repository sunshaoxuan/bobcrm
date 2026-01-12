import time
import uuid
import random
import requests
import json
from locust import HttpUser, task, between, events

# Target the stable entity created via debug script
SHARED_ENTITY_NAME = "PerfProduct_Stable"
SHARED_FULL_TYPE_NAME = f"BobCrm.Base.Performance.{SHARED_ENTITY_NAME}"

@events.test_start.add_listener
def on_test_start(environment, **kwargs):
    print(f"Starting Performance Test against pre-loaded entity: {SHARED_ENTITY_NAME}")

class BobCrmUser(HttpUser):
    wait_time = between(1, 3)
    
    def on_start(self):
        self.login()
        self.entity_name = SHARED_ENTITY_NAME
        self.full_type_name = SHARED_FULL_TYPE_NAME

    def login(self):
        # Login to get token
        try:
            response = self.client.post("/api/auth/login", json={"username": "admin", "password": "Admin@12345"})
            if response.status_code == 200:
                token = response.json()['data']['accessToken']
                self.client.headers.update({"Authorization": f"Bearer {token}"})
            else:
                print(f"Login failed: {response.status_code}")
        except Exception as e:
                print(f"Login Exception: {e}")

    @task(3)
    def load_users_list(self):
        with self.client.get("/api/users", catch_response=True) as response:
            if response.status_code == 200:
                try:
                    data = response.json().get("data", [])
                    if data:
                        self.target_user_id = random.choice(data).get("id")
                except:
                    pass
            else:
                response.failure(f"List Users Failed: {response.status_code}")

    @task(5)
    def load_user_detail(self):
        if hasattr(self, 'target_user_id') and self.target_user_id:
             self.client.get(f"/api/users/{self.target_user_id}")
        else:
             # Fallback to list if no ID yet
             self.load_users_list()

    # Removed dynamic entity tasks
