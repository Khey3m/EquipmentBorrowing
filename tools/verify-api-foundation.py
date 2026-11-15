"""Build the API first, then verify its real HTTP foundation without PostgreSQL or Desktop."""
import argparse
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

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--openapi-output", type=Path)
args = parser.parse_args()
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
        if "/api/v1/students" in document["paths"]:
            assert {"get", "post"} <= document["paths"]["/api/v1/students"].keys()
            assert {"get", "put"} <= document["paths"]["/api/v1/students/{id}"].keys()
            assert "patch" in document["paths"]["/api/v1/students/{id}/eligibility"]
            schemas = document["components"]["schemas"]
            update_schema = schemas["StudentUpdateInput"]
            assert {"studentNumber", "fullName", "contact"} <= set(update_schema["required"])
            assert update_schema.get("additionalProperties") is False
            create = document["paths"]["/api/v1/students"]["post"]["responses"]
            assert {"201", "400", "409", "500"} <= create.keys()
        if "/api/v1/equipment" in document["paths"]:
            paths = document["paths"]
            assert {"get", "post"} <= paths["/api/v1/equipment"].keys()
            assert {"get", "put"} <= paths["/api/v1/equipment/{id}"].keys()
            available = next(p for p in paths["/api/v1/equipment"]["get"]["parameters"] if p["name"] == "available")
            assert available["in"] == "query"
            schema = document["components"]["schemas"]["EquipmentUpdateInput"]
            assert {"equipmentCode", "name", "description"} <= set(schema["required"])
            assert schema.get("additionalProperties") is False
            assert "isAvailable" not in schema["properties"]
            create = paths["/api/v1/equipment"]["post"]["responses"]
            assert {"201", "400", "409", "500"} <= create.keys()
            assert any(p["name"] == "search" for p in paths["/api/v1/students"]["get"]["parameters"])
        if "/api/v1/borrowings/{id}" in document["paths"]:
            paths = document["paths"]
            assert "post" in paths["/api/v1/borrowings"]
            assert "get" in paths["/api/v1/borrowings/{id}"]
            assert "post" in paths["/api/v1/borrowings/{id}/return"]
            schemas = document["components"]["schemas"]
            assert {"studentId", "equipmentId", "expectedReturnDate"} <= set(schemas["BorrowInput"]["required"])
            assert schemas["ReturnInput"].get("additionalProperties") is False
            assert not schemas["ReturnInput"].get("properties", {})
            assert set(schemas["BorrowingState"]["enum"]) == {"Active", "Returned"}
            assert {"201", "400", "404", "409", "500"} <= paths["/api/v1/borrowings"]["post"]["responses"].keys()
        if "/api/v1/checkout-requests" in document["paths"]:
            paths = document["paths"]
            operations = {op.get("operationId") for path in paths.values() for op in path.values() if isinstance(op, dict)}
            assert {f"API-{i:02}" for i in range(1,17)} <= operations
            parameters = {p["name"]:p for p in paths["/api/v1/borrowings"]["get"]["parameters"]}
            for name, default, values in [("status","All",{"All","Active","Returned"}),
                ("sort","borrowedDate",{"borrowedDate","expectedReturnDate"}), ("direction","asc",{"asc","desc"})]:
                assert parameters[name]["schema"]["default"] == default
                assert set(parameters[name]["schema"]["enum"]) == values
            assert {"studentId", "equipmentIds", "expectedReturnDate"} <= set(document["components"]["schemas"]["CheckoutInput"]["required"])
            for path in ["/api/v1/students", "/api/v1/equipment", "/api/v1/borrowings"]:
                assert "Location" in paths[path]["post"]["responses"]["201"]["headers"]
        if args.openapi_output:
            args.openapi_output.parent.mkdir(parents=True, exist_ok=True)
            args.openapi_output.write_text(json.dumps(document, indent=2) + "\n")
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
