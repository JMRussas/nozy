#!/usr/bin/env python3
#  YesZ - Developer CLI
#
#  Inspect fork divergence from upstream NoZ, roadmap status, and test counts.
#  Read-only — never mutates state.
#
#  Depends on: engine/noz/ (git submodule), .claude/roadmap.md, .claude/maintenance.md, yesz.slnx
#  Used by:    developers, AI agents

import argparse
import re
import subprocess
import sys
from pathlib import Path

if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

PROJECT_ROOT = Path(__file__).parent.parent
NOZ_DIR = PROJECT_ROOT / "engine" / "noz"
ROADMAP_MD = PROJECT_ROOT / ".claude" / "roadmap.md"
MAINTENANCE_MD = PROJECT_ROOT / ".claude" / "maintenance.md"
SLNX_FILE = PROJECT_ROOT / "yesz.slnx"
SLN_FILE = PROJECT_ROOT / "yesz.sln"


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def pass_fail(label: str, ok: bool, detail: str = "") -> None:
    mark = "✓ PASS" if ok else "✗ FAIL"
    suffix = f"  ({detail})" if detail else ""
    print(f"  {mark}  {label}{suffix}")


# ---------------------------------------------------------------------------
# check-fork-divergence
# ---------------------------------------------------------------------------

def cmd_check_fork_divergence(args) -> None:
    """Show commits in the local noz fork that are not yet in upstream/main."""
    print("\n=== Fork Divergence from upstream/main ===\n")

    if not NOZ_DIR.exists():
        print(f"  ✗ FAIL  engine/noz not found at {NOZ_DIR}")
        print("  Is the submodule initialized? Run: git submodule update --init")
        print()
        return

    # Check if upstream remote exists
    result = subprocess.run(
        ["git", "-C", str(NOZ_DIR), "remote"],
        capture_output=True, text=True,
    )
    remotes = result.stdout.strip().splitlines()
    if "upstream" not in remotes:
        print("  upstream remote not configured in engine/noz")
        print(f"  Available remotes: {', '.join(remotes) if remotes else '(none)'}")
        print()
        print("  To add upstream:")
        print("    git -C engine/noz remote add upstream https://github.com/nozgames/noz-cs")
        print("    git -C engine/noz fetch upstream")
        print()
        return

    # Fetch upstream quietly (don't fail if offline)
    subprocess.run(
        ["git", "-C", str(NOZ_DIR), "fetch", "upstream", "--quiet"],
        capture_output=True,
    )

    # Show local commits not in upstream/main
    result = subprocess.run(
        ["git", "-C", str(NOZ_DIR), "log", "--oneline", "upstream/main..HEAD"],
        capture_output=True, text=True,
    )
    if result.returncode != 0:
        print(f"  ✗ FAIL  git log failed: {result.stderr.strip()}")
        print()
        return

    lines = result.stdout.strip().splitlines()
    if not lines:
        print("  No local commits ahead of upstream/main — fork is in sync.")
    else:
        print(f"  {len(lines)} commit(s) ahead of upstream/main:\n")
        for line in lines:
            print(f"    {line}")
    print()


# ---------------------------------------------------------------------------
# list-fork-changes
# ---------------------------------------------------------------------------

def cmd_list_fork_changes(args) -> None:
    """Read maintenance.md and extract the Fork Changes Log table."""
    print("\n=== Fork Changes Log (from .claude/maintenance.md) ===\n")

    if not MAINTENANCE_MD.exists():
        print(f"  ✗ FAIL  maintenance.md not found at {MAINTENANCE_MD}")
        print()
        return

    # Read in chunks since the file is large
    content = MAINTENANCE_MD.read_text(encoding="utf-8")

    # Find the "Fork Changes Log" section and its table
    # The table has columns: Date | File | Change | Rationale
    section_m = re.search(r'## Fork Changes Log\n', content)
    if not section_m:
        print("  'Fork Changes Log' section not found in maintenance.md")
        print()
        return

    section_text = content[section_m.end():]

    # Find the next ## section to limit our search
    next_section_m = re.search(r'\n## ', section_text)
    if next_section_m:
        section_text = section_text[:next_section_m.start()]

    # Extract markdown table rows (pipe-separated)
    # Skip header row and separator row
    table_rows = []
    for line in section_text.splitlines():
        line = line.strip()
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.split("|")[1:-1]]
        if not cells:
            continue
        # Skip header (Date | File | ...) and separator (---|---|...)
        if cells[0].startswith("---") or cells[0].lower() == "date":
            continue
        # Skip blank entries
        if all(c in ("", "—", "(none yet)") for c in cells):
            continue
        table_rows.append(cells)

    if not table_rows:
        print("  No fork change entries found.")
        print()
        return

    print(f"  {len(table_rows)} fork change(s) logged:\n")
    for row in table_rows:
        date = row[0] if len(row) > 0 else "?"
        file_ = row[1] if len(row) > 1 else "?"
        change = row[2] if len(row) > 2 else "?"
        rationale = row[3] if len(row) > 3 else ""
        print(f"  [{date}] {file_}")
        print(f"    Change:    {change}")
        if rationale:
            print(f"    Rationale: {rationale}")
        print()


