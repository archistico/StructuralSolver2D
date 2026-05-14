# Release checklist

This checklist is intended for the first technical releases of StructuralSolver2D.

The project is not a certified structural design product. A release only means that the current educational and experimental solver baseline is coherent, documented and testable.

---

## 1. Code and tests

Run from the repository root:

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln
dotnet test StructuralSolver2D.sln
```

The release should not be tagged if the solution does not build or if tests fail.

Recommended optional checks:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- help
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
```

---

## 2. Documentation

Before tagging a release, check that these files are aligned:

```text
README.md
CHANGELOG.md
VERSION
ai-handoff.md
docs/structural/roadmap.md
docs/structural/public-api.md
docs/structural/reporting.md
docs/structural/csv-export.md
```

The documentation must state the same current milestone and the same next recommended milestone.

---

## 3. Scope wording

Every release note must keep the scope explicit:

```text
StructuralSolver2D is an experimental and educational 2D linear elastic structural analysis engine.
```

Do not describe it as:

```text
certified
production-ready for real structural design
Eurocode-compliant
NTC-compliant
suitable for construction decisions without independent verification
```

---

## 4. Suggested Git commands

After applying the release patch and verifying tests:

```powershell
git status
git add .
git commit -m "Prepare v0.1.0 technical release"
git tag -a v0.1.0 -m "StructuralSolver2D v0.1.0 technical release"
git push
git push origin v0.1.0
```

If the remote branch is not `main`, adapt the push command to the active branch.

---

## 5. Suggested GitHub release title

```text
StructuralSolver2D v0.1.0 - First technical release
```

Suggested release description:

```text
First technical release of StructuralSolver2D, an experimental .NET 8 engine for educational 2D linear elastic structural analysis.

This snapshot consolidates the first public API facade, benchmark-backed analysis, Markdown reporting, CSV export, parametric sections, material presets and preliminary deflection checks.

This is not a certified structural design tool. Results must be independently verified before any real engineering use.
```
