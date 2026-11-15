# Lab 4 continuity baseline

Lab 4 source revision: `51f47ea5c127a09de16dbd95bd21c63197080823`.

Identified from the final Lab 4 ZIP/manifest prepared in this conversation for Google Classroom: `EquipmentBorrowing-Lab4-51f47ea5c127.zip`. Local package SHA-256 verified: `bc63694760522e83c4ae5b44663e81418212944c2c0f389399e8ff86141c9115`. This is the user-designated submission package; actual Classroom upload/turn-in is not independently inspected. Preserve this distinction rather than claiming platform confirmation. No lab4-submitted tag is created until original-submission provenance is confirmed.

Before boundary: Desktop directly references Application and Infrastructure; App.axaml.cs composes EF/Npgsql repositories, sessions and database bootstrap. Desktop csproj includes Infrastructure and EF design dependencies. Lab 5 replaces this path with HTTP; existing business services, constraints, migrations and history remain.

Baseline limitations: complete recorded manual Lab 4 acceptance has not been independently reviewed. Lab 4 CI evidence and four UI refresh tests remain revision-specific. Source supports fourteen workflows; no API host, published sixteen-route contract or HTTP-only Desktop exists at this checkpoint.

Lab 5 final revision/tag: pending; lab5-submitted must identify completed work only. Completed boundary: Desktop/App uses DesktopComposition.Build with HTTP-only adapters. Infrastructure/EF Design references were removed in 9779dab; runtime dependencies and native startup without DB settings verified in Session 8. Application/Domain remain for shared types; server owns repositories, migration and transactions. No new schema migration was needed. Architecture and reproduction: architecture.md and README.

Verified integrated code/tools checkpoint: dc1e54186f35600bc8cad15ed8ec6897ccdbb167, CI 43710719990. Review package HEAD is recorded by the generated manifest, not guessed from a document's own future commit. T07 rendered staff evidence and lab5-submitted remain pending; later documentation is not retroactively labeled a completed submission.
