"""Build a screenshot-oriented Word walkthrough. Requires python-docx."""
import argparse
from pathlib import Path
import shutil
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[1]/"docs/lab5/DEMO_LAB_5_DRAFT.docx")
parser.add_argument("--copy-to", type=Path)
args = parser.parse_args()
for path in [args.output, args.copy_to]:
    if path is not None and path.exists():
        parser.error("Output exists; choose a new filename to preserve edits.")
doc = Document()
section = doc.sections[0]
section.top_margin = section.bottom_margin = Inches(.65)
section.left_margin = section.right_margin = Inches(.75)
doc.styles["Normal"].font.name = "Calibri"
doc.styles["Normal"].font.size = Pt(11)
for style in ["Title", "Heading 1", "Heading 2"]:
    doc.styles[style].font.color.rgb = RGBColor.from_string("126A78")
footer = section.footer.paragraphs[0]
footer.alignment = WD_ALIGN_PARAGRAPH.RIGHT
footer.add_run("Lab 5 draft | Page ")
field = OxmlElement("w:fldSimple")
field.set(qn("w:instr"), "PAGE")
footer._p.append(field)

def para(text): doc.add_paragraph(text)
def bullet(text): doc.add_paragraph(text, "List Bullet")
def code(text):
    table = doc.add_table(rows=1, cols=1)
    table.style = "Table Grid"
    table.cell(0, 0).text = text.strip()
    for p in table.cell(0, 0).paragraphs:
        for run in p.runs:
            run.font.name = "Consolas"
            run.font.size = Pt(9)
    para("")

shot_number = 0
def shot(caption):
    global shot_number
    shot_number += 1
    doc.add_paragraph(f"S{shot_number:02} - {caption}").runs[0].bold = True
    table = doc.add_table(rows=1, cols=1)
    table.style = "Table Grid"
    table.rows[0].height = Inches(1.1)
    table.cell(0, 0).text = "INSERT YOUR ACTUAL SCREENSHOT HERE\n" + caption
    for p in table.cell(0, 0).paragraphs:
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    para("")

def step(title):
    doc.add_page_break()
    doc.add_heading(title, 1)

def expected(text):
    doc.add_paragraph("Expected: " + text).runs[0].bold = True
    para("Observed: [Fill after execution; do not copy the expected result as an observation.]")
    para("Result: [Pass / Fail / Not run]    Evidence filename: __________________")

doc.add_heading("DEMO LAB 5", 0)
doc.add_heading("Step-by-step walkthrough and screenshot evidence draft", 1)
para("Campus Equipment Borrowing System - API Integration and Acceptance Testing")
para("DRAFT: Screenshot boxes and observed-result fields are intentionally blank. This document does not mark T07 passed.")
doc.add_heading("Is video required?", 2)
para("Neither supplied Lab 5 PDF explicitly requires video or screen recording. Activity Section III requires a short UI demonstration. Reference T07 requires an API-alone request, UI capture, database readback and a code/dependency walkthrough. Follow any additional format instruction posted by your teacher.")
para("Word can organize UI screenshots and explanations, but screenshots alone do not replace actual HTTP requests/status/bodies, fresh SQL state, transaction evidence or architecture references. The reference explicitly distinguishes these evidence types. A teacher may still ask for a live demonstration.")
for text in [
    "Follow the steps on a separate disposable Lab 5 database; keep Lab 4 data intact.",
    "Replace each placeholder with your actual image and retain the screenshot label/caption.",
    "Use only synthetic values below; keep connection credentials and private identities out of captures.",
    "Fill actual observations, failures, corrections and retests only after execution.",
    "Give the completed document/evidence location to Codex to update consolidated T07 results and prepare the final tag/package."
]: bullet(text)
para("Date: __________  Tested commit: __________  Demo database: __________")
para("Campus date (Asia/Manila): __________  Shared future due date: __________")

