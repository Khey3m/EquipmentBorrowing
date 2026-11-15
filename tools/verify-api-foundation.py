"""Build the API first, then verify its real HTTP foundation without PostgreSQL or Desktop."""
import datetime
import json
import os
from pathlib import Path
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

root = Path(__file__).resolve().parents[1]
dll = root / "src/EquipmentBorrowing.Api/bin/Release/net10.0/EquipmentBorrowing.Api.dll"
if not dll.exists():
    raise SystemExit("Build the API in Release before running this verifier.")
with socket.socket() as listener:
    listener.bind(("127.0.0.1", 0))
    port = listener.getsockname()[1]
base = f"http://127.0.0.1:{port}"
environment = dict(os.environ)
environment.pop("EQUIPMENT_BORROWING_CONNECTION_STRING", None)
environment["ASPNETCORE_ENVIRONMENT"] = "Production"
checks = []

def request(path, method="GET"):
    try:
        response = urllib.request.urlopen(urllib.request.Request(base + path, method=method), timeout=3)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        return response.status, dict(response.headers), json.load(response)

with tempfile.TemporaryFile() as log:
    process = subprocess.Popen(["dotnet", str(dll), "--urls", base], cwd=dll.parent,
                               env=environment, stdout=log, stderr=log)
    try:
        for attempt in range(60):
            if process.poll() is not None:
                raise RuntimeError("API exited before accepting requests; inspect host setup.")
            try:
                status, headers, body = request("/api/v1/status")
                break
            except (urllib.error.URLError, TimeoutError):
                time.sleep(0.25)
        else:
            raise RuntimeError("API did not start within 15 seconds.")
        assert status == 200 and body["service"] == "CampusEQ API"
        checks.append({"method":"GET", "path":"/api/v1/status", "status":status, "body":body})
        status, headers, document = request("/openapi/v1.json")
        assert status == 200 and document["openapi"].startswith("3.")
        assert "/api/v1/status" in document["paths"]
        assert not any(path.startswith("/api/v1/students") for path in document["paths"])
        checks.append({"method":"GET", "path":"/openapi/v1.json", "status":status,
                       "openapi":document["openapi"], "paths":list(document["paths"])})
        for path, method, expected, code in [("/api/v1/does-not-exist","GET",404,"not_found"),
                                            ("/api/v1/status","POST",405,"method_not_allowed")]:
            status, headers, body = request(path, method)
            assert status == expected and body["code"] == code
            assert set(body) == {"code", "message"}
            if expected == 405:
                assert "GET" in headers.get("Allow", "")
            checks.append({"method":method, "path":path, "status":status,"body":body})
        print(json.dumps({"observed_at_utc":datetime.datetime.now(datetime.timezone.utc).isoformat(),
                          "database_connection_setting":"absent in child process",
                          "url_override_verified":True, "checks":checks},indent=2))
    finally:
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
