# Session 1 - independent host and contract foundation

Date: 2026-11-15. Source checkpoint: 0e6dcd9 plus the verifier committed with this evidence.

## Delivered and observed

- Added API and dependency-free JSON transport projects to the existing net10 solution.
- Registered existing backend services and factory contexts in the server; no database connection/migration/seed is triggered by host liveness or OpenAPI retrieval.
- Drafted all sixteen required contracts in endpoint-contract.md. Business routes remain pending; generated OpenAPI truthfully contains only the implemented GET /api/v1/status operational route.
- Full solution Release build: zero warnings/errors. Existing Desktop code remains intact.
- With no Desktop process running, Production host launched on an explicit loopback URL with the PostgreSQL connection variable removed from its environment. GET status -> 200, GET OpenAPI -> 200 (3.1.1), unknown route -> 404 JSON, POST to GET-only status -> 405 JSON plus Allow: GET. See session1-http-evidence.json.
- The verifier stops its child host when finished and creates no database. Default manual host address is http://127.0.0.1:5085; the observed automated run verifies --urls override with a free local port.

Safe 400/500 middleware is implemented but database-error and invalid-business-body execution remains pending until database-backed endpoints exist. Registration validation does not prove HTTP error mapping; broad Lab 4 Rejected/Conflict categories will be refined when student endpoints are implemented in Session 2. No human-message parsing was introduced.

No T01-T08 pass is claimed: T01 requires the actual business reads and all sixteen OpenAPI contracts, beyond this operational foundation. Existing migration/schema and saved connections were not changed. The generated OpenAPI document is runtime-retrievable; a committed complete sixteen-operation export will be produced after implementation.

## Reproduction

```powershell
dotnet build EquipmentBorrowing.slnx --configuration Release
python tools/verify-api-foundation.py
dotnet run --project src/EquipmentBorrowing.Api --configuration Release --no-build
```

Then open http://127.0.0.1:5085/api/v1/status and http://127.0.0.1:5085/openapi/v1.json. Ctrl+C stops the manually started host. Database-backed endpoints will read EQUIPMENT_BORROWING_CONNECTION_STRING privately in the API process; do not aim acceptance tests at the Lab 4 demonstration database. No runtime business routes are implemented yet.

Implementation references: API/Program.cs (host, JSON errors, liveness/OpenAPI); API/BackendServices.cs (composition); Contracts/ApiContracts.cs (draft DTOs); tools/verify-api-foundation.py (real HTTP verification).

Official API-host reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi?view=aspnetcore-10.0
