"""Package committed Lab 4 source without local credentials or build output."""

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
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = args.output_dir.resolve()
    if output == root or root in output.parents:
        parser.error("Choose an output directory outside the repository.")
    if git(root, "status", "--porcelain"):
        parser.error("Commit or resolve working-tree changes before packaging HEAD.")
    commit = git(root, "rev-parse", "HEAD")
    stem = f"EquipmentBorrowing-Lab4-{commit[:12]}"
    paths = [output / (stem + suffix) for suffix in (".zip", ".json", "-history.txt")]
    if any(path.exists() for path in paths):
        parser.error("This revision already has output files; choose another directory.")
    output.mkdir(parents=True, exist_ok=True)
    archive, manifest, history = paths
    subprocess.run(["git", "-C", str(root), "archive", "--format=zip",
                    f"--output={archive}", commit], check=True)
    with zipfile.ZipFile(archive) as package:
        if package.testzip() is not None:
            raise RuntimeError("ZIP integrity check failed.")
        entries = package.namelist()
        required = ["README.md", "EquipmentBorrowing.slnx",
                    "docs/lab4/submission-handoff.md", "docs/lab4/use-case-suite.md"]
        if any(name not in entries for name in required):
            raise RuntimeError("Required submission files are missing.")
    history.write_text(git(root, "log", "--reverse", "--date=iso-strict",
                           "--format=%H %ad %s", commit) + "\n", encoding="utf-8")
    record = {
        "commit": commit,
        "archive": archive.name,
        "archive_sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
        "archive_bytes": archive.stat().st_size,
        "archive_entries": len(entries),
        "history": history.name,
        "scope": "Committed source and documentation; Git database is not embedded.",
        "acceptance": "Manual Desktop walkthrough/restart and private submission actions remain pending.",
    }
    manifest.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(record, indent=2))
    print(f"Output directory: {output}")


if __name__ == "__main__":
    main()
