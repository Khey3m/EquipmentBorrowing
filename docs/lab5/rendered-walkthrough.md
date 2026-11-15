# Remaining rendered T07 walkthrough

Status: Not performed by the agent. This is a script, not a pass record. Use synthetic values; keep private identities/credentials outside screenshots. Automated HTTP ViewModels, native startup/catalog and error feedback already passed in Sessions 8-9.

1. Start the API independently with a prepared disposable database using README. With Desktop closed, GET /api/v1/equipment and retrieve /openapi/v1.json; retain actual response/status or reference the retained CI capture.
2. Start Desktop using only EQUIPMENT_BORROWING_API_BASE_URL. Record revision, server campus date, environment and a short screen recording or labeled screenshots.
3. Students: register ST-UI-600, Demo Student, demo.student@example.edu. Note the returned internal ID via API readback. Edit name to Demo Edited Student and clear contact. Observe refreshed details; show the same ID.
4. Manage equipment: register EQ-UI-600 / Demo Camera, EQ-UI-601 / Demo Tripod and EQ-UI-602 / Demo Microphone. Edit the camera name and clear description; show unchanged identity and available defaults.
5. Catalog: issue EQ-UI-600 to ST-UI-600 with a date after the actual campus date. Show unavailable state and the Active row. In Active borrowings, select it, confirm return and return it; show retained Returned history and available state.
6. Multiple checkout: select EQ-UI-601/602 for that student with one future date. Show both Active loans and both unavailable flags after refresh. Observe that stale selections/checks clear.
7. Histories: select the student and then the camera in Equipment mode. Show related names/IDs/dates/status and empty history for another unused subject. Search the demo student, combine Active/date/direction, then Clear.
8. Independently PUT the student's complete editable fields through an HTTP client using the discovered opaque ID. Change the name to Demo External Edit, preserve studentNumber and send contact:null. Refresh Students in Desktop and show the external change. Do not hardcode a database primary key.
9. Close the entire Desktop process and reopen it against the same API. Compare edited details, active/returned history and availability with fresh PostgreSQL/API reads. API remains independently usable. For failure/recovery, use recorded Session 9 or repeat its isolated strategy; do not stop shared PostgreSQL.
10. Add an actual T07 execution entry to test-execution.md: date/commit/environment, steps/data, expected versus observed, pass/fail, diagnosis/correction/retest and evidence paths. Mark unperformed branches explicitly. Do not replace this status with Pass merely because you read the script.

Fresh SQL should confirm unchanged IDs/relationships, null cleared fields, the retained returned camera loan and two active multi-item loans. Existing automated evidence covers broader validation, tie ordering, rollback and unknown-outcome handling; cite that evidence with its scope.

After the required rendered sequence is recorded, update submission-handoff.md and baseline.md with the actual completed revision, create/push lab5-submitted, then regenerate the final package. The current review draft does not substitute for that completion record.