# ---------------------------------------------------------------------------
# validate-phase-status
# ---------------------------------------------------------------------------

def cmd_validate_phase_status(args) -> None:
    """Parse roadmap.md phase table and print status of each phase."""
    print("\n=== Phase Status (from .claude/roadmap.md) ===\n")

    if not ROADMAP_MD.exists():
        print(f"  ✗ FAIL  roadmap.md not found at {ROADMAP_MD}")
        print()
        return

    content = ROADMAP_MD.read_text(encoding="utf-8")

    # Find the phase overview table
    # Columns: Phase | Name | Milestone | Depends on | Status
    phases = []
    in_table = False
    for line in content.splitlines():
        stripped = line.strip()
        if not stripped.startswith("|"):
            if in_table:
                break  # end of table
            continue
        cells = [c.strip() for c in stripped.split("|")[1:-1]]
        if not cells:
            continue
        if cells[0].lower() == "phase" or cells[0].startswith("---"):
            in_table = True
            continue
        in_table = True
        if len(cells) >= 5:
            phase_id = cells[0]
            name = cells[1]
            status_raw = cells[4].strip("*").strip()
            phases.append((phase_id, name, status_raw))

    if not phases:
        print("  No phase entries found in roadmap.md")
        print()
        return

    # Bucket by status
    done = [(p, n, s) for p, n, s in phases if "done" in s.lower()]
    in_progress = [(p, n, s) for p, n, s in phases if "in progress" in s.lower() or "in-progress" in s.lower()]
    not_started = [(p, n, s) for p, n, s in phases if "not started" in s.lower()]
    other = [(p, n, s) for p, n, s in phases if (p, n, s) not in done + in_progress + not_started]

    print(f"  Total phases: {len(phases)}")
    print(f"  Done:         {len(done)}")
    print(f"  In progress:  {len(in_progress)}")
    print(f"  Not started:  {len(not_started)}")
    if other:
        print(f"  Other:        {len(other)}")
    print()

    if in_progress:
        print("  In Progress:")
        for p, n, s in in_progress:
            print(f"    Phase {p}: {n}  [{s}]")
        print()

    if not_started:
        print("  Not Started:")
        for p, n, s in not_started:
            print(f"    Phase {p}: {n}")
        print()

    if done:
        print("  Done:")
        for p, n, s in done:
            print(f"    Phase {p}: {n}")
        print()


# ---------------------------------------------------------------------------
# check-test-count
# ---------------------------------------------------------------------------

def cmd_check_test_count(args) -> None:
    """Run dotnet test and report pass/fail counts."""
    print("\n=== Test Count ===\n")

    # Find solution file
    sln = None
    for candidate in (SLNX_FILE, SLN_FILE):
        if candidate.exists():
            sln = candidate
            break

    if sln is None:
        print("  ✗ FAIL  No .slnx or .sln file found in project root")
        print()
        return

    print(f"  Running: dotnet test {sln.name} --no-build --verbosity minimal")
    print()

    result = subprocess.run(
        ["dotnet", "test", str(sln), "--no-build", "--verbosity", "minimal"],
        capture_output=True, text=True, cwd=str(PROJECT_ROOT),
    )

    output = result.stdout + result.stderr

    # Parse counts from output like:
    # "Passed! - Failed: 0, Passed: 42, Skipped: 0, Total: 42"
    # or: "Failed! - Failed: 2, Passed: 40, Skipped: 0, Total: 42"
    passed = failed = skipped = total = None
    for line in output.splitlines():
        m = re.search(
            r'Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)',
            line,
        )
        if m:
            failed = int(m.group(1))
            passed = int(m.group(2))
            skipped = int(m.group(3))
            total = int(m.group(4))
            break

    if total is not None:
        ok = failed == 0
        pass_fail(f"Tests", ok, f"{passed}/{total} passed, {failed} failed, {skipped} skipped")
    else:
        # Couldn't parse — print raw output summary
        print("  Could not parse test result counts. Raw output:")
        for line in output.splitlines()[-20:]:
            print(f"    {line}")
        if result.returncode != 0:
            print(f"\n  Exit code: {result.returncode}")
    print()