step("Preparation - Separate database, API terminal and Desktop terminal")
bullet("In pgAdmin, open Query Tool on postgres and create a NEW database. If this name exists, choose a new one; never overwrite existing data.")
code("CREATE DATABASE equipment_borrowing_lab5_walkthrough;")
bullet("Visual Studio > View > Terminal > Developer PowerShell. Run from the project folder containing EquipmentBorrowing.slnx. Keep this as Terminal 1/API.")
bullet("Enter a private connection targeting the new database. Do not screenshot the connection entry.")
code("""$walkthroughSecret = Read-Host 'PostgreSQL connection string for new walkthrough DB' -AsSecureString
$env:EQUIPMENT_BORROWING_CONNECTION_STRING = [System.Net.NetworkCredential]::new('', $walkthroughSecret).Password
dotnet build EquipmentBorrowing.slnx --configuration Release
dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Infrastructure --configuration Release --no-build
dotnet run --project src/EquipmentBorrowing.Api --configuration Release --no-build""")
para("Private shape: Host=localhost;Port=5432;Database=equipment_borrowing_lab5_walkthrough;Username=<local-role>;Password=<local-password>. Replace placeholders privately. API does not automatically seed; fresh empty lists are valid.")
bullet("Use Terminal 2 for Desktop; start it when Step 2 says:")
code("""$env:EQUIPMENT_BORROWING_API_BASE_URL = 'http://127.0.0.1:5085/'
dotnet run --project src/EquipmentBorrowing.Desktop --configuration Release --no-build""")
para("Desktop needs only API origin. Keep API running when restarting Desktop.")
doc.add_heading("Prepare the supplied controlled baseline once", 2)
bullet("After migration, connect pgAdmin Query Tool to the NEW walkthrough database. Run this once before Step 1. It creates instructor-supplied S1-S4/E1-E7; the later UI steps add separate demo records. Do not run it on existing development data.")
code('''BEGIN;
INSERT INTO "Students" ("StudentNumber","FullName","IsEligibleToBorrow") VALUES
('ST-100','Ada Cruz',true),('ST-200','Ben Diaz',false),
('ST-300','Cara Lim',true),('ST-400','Dani Ong',true);
INSERT INTO "Equipment" ("EquipmentIdentifier","Name","IsAvailable") VALUES
('EQ-001','Camera',false),('EQ-002','Laptop',true),('EQ-003','Projector',true),
('EQ-004','Tablet',false),('EQ-005','Microphone',false),
('EQ-006','Tripod',true),('EQ-007','Speaker',true);
WITH demo_clock AS (
 SELECT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Manila')::date AS d
), seed_loans(student_number,equipment_code,borrow_offset,due_offset,return_offset,status) AS (
 VALUES ('ST-100','EQ-001',-9,-1,NULL::int,'Active'),
 ('ST-100','EQ-003',-8,2,-2,'Returned'),
 ('ST-300','EQ-004',-7,2,NULL::int,'Active'),
 ('ST-300','EQ-005',-7,2,NULL::int,'Active')
)
INSERT INTO "Borrowings" ("StudentId","EquipmentId","BorrowedAt","ExpectedReturnAt","ActualReturnAt","Status")
SELECT s."Id",e."Id",c.d+l.borrow_offset,c.d+l.due_offset,
 CASE WHEN l.return_offset IS NULL THEN NULL ELSE c.d+l.return_offset END,l.status
FROM seed_loans l CROSS JOIN demo_clock c
JOIN "Students" s ON s."StudentNumber"=l.student_number
JOIN "Equipment" e ON e."EquipmentIdentifier"=l.equipment_code;
COMMIT;
SELECT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Manila')::date AS campus_date;''')
para("Record this actual campus date. Reference dates are shifted consistently by the supplied offsets; do not change the system clock. Existing controlled T01-T08 executions remain in the retained evidence. This document adds your rendered UI observations rather than replacing those captures.")

