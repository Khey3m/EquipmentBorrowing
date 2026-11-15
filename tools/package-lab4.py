"""Package committed laboratory source with revision and acceptance metadata."""

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile


def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args], text=True).strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--lab", type=int, choices=[4, 5], default=4)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = args.output_dir.resolve()
    if output == root or root in output.parents:
        parser.error("Choose an output directory outside the repository.")
    if git(root, "status", "--porcelain"):
        parser.error("Commit or resolve working-tree changes before packaging HEAD.")
    commit = git(root, "rev-parse", "HEAD")
    stem = f"EquipmentBorrowing-Lab{args.lab}-{commit[:12]}"
    acceptance = "Manual Desktop walkthrough/restart and private submission actions remain pending."
    statuses = {}
    submitted_tag = False
    if args.lab == 5:
        for line in (root / "docs/lab5/test-execution.md").read_text(encoding="utf-8").splitlines():
            if line.startswith("| T0"):
                columns = line.split("|")
                statuses[columns[1].strip()] = columns[3].strip()
        if set(statuses) != {f"T{i:02}" for i in range(1, 9)}:
            parser.error("The consolidated T01-T08 status inventory is incomplete.")
        submitted_tag = "lab5-submitted" in git(root, "tag", "--points-at", commit).splitlines()
        complete = all(value == "Pass" for value in statuses.values()) and submitted_tag
        acceptance = ("Recorded acceptance cases pass and lab5-submitted identifies this revision; private submission actions remain separate."
            if complete else "Review draft: incomplete acceptance and/or lab5-submitted tag remains pending. See docs/lab5/submission-handoff.md.")
    paths = [output / (stem + suffix) for suffix in (".zip", ".json", "-history.txt")]
    if any(path.exists() for path in paths):
        parser.error("This revision already has output files; choose another directory.")
    output.mkdir(parents=True, exist_ok=True)
    archive, manifest, history = paths
    subprocess.run(["git", "-C", str(root), "archive", "--format=zip",
                    f"--output={archive}", commit], check=True)
    history_text = git(root, "log", "--reverse", "--date=iso-strict", "--format=%H %ad %s", commit) + "\n"
    revision = {"commit": commit, "lab": args.lab, "acceptance": acceptance,
                "acceptance_cases": statuses, "lab5_submitted_at_head": submitted_tag}
    if args.lab == 5:
        with zipfile.ZipFile(archive, "a", compression=zipfile.ZIP_DEFLATED) as package:
            package.writestr("submission/git-history.txt", history_text)
            package.writestr("submission/revision.json", json.dumps(revision, indent=2) + "\n")
    with zipfile.ZipFile(archive) as package:
        if package.testzip() is not None:
            raise RuntimeError("ZIP integrity check failed.")
        entries = package.namelist()
        required = ["README.md", "EquipmentBorrowing.slnx",
                    "docs/lab4/submission-handoff.md", "docs/lab4/use-case-suite.md"]
        if args.lab == 5:
            required += ["src/EquipmentBorrowing.Api/Program.cs", "src/EquipmentBorrowing.Http/ApiTransport.cs",
                "docs/lab5/development-report.md", "docs/lab5/architecture.md", "docs/lab5/baseline.md",
                "docs/lab5/traceability.md", "docs/lab5/test-execution.md", "docs/lab5/openapi-current.json",
                "docs/lab5/submission-handoff.md", "docs/lab5/evidence/ci-43710719990.zip",
                "submission/git-history.txt", "submission/revision.json"]
            document = json.loads(package.read("docs/lab5/openapi-current.json"))
            operations = {op.get("operationId") for path in document["paths"].values() for op in path.values() if isinstance(op, dict)}
            if not {f"API-{i:02}" for i in range(1, 17)} <= operations:
                raise RuntimeError("Required sixteen-operation OpenAPI inventory is incomplete.")
        if any(name not in entries for name in required):
            raise RuntimeError("Required submission files are missing.")
    history.write_text(history_text, encoding="utf-8")
    record = {
        "commit": commit,
        "archive": archive.name,
        "archive_sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
        "archive_bytes": archive.stat().st_size,
        "archive_entries": len(entries),
        "history": history.name,
        "scope": "Committed source/documentation; Lab 5 also embeds generated revision/history. Git database is not embedded.",
        "acceptance": acceptance,
        "acceptance_cases": statuses,
        "lab5_submitted_at_head": submitted_tag,
    }
    manifest.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(record, indent=2))
    print(f"Output directory: {output}")


if __name__ == "__main__":
    main()