# ---------------------------------------------------------------------------
# verify-bindings
# ---------------------------------------------------------------------------

def cmd_verify_bindings(args) -> None:
    """Scan .csproj and AssemblyInfo.cs files for InternalsVisibleTo attributes."""
    print("\n=== InternalsVisibleTo Bindings ===\n")

    # Collect all .csproj and AssemblyInfo.cs files under the project root,
    # excluding the engine/noz submodule (checked separately below).
    yesz_files = []
    for pattern in ("**/*.csproj", "**/AssemblyInfo.cs"):
        for path in PROJECT_ROOT.glob(pattern):
            # Skip the submodule — scanned separately
            try:
                path.relative_to(NOZ_DIR)
                continue
            except ValueError:
                pass
            yesz_files.append(path)

    noz_files = []
    if NOZ_DIR.exists():
        for pattern in ("**/*.csproj", "**/AssemblyInfo.cs"):
            noz_files.append(list(NOZ_DIR.glob(pattern)))
        noz_files = [p for group in noz_files for p in group]

    ivt_pattern = re.compile(
        r'InternalsVisibleTo\s*(?:Include\s*=\s*"([^"]+)"|(?:\()?"([^"]+)")',
        re.IGNORECASE,
    )

    def scan_files(file_list):
        found = []
        for path in file_list:
            try:
                text = path.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            for m in ivt_pattern.finditer(text):
                assembly = m.group(1) or m.group(2)
                rel = path.relative_to(PROJECT_ROOT)
                found.append((str(rel).replace("\\", "/"), assembly))
        return found

    yesz_ivt = scan_files(yesz_files)
    noz_ivt = scan_files(noz_files)

    all_ivt = yesz_ivt + noz_ivt

    if not all_ivt:
        print("  No InternalsVisibleTo attributes found in any .csproj or AssemblyInfo.cs")
        print()
        return

    print(f"  Found {len(all_ivt)} InternalsVisibleTo declaration(s):\n")
    for file_rel, assembly in all_ivt:
        print(f"    {file_rel}")
        print(f"      -> grants access to: {assembly}")
        print()

    # Summary check: does any YesZ project grant access to the test projects?
    yesz_assemblies = {a for _, a in yesz_ivt}
    test_assemblies = {a for a in yesz_assemblies if "test" in a.lower()}
    noz_assemblies = {a for a in yesz_assemblies if "noz" in a.lower()}

    print("  Summary:")
    pass_fail("YesZ grants tests internal access", bool(test_assemblies),
              ", ".join(sorted(test_assemblies)) if test_assemblies else "none found")
    pass_fail("YesZ grants NoZ internal access", bool(noz_assemblies),
              ", ".join(sorted(noz_assemblies)) if noz_assemblies else "none found")
    if noz_ivt:
        noz_grants_yesz = {a for _, a in noz_ivt if "yesz" in a.lower()}
        pass_fail("NoZ grants YesZ internal access", bool(noz_grants_yesz),
                  ", ".join(sorted(noz_grants_yesz)) if noz_grants_yesz else "none found")
    print()


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="YesZ Developer CLI",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Commands:
  check-fork-divergence   Show local noz fork commits not in upstream/main
  list-fork-changes       Print the fork change log from maintenance.md
  validate-phase-status   Show roadmap phase completion status
  check-test-count        Run dotnet test and report pass/fail counts
  verify-bindings         Check InternalsVisibleTo declarations in .csproj files
""",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("check-fork-divergence",
                   help="Show local noz commits not in upstream/main")
    sub.add_parser("list-fork-changes",
                   help="Print fork changes log from .claude/maintenance.md")
    sub.add_parser("validate-phase-status",
                   help="Show done/in-progress/not-started phases from roadmap.md")
    sub.add_parser("check-test-count",
                   help="Run dotnet test and report pass/fail counts")
    sub.add_parser("verify-bindings",
                   help="Check InternalsVisibleTo declarations in .csproj and AssemblyInfo.cs")

    args = parser.parse_args()

    commands = {
        "check-fork-divergence": cmd_check_fork_divergence,
        "list-fork-changes":     cmd_list_fork_changes,
        "validate-phase-status": cmd_validate_phase_status,
        "check-test-count":      cmd_check_test_count,
        "verify-bindings":       cmd_verify_bindings,
    }
    commands[args.command](args)


if __name__ == "__main__":
    main()