step("Step 1 - API works with Desktop closed (T01/T07)")
bullet("Close Desktop; keep API running. Use a spare PowerShell terminal:")
code("curl.exe -i http://127.0.0.1:5085/api/v1/equipment")
bullet("Open http://127.0.0.1:5085/openapi/v1.json in a browser.")
expected("HTTP 200 with the seven seeded items while Desktop is closed. OpenAPI describes all sixteen operations; valid empty arrays remain distinct from errors.")
shot("API-only GET showing URL, HTTP 200 and JSON body")
shot("Machine-readable OpenAPI showing route/contract information")

student_sql = """SELECT current_database();
SELECT "Id", "StudentNumber", "FullName", "ContactInformation", "IsEligibleToBorrow"
FROM "Students" WHERE "StudentNumber"='ST-UI-600';"""
equipment_sql = """SELECT "Id", "EquipmentIdentifier", "Name", "Description", "IsAvailable"
FROM "Equipment" WHERE "EquipmentIdentifier" LIKE 'EQ-UI-%'
ORDER BY "EquipmentIdentifier";"""
loan_sql = """SELECT b."Id", s."StudentNumber", e."EquipmentIdentifier", b."BorrowedAt",
 b."ExpectedReturnAt", b."ActualReturnAt", b."Status", e."IsAvailable"
FROM "Borrowings" b JOIN "Students" s ON s."Id"=b."StudentId"
JOIN "Equipment" e ON e."Id"=b."EquipmentId"
WHERE s."StudentNumber"='ST-UI-600' ORDER BY b."Id";"""

step("Step 2 - Register/edit student and verify identity (T02/T07)")
bullet("Start Desktop with Terminal 2 commands. Students > New student:")
code("Student number: ST-UI-600\nFull name: Demo Student\nContact: demo.student@example.edu")
bullet("Register, then run the following in pgAdmin on the walkthrough database. Note the generated Id BEFORE editing:")
code(student_sql)
bullet("Select that student; edit name to Demo Edited Student and clear contact completely. Save. Execute the same query again.")
expected("One durable identity, same Id, edited name, contact NULL and eligible true. UI displays refreshed values.")
shot("Registered student with eligible default and original ID readback")
shot("Edited student and fresh SQL with same ID and NULL contact")

step("Step 3 - Equipment management and empty-history controls (T03/T07)")
bullet("Manage equipment > New equipment before each registration:")
code("""EQ-UI-600 | Demo Camera | Demonstration camera
EQ-UI-601 | Demo Tripod | Demonstration tripod
EQ-UI-602 | Demo Microphone | Demonstration microphone
EQ-UI-603 | Demo Tablet | Zero-history control""")
bullet("Run SQL before editing the camera, then edit its name to Demo Edited Camera and clear description. Save and run SQL again:")
code(equipment_sql)
bullet("Also register ST-UI-603 / Demo Empty Student with blank contact. Do not borrow for this control student or use EQ-UI-603.")
expected("Available defaults, same camera ID after edit, NULL description. Management has no editable availability.")
shot("Equipment management registration/edit with protected availability")
shot("Equipment SQL before/after: stable ID, NULL description, available defaults")

step("Step 4 - Catalog, filter and single issue (T04/T07)")
bullet("Open Equipment catalog; compare the edited camera details. Toggle Available items only and Apply filter / Refresh.")
bullet("Select ST-UI-600 and EQ-UI-600. Choose an expected date AFTER the actual server campus date. Reuse it for Step 6.")
bullet("Before issue, run the SQL below and save the baseline result; then Borrow selected item, open Active borrowings, and run SQL again.")
code(loan_sql)
expected("One Active camera loan; item unavailable and excluded from available-only results.")
shot("Active camera borrowing after single issue")
shot("Refreshed catalog or available-only results excluding the on-loan camera")

step("Step 5 - Explicit return confirmation and retained history (T04/T07)")
bullet("Run the Step 4 SQL before return and save it. Select the Active camera loan, show the confirmation requirement, tick Confirm return, then Return selected item.")
bullet("Refresh catalog and open student/camera history. Run:")
code(loan_sql)
expected("Same loan ID retained as Returned with actual campus return date; camera available again; loan removed only from Active list.")
shot("Return confirmation and refreshed Active list")
shot("Returned history and fresh SQL showing retained ID and available camera")

