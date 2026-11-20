"""Supervised Lab 6 Windows Desktop acceptance with an isolated PostgreSQL database."""
import argparse
import concurrent.futures
import json
import os
from pathlib import Path
import re
import subprocess
import time
import uuid
from urllib.error import URLError
from urllib.request import Request, urlopen
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from threading import Thread

ROOT = Path(__file__).resolve().parents[1]
API_DLL = ROOT / "src/EquipmentBorrowing.Api/bin/Release/net10.0/EquipmentBorrowing.Api.dll"
DESKTOP = ROOT / "src/EquipmentBorrowing.Desktop/bin/Release/net10.0/EquipmentBorrowing.Desktop.exe"


def call(base, path, data=None):
    payload = None if data is None else json.dumps(data).encode()
    method = "GET" if data is None else "PUT"
    with urlopen(Request(base + path, payload, {"Content-Type": "application/json"}, method=method), timeout=12) as response:
        return json.load(response)


def stop(process):
    if process is not None and process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=10)


def start_api(environment):
    process = subprocess.Popen(
        ["dotnet", str(API_DLL), "--urls", "http://127.0.0.1:0"],
        cwd=API_DLL.parent, env=environment, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
    )

    def listen():
        address = None
        for line in process.stdout:
            match = re.search(r"Now listening on: (http://127\.0\.0\.1:\d+)", line)
            if match and address is None:
                address = match.group(1)
                ready.set_result(address)
        if address is None:
            ready.set_exception(RuntimeError("API exited before listening."))

    ready = concurrent.futures.Future()
    Thread(target=listen, daemon=True).start()
    try:
        address = ready.result(timeout=30)
        call(address, "/api/v1/status")
        return process, address
    except BaseException:
        stop(process)
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--prepare-only", action="store_true", help="Exercise setup and HTTP/SQL checks without opening Desktop.")
    parser.add_argument("--output", type=Path, default=ROOT / "TestResults/lab6/session9/manual-observations.json")
    args = parser.parse_args()
    if not API_DLL.exists() or not DESKTOP.with_suffix(".deps.json").exists() or (not args.prepare_only and not DESKTOP.exists()):
        parser.error("Build EquipmentBorrowing.slnx in Release first.")
    psql = os.environ.get("PSQL_PATH", r"C:\Program Files\PostgreSQL\18\bin\psql.exe")
    environment = dict(os.environ)
    environment["ASPNETCORE_ENVIRONMENT"] = environment["DOTNET_ENVIRONMENT"] = "Production"
    environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information"
    database = "equipment_lab6_manual_" + uuid.uuid4().hex
    admin_connection = environment["POSTGRES_INTEGRATION_ADMIN_CONNECTION"].rstrip(";")
    environment["EQUIPMENT_BORROWING_CONNECTION_STRING"] = admin_connection + f";Database={database}"

    def sql(target, statement):
        run = subprocess.run([psql, "-X", "-w", "-At", "-v", "ON_ERROR_STOP=1", "-d", target],
                             input=statement, text=True, capture_output=True, env=environment)
        if run.returncode:
            raise RuntimeError("PostgreSQL setup/read failed. Check private PG settings and role permissions.")
        return run.stdout.strip()

    def snapshot():
        return json.loads(sql(database, '''SELECT jsonb_build_object(
          'students', (SELECT jsonb_agg(to_jsonb(s) ORDER BY "Id") FROM "Students" s),
          'equipment', (SELECT jsonb_agg(to_jsonb(e) ORDER BY "Id") FROM "Equipment" e),
          'borrowings', (SELECT jsonb_agg(to_jsonb(b) ORDER BY "Id") FROM "Borrowings" b));'''))

    def observe(prompt):
        if args.prepare_only:
            return ""
        return input(prompt + "\nDescribe what you actually observed, including any discrepancy (blank if unobserved): ").strip()

    state = {"upstream": None, "delay": 0}
    trace = {"mode": "setup rehearsal" if args.prepare_only else "supervised", "database": database}
    api = desktop = proxy = None
    created = False

    class Proxy(BaseHTTPRequestHandler):
        def do_GET(self):
            upstream = state["upstream"]
            if upstream is None:
                self.close_connection = True
                return
            if self.path.startswith("/api/v1/students"):
                time.sleep(state["delay"])
            try:
                with urlopen(upstream + self.path, timeout=12) as response:
                    body = response.read()
                    self.send_response(response.status)
                    self.send_header("Content-Type", response.headers["Content-Type"])
                    self.send_header("Content-Length", str(len(body)))
                    self.end_headers()
                    self.wfile.write(body)
            except (URLError, OSError):
                self.close_connection = True

        def log_message(self, *_):
            pass

    try:
        sql("postgres", f'CREATE DATABASE "{database}";')
        created = True
        migration = subprocess.run(["dotnet", "ef", "database", "update", "--project", "src/EquipmentBorrowing.Infrastructure",
                                    "--startup-project", "src/EquipmentBorrowing.Infrastructure",
                                    "--configuration", "Release", "--no-build"],
                                   cwd=ROOT, env=environment, capture_output=True)
        if migration.returncode:
            raise RuntimeError("Migration failed. Check dotnet tool restore and private connection settings.")
        sql(database, '''INSERT INTO "Students" ("StudentNumber","FullName","IsEligibleToBorrow") VALUES ('ST-900','Session Nine Before',true);
                         INSERT INTO "Equipment" ("EquipmentIdentifier","Name","IsAvailable") VALUES ('EQ-900','Session Nine Camera',true);''')
        baseline = snapshot()
        api, state["upstream"] = start_api(environment)
        equipment = call(state["upstream"], "/api/v1/equipment")
        openapi = call(state["upstream"], "/openapi/v1.json")
        expected = json.loads((ROOT / "docs/lab5/openapi-current.json").read_text(encoding="utf-8"))
        def operations(document):
            return {(path, method): value for path, methods in document["paths"].items()
                    for method, value in methods.items() if value.get("operationId", "").startswith("API-")}
        assert len(operations(openapi)) == 16 and operations(openapi) == operations(expected)
        assert openapi["components"]["schemas"] == expected["components"]["schemas"]
        assert [row["equipmentCode"] for row in equipment] == ["EQ-900"]
        trace["independent_api"] = {"equipment": equipment, "openapi_operations": 16}
        deps = json.loads(DESKTOP.with_suffix(".deps.json").read_text(encoding="utf-8"))
        forbidden = ("equipmentborrowing.infrastructure", "npgsql", "microsoft.entityframeworkcore")
        assert not any(name.lower().startswith(forbidden) for name in deps["libraries"])
        project = (ROOT / "src/EquipmentBorrowing.Desktop/EquipmentBorrowing.Desktop.csproj").read_text(encoding="utf-8")
        assert "EquipmentBorrowing.Http" in project
        assert not any(name in project.lower() for name in forbidden)
        trace["desktop_http_dependency_check"] = "Passed"

        proxy = ThreadingHTTPServer(("127.0.0.1", 0), Proxy)
        Thread(target=proxy.serve_forever, daemon=True).start()
        origin = f"http://127.0.0.1:{proxy.server_port}"
        desktop_env = {key: value for key, value in os.environ.items()
                       if key.upper() not in {"EQUIPMENT_BORROWING_CONNECTION_STRING", "POSTGRES_INTEGRATION_ADMIN_CONNECTION"}
                       and not key.upper().startswith("PG")}
        desktop_env["EQUIPMENT_BORROWING_API_BASE_URL"] = origin
        if not args.prepare_only:
            desktop = subprocess.Popen([str(DESKTOP)], cwd=DESKTOP.parent, env=desktop_env)
        state["delay"] = 8 if not args.prepare_only else 0.2
        started = time.monotonic()
        assert len(call(origin, "/api/v1/students")) == 1
        assert time.monotonic() - started >= state["delay"]
        trace["delayed_ui"] = observe("MA6-01: Click Students or Search / Refresh, watch the loading indicator, and move the window or switch to Borrowing histories while the student request is delayed. Return to Students after it loads.")
        state["delay"] = 0
        stop(api)
        state["upstream"] = None
        try:
            call(origin, "/api/v1/students")
        except OSError:
            pass
        else:
            raise AssertionError("Expected the stopped API to produce no HTTP response.")
        trace["offline_ui"] = observe("The isolated API is stopped. On Students click Search / Refresh and note the feedback.")
        api, state["upstream"] = start_api(environment)
        assert len(call(origin, "/api/v1/students")) == 1
        trace["recovered_ui"] = observe("The API is restored. Click Search / Refresh on Students and confirm the row returns.")
        assert snapshot() == baseline
        trace["recovery_sql_unchanged"] = True

        trace["before_edit_ui"] = observe("MA6-02: Verify ST-900 displays Session Nine Before.")
        student, = call(state["upstream"], "/api/v1/students")
        path = f'/api/v1/students/{student["id"]}'
        response = call(state["upstream"], path, {"studentNumber": "ST-900", "fullName": "Session Nine After", "contact": None})
        assert response == student | {"fullName": "Session Nine After"}
        after = snapshot()
        expected_after = json.loads(json.dumps(baseline))
        expected_after["students"][0]["FullName"] = "Session Nine After"
        assert after == expected_after
        trace["external_edit"] = {"before": student, "after": response, "sql_only_expected_field_changed": True}
        trace["after_edit_ui"] = observe("An independent API client updated ST-900. Click Search / Refresh and check that the same record now displays Session Nine After.")
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(trace, indent=2) + "\n", encoding="utf-8")
        print(f"Saved evidence to {args.output}")
    finally:
        stop(desktop)
        if proxy is not None:
            proxy.shutdown()
            proxy.server_close()
        stop(api)
        if created:
            sql("postgres", f'DROP DATABASE "{database}";')


if __name__ == "__main__":
    main()
