"""Minimal PixelLab v2 REST client: auth, JSON calls, background-job polling and downloads."""
from __future__ import annotations

import base64
import os
import time
from pathlib import Path
from typing import Any

import requests

BASE_URL = "https://api.pixellab.ai/v2"
REPO_ROOT = Path(__file__).resolve().parents[2]


class PixelLabError(RuntimeError):
    pass


class RateLimited(PixelLabError):
    """Too many requests or concurrent jobs; wait for running jobs to finish, then retry."""


def load_token() -> str:
    """Reads PIXELLAB_API_TOKEN from the environment, or from a gitignored pixellab.env / .env at the repo root."""
    token = os.environ.get("PIXELLAB_API_TOKEN")
    if token:
        return token.strip()
    for env_file in (REPO_ROOT / "pixellab.env", REPO_ROOT / ".env", Path(__file__).with_name(".env")):
        if env_file.exists():
            for line in env_file.read_text(encoding="utf-8").splitlines():
                key, _, value = line.partition("=")
                if key.strip() == "PIXELLAB_API_TOKEN" and value.strip():
                    return value.strip().strip('"').strip("'")
    raise PixelLabError(
        "No PixelLab token. Create a .env file in the repo root containing\n"
        "  PIXELLAB_API_TOKEN=your-token\n"
        "(get it from https://pixellab.ai/account). Both files are gitignored."
    )


class PixelLab:
    def __init__(self, token: str | None = None, timeout: float = 120.0):
        self.session = requests.Session()
        self.session.headers["Authorization"] = f"Bearer {token or load_token()}"
        self.timeout = timeout

    def _request(self, method: str, path: str, **kwargs: Any) -> Any:
        url = path if path.startswith("http") else f"{BASE_URL}{path}"
        for attempt in range(6):
            response = self.session.request(method, url, timeout=self.timeout, **kwargs)
            if response.status_code == 429 and attempt >= 2:
                raise RateLimited(f"{method} {path} -> 429: {response.text[:300]}")
            if response.status_code == 429 or response.status_code >= 500:
                wait = min(60, 5 * (attempt + 1))
                print(f"    {response.status_code} from {path}; retrying in {wait}s")
                time.sleep(wait)
                continue
            if not response.ok:
                raise PixelLabError(f"{method} {path} -> {response.status_code}: {response.text[:1000]}")
            if "application/json" in response.headers.get("content-type", ""):
                return response.json()
            return response.content
        raise PixelLabError(f"{method} {path} kept failing after retries")

    def get(self, path: str) -> Any:
        return self._request("GET", path)

    def post(self, path: str, body: dict) -> Any:
        return self._request("POST", path, json=body)

    def balance(self) -> dict:
        return self.get("/balance")

    def wait_for_job(self, job_id: str, poll_seconds: float = 8.0, timeout_seconds: float = 1800.0) -> dict:
        """Polls a background job until it completes. Raises if it fails or times out."""
        start = time.time()
        while True:
            job = self.get(f"/background-jobs/{job_id}")
            status = job.get("status")
            if status == "completed":
                return job
            if status == "failed":
                raise PixelLabError(f"Job {job_id} failed: {job.get('last_response')}")
            if time.time() - start > timeout_seconds:
                raise PixelLabError(f"Job {job_id} timed out (last status {status})")
            time.sleep(poll_seconds)

    def download(self, url: str, dest: Path) -> None:
        dest.parent.mkdir(parents=True, exist_ok=True)
        # Asset URLs are public; don't send the token to the storage host.
        response = requests.get(url, timeout=self.timeout)
        response.raise_for_status()
        dest.write_bytes(response.content)


def save_base64_image(image: dict, dest: Path) -> None:
    dest.parent.mkdir(parents=True, exist_ok=True)
    data = image["base64"]
    if "," in data and data.startswith("data:"):
        data = data.split(",", 1)[1]
    dest.write_bytes(base64.b64decode(data))