step("Step 6 - Coordinated multi-item checkout (T05/T07)")
bullet("Save fresh Step 3/5 SQL before checkout. Multiple checkout: ST-UI-600; select EQ-UI-601 and EQ-UI-602; use one shared future due date.")
bullet("Check out selected items. Inspect Active borrowings and refreshed catalog. Run the Step 5 SQL again.")
expected("Two new Active rows, both items unavailable, checked selections cleared; original camera loan remains Returned.")
shot("Two-item selection and successful refreshed checkout")
shot("Two Active loans plus fresh PostgreSQL dates/status/availability")
para("Atomic failure proof already exists in session5-checkout-evidence.json and retained CI checkout capture: temporary AFTER INSERT fault, safe 500 and identical before/after state. Cite that evidence rather than manufacturing a shared failure.")

step("Step 7 - Student/equipment histories and empty state (T06/T07)")
bullet("Histories > Student: select ST-UI-600, then unused ST-UI-603.")
bullet("Switch to Equipment: select EQ-UI-600, then unused EQ-UI-603. Observe rows replace rather than mix.")
expected("Student has one Returned plus two Active loans. Camera has returned history. Control student/tablet have a valid empty history.")
shot("Student history with related names, dates and both statuses")
shot("Equipment history and a labeled empty control result")

step("Step 8 - Search, combined criteria, sort and Clear (T06/T07)")
code("Student number: ST-UI-600\nStudent name: Demo Edited\nEquipment name: Tripod\nNo match: NON-EXISTENT-999")
bullet("Equipment NAME is searchable; equipment-code search is not required by this contract.")
bullet("Combine student search, Active status, expected-return-date sort and descending. Equal due dates use ascending borrowing ID.")
bullet("Show no-match, then Clear criteria.")
expected("Two Active rows for combined criteria, clear empty result for no match, then restored All/borrowed-date/ascending defaults.")
shot("Combined search/filter/sort and tied-date ID order")
shot("Valid no-match and/or Clear restoring default list")

step("Step 9 - Independent API edit appears after Desktop refresh (T07)")
bullet("Keep API/Desktop running. In another PowerShell terminal:")
code("""$matches = Invoke-RestMethod 'http://127.0.0.1:5085/api/v1/students?search=ST-UI-600'
$demoStudent = @($matches) | Where-Object { $_.studentNumber -eq 'ST-UI-600' }
$body = @{studentNumber='ST-UI-600';fullName='Demo External Edit';contact=$null} | ConvertTo-Json
$response = Invoke-WebRequest -UseBasicParsing -Method Put -Uri "http://127.0.0.1:5085/api/v1/students/$($demoStudent.id)" -ContentType 'application/json' -Body $body
"HTTP $([int]$response.StatusCode)"
$response.Content""")
bullet("Desktop > Students > Search / Refresh and reselect ST-UI-600. Execute Step 2 SQL again.")
expected("HTTP 200; Desktop and DB show Demo External Edit, same ID, NULL contact and preserved history.")
shot("Independent PUT context, HTTP 200 and actual JSON response")
shot("Desktop refreshed external edit and fresh SQL readback")

