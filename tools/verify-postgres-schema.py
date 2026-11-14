"""Verify authored migration SQL using a newly created disposable PostgreSQL database.
Requires PGHOST, PGPORT, PGUSER, PGPASSWORD and psql on PATH (or PSQL_PATH).
Never prints connection credentials. Leaves the verification database for inspection.
This verifies PostgreSQL SQL, not EF execution or desktop acceptance.
"""
from datetime import datetime, timezone
from pathlib import Path
import os
import re
import shutil
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
psql = os.environ.get("PSQL_PATH") or shutil.which("psql")
if not psql:
    raise SystemExit("Set PSQL_PATH or add PostgreSQL bin to PATH.")
database = "equipment_borrowing_verify_" + datetime.now(timezone.utc).strftime("%Y%m%d%H%M%S%f")

def run(sql, db=database, expected=None):
    result = subprocess.run([psql, "-X", "-w", "-d", db, "-v", "ON_ERROR_STOP=1",
        "-v", "VERBOSITY=verbose", "-At"], input=sql, capture_output=True, text=True)
    if expected:
        if result.returncode == 0 or expected not in result.stderr:
            raise AssertionError("Expected PostgreSQL rejection " + expected)
    elif result.returncode:
        raise RuntimeError("PostgreSQL verification failed; inspect locally without publishing credentials.")
    return result.stdout.strip()

run('CREATE DATABASE "' + database + '";', db="postgres")
print("Disposable database: " + database)
schema = (root / "docs/lab4/postgres-schema.sql").read_text()
migration = (root / "src/EquipmentBorrowing.Infrastructure/Persistence/Migrations/20261114000100_InitialPostgres.cs").read_text()
up_sql = migration.split('public const string SchemaSql = """', 1)[1].split('""";', 1)[0].strip()
assert schema.split("\n", 1)[1].strip() == up_sql, "SQL copy differs from migration"
run(schema)
print("PASS: exact migration Up SQL creates clean PostgreSQL schema")
run('INSERT INTO "Students" ("StudentNumber","FullName","IsEligibleToBorrow") VALUES (\'STU-001\',\'Alice\',true);')
run('INSERT INTO "Equipment" ("EquipmentIdentifier","Name","IsAvailable") VALUES (\'EQ-001\',\'Scope\',true),(\'EQ-002\',\'Meter\',true);')
run('INSERT INTO "Students" ("StudentNumber","FullName","IsEligibleToBorrow") VALUES (\'stu-001\',\'Duplicate\',true);', expected="23505")
run('INSERT INTO "Students" ("StudentNumber","FullName","IsEligibleToBorrow") VALUES (\' STU-001 \',\'Duplicate\',true);', expected="23514")
run('INSERT INTO "Equipment" ("EquipmentIdentifier","Name","IsAvailable") VALUES (\'eq-001\',\'Duplicate\',true);', expected="23505")
run('INSERT INTO "Students" ("StudentNumber","FullName","IsEligibleToBorrow") VALUES (\'OTHER\',\' \',true);', expected="23514")
run('INSERT INTO "Students" ("StudentNumber","FullName","IsEligibleToBorrow") VALUES (E\'\\tSTU-001\\t\',\'Duplicate\',true);', expected="23514")
print("PASS: generated canonical keys reject case duplicates; whitespace/blank checks reject invalid input")
insert = 'INSERT INTO "Borrowings" ("StudentId","EquipmentId","BorrowedAt","ExpectedReturnAt","Status") VALUES '
run(insert + "(999999,1,'2026-11-14','2026-11-15','Active');", expected="23503")
run(insert + "(1,1,'2026-11-14','2026-11-14','Active');", expected="23514")
run(insert + "(1,1,'2026-11-14','2026-11-15','Returned');", expected="23514")
run(insert + "(1,1,'2026-11-14','2026-11-15','Active');")
run('UPDATE "Equipment" SET "IsAvailable"=false WHERE "Id"=1;')
run(insert + "(1,1,'2026-11-14','2026-11-16','Active');", expected="23505")
print("PASS: foreign keys, return-date/state checks and partial unique active-item index")
before = run('SELECT "Id", "IsAvailable" FROM "Equipment" ORDER BY "Id"; SELECT count(*) FROM "Borrowings";')
run('BEGIN; UPDATE "Equipment" SET "IsAvailable"=false WHERE "Id"=2;' +
    insert + "(1,2,'2026-11-14','2026-11-15','Active');" +
    insert + "(1,1,'2026-11-14','2026-11-15','Active'); COMMIT;", expected="23505")
after = run('SELECT "Id", "IsAvailable" FROM "Equipment" ORDER BY "Id"; SELECT count(*) FROM "Borrowings";')
assert before == after, "Transaction left partial changes"
print("PASS: failure after UPDATE and INSERT leaves no partial changes (fresh connection readback)")
run('BEGIN; UPDATE "Borrowings" SET "Status"=\'Returned\',"ActualReturnAt"=\'2026-11-14\' WHERE "EquipmentId"=1; UPDATE "Equipment" SET "IsAvailable"=true WHERE "Id"=1; COMMIT;')
run(insert + "(1,1,'2026-11-14','2026-11-16','Active');")
assert run('SELECT count(*) FROM "Borrowings" WHERE "EquipmentId"=1;') == "2"
print("PASS: returned history retained and a subsequent active loan allowed")
print("Schema checks passed. EF migration/history, seeding and application execution require separate verification.")
