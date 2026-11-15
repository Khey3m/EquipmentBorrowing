# Required endpoint contract - student subset implemented

Source: supplied API and Acceptance Reference Section 2. Prefix /api/v1; UTF-8 JSON. This catalog describes the complete target. Session 2 implements API-02-06; the other eleven endpoints remain pending. Generated OpenAPI at /openapi/v1.json and openapi-current.json describe the implemented routes, including operational status. No placeholder business routes pretend to return successful data.

| API | Method | Relative route | Request/query | Success and expected rejection |
|---|---|---|---|---|
| API-01 | GET | `/equipment` | available=true optional; malformed Boolean -> 400 | 200 equipment[] |
| API-02 | GET | `/students` | search optional, name or number | 200 student[] |
| API-03 | GET | `/students/{id}` | stable opaque ID | 200 student; 404 |
| API-04 | POST | `/students` | studentNumber,fullName,contact | 201 student + Location API-03; 400/409 |
| API-05 | PUT | `/students/{id}` | complete student editable fields; null clears contact | 200 student; 400/404/409 |
| API-06 | PATCH | `/students/{id}/eligibility` | isEligible Boolean, required; same value no-op | 200 student; 400/404 |
| API-07 | GET | `/equipment/{id}` | stable opaque ID | 200 equipment; 404 |
| API-08 | POST | `/equipment` | equipmentCode,name,description | 201 equipment + Location API-07; 400/409 |
| API-09 | PUT | `/equipment/{id}` | complete equipment editable fields; protected/unknown fields rejected | 200 equipment; 400/404/409 |
| API-10 | POST | `/borrowings` | studentId,equipmentId,expectedReturnDate | 201 borrowing + Location API-13; 400/404/409 |
| API-11 | POST | `/checkout-requests` | studentId,equipmentIds[],expectedReturnDate | 201 {borrowings:[]}; 400/404/409 |
| API-12 | POST | `/borrowings/{id}/return` | empty JSON object {} | 200 borrowing; 404/409 |
| API-13 | GET | `/borrowings/{id}` | stable opaque ID | 200 borrowing; 404 |
| API-14 | GET | `/borrowings` | search; status=All|Active|Returned; sort=borrowedDate|expectedReturnDate; direction=asc|desc | 200 borrowing[]; 400 unsupported value |
| API-15 | GET | `/students/{id}/borrowings` | student ID; empty history differs from missing student | 200 borrowing[]; 404 |
| API-16 | GET | `/equipment/{id}/borrowings` | equipment ID; empty history differs from missing item | 200 borrowing[]; 404 |

All database/server failures require a safe 500 body. Errors use {code,message}; unknown routes return 404, wrong methods 405. Empty lists are arrays, not errors. Unreachable API means no response, not 500. The server owns checkout/return dates and transaction fields. DateOnly serializes YYYY-MM-DD; campus calendar remains Asia/Manila.

Student response: id,studentNumber,fullName,contact,isEligible. Equipment response: id,equipmentCode,name,description,isAvailable. Borrowing response: id,studentId,equipmentId,borrowedDate,expectedReturnDate,returnedDate,status, with studentNumber/studentName/equipmentCode/equipmentName on lists/history. Draft DTOs allow related display fields on detail responses too; required list/history fields will be populated rather than null.

Creates omit optional contact/description or send null; updates send complete editable fields, with null clearing the optional value. Server validation must reject missing required values, wrong JSON types and protected/unknown fields. PATCH uses nullable Boolean at binding so missing eligibility cannot become false silently. IDs are database-generated and treated as opaque; clients discover them rather than assuming seed keys.

Borrowing defaults: status All, sort borrowedDate, direction asc. Search is trimmed case-insensitive partial matching over student name/number and equipment name; filters combine, ascending borrowing ID breaks ties in either date direction. Multi-checkout duplicate/fewer-than-two IDs and invalid dates -> 400; missing references -> 404; business conflicts -> 409. Fresh database reads establish atomicity, not response status alone.

Result mapping gate: Lab 4 OperationOutcome.Rejected and Conflict do not yet distinguish every HTTP category. Session 2 added structured application failure categories for student endpoints; other services will adopt them as needed; do not parse message text or incorrectly equate every conflict with 409. Preserve Unknown and never automatically replay uncertain writes.