step("Step 10 - No HTTP response versus received 500 and recovery (T08)")
bullet("Save fresh readback first. Stop ONLY Terminal 1/API with Ctrl+C; leave Desktop open. Refresh Desktop and run:")
code("curl.exe -i http://127.0.0.1:5085/api/v1/equipment")
bullet("No HTTP response is expected. Next, start API using an invalid DB port in that API process only; do not stop shared PostgreSQL:")
code("""$workingApiConnection = $env:EQUIPMENT_BORROWING_CONNECTION_STRING
$env:EQUIPMENT_BORROWING_CONNECTION_STRING = $workingApiConnection + ';Port=1;Timeout=2;Command Timeout=2'
dotnet run --project src/EquipmentBorrowing.Api --configuration Release --no-build""")
bullet("Refresh Desktop and run the curl GET again. Capture safe HTTP 500 and its distinct UI feedback.")
bullet("Stop API with Ctrl+C, restore the connection and restart:")
code("""$env:EQUIPMENT_BORROWING_CONNECTION_STRING = $workingApiConnection
dotnet run --project src/EquipmentBorrowing.Api --configuration Release --no-build""")
bullet("Retry connection / Refresh in the SAME Desktop window. Compare fresh SQL. Verify state before deciding to resubmit an uncertain write.")
expected("No response, then received safe 500, then restored 200/data. No credentials/stack traces; saved records unchanged.")
shot("Actual Desktop no-response feedback while API is stopped")
shot("Safe received 500 body and distinct Desktop server-error feedback")
shot("Same Desktop after Retry, restored data and fresh SQL")
para("Prior real Session 9 JSON/PNGs may be cited as automated evidence; do not relabel them as your own new execution. This is not a live lost-commit-acknowledgment experiment.")

step("Step 11 - Final process restart and fresh persistence readback")
bullet("Close Desktop completely. Restart API too if demonstrating server restart; keep its correct connection and the SAME database.")
bullet("Reopen Desktop with only API origin. Re-run Step 2/3/5 SQL, comparing IDs/fields/relationships with earlier screenshots.")
expected("Demo External Edit and cleared fields persist; camera available with retained Returned loan; tripod/microphone unavailable with two Active loans. No startup overwrite.")
shot("Reopened Desktop showing edited student and Active/history state")
shot("Fresh SQL after restart with same IDs, dates, statuses and availability")

step("Step 12 - Architecture, tests and evidence references")
bullet("Show DesktopComposition.cs (HTTP adapters), Desktop csproj (no Infrastructure/EF), and Api/BackendServices.cs (server persistence).")
bullet("Explain View > ViewModel > HTTP > API > Application > EF/Npgsql > PostgreSQL; show OpenAPI and traceability.")
bullet("Show CI run 43710719990: 99 tests, five persistence stages, seven HTTP scenarios. Raw TRX/OpenAPI/HTTP captures are retained in docs/lab5/evidence/ci-43710719990.zip with checksum inventory.")
bullet("Show before/after SQL and documented fault point for controlled T05 rollback. Screenshots/statuses alone do not prove transaction integrity.")
shot("Client HTTP composition versus server backend/dependency code")
shot("Verified CI and relevant traceability/raw evidence references")
para("Explanation: server owns rules/dates/transactions; client awaits JSON and refreshes. No response differs from 500. Unknown writes require verification, not automatic replay.")

step("Completion record and Google Classroom checklist")
for label in ["Date/commit/environment", "Database/campus date", "Steps performed", "Expected versus observed summary",
              "Failures/diagnosis/correction/retest", "Evidence filenames/locations", "Unperformed branches"]:
    para(label + ": ______________________________________________")
para("T07 rendered sequence: [Pass / Fail / Not run] - fill only after actual execution.")
doc.add_heading("What to pass", 2)
for text in [
    "Final source ZIP after actual T07 evidence is recorded and completed revision is tagged lab5-submitted.",
    "GitHub repository/branch/final commit/tag link.",
    "This completed screenshot Word document plus any additional teacher-required live/recorded demonstration.",
    "Actual API/SQL/rollback/architecture evidence or clear references to the raw evidence included in the source ZIP.",
    "Each partner submits individually and identifies both partners privately."
]: bullet(text)
para("Current review ZIP stays a draft while T07 is Partial. No video requirement was found in the two supplied PDFs; additional Classroom/teacher instructions still apply.")
args.output.parent.mkdir(parents=True, exist_ok=True)
doc.save(args.output)
if args.copy_to:
    args.copy_to.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(args.output, args.copy_to)
print(str(args.output))
print(f"Screenshot placeholders: {shot_number}; walkthrough steps: 12; observations blank.")
if args.copy_to: print(str(args.copy_to))
