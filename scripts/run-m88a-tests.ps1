# M88-A: versioned elastic-only steel and timber catalog over existing M80 geometry.
[CmdletBinding()]
param([switch]$WithM86Smoke)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
    & (Join-Path $PSScriptRoot 'clean-project-build.ps1')
    . (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

    Write-Host 'M88-A 1/4: clean Release build; warnings treated as errors'
    dotnet build StructuralSolver2D.sln -c Release --tl:off -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "M87-C build FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M88-A 2/4: complete Verification tests (M87 A, B and C)'
    dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj `
        -c Release --no-build --tl:off --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "M87-C verification tests FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M88-A 3/4: catalog validation and JSON tests'
    dotnet test tests/StructuralSolver2D.Catalogs.Tests/StructuralSolver2D.Catalogs.Tests.csproj `
        -c Release --no-build --tl:off --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "M88-A catalog tests FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M88-A 4/4: full FEM, reporting and CLI regression'
    $projects = @(
        'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj',
        'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj',
        'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj',
        'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj'
    )
    foreach ($project in $projects) {
        dotnet test $project -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "M87-C regression FAILED ($project). Log: $script:StructuralConsoleLogPath" }
    }
    if ($WithM86Smoke) {
        & (Join-Path $PSScriptRoot 'run-m86-release-gate.ps1') -SkipClean
        if ($LASTEXITCODE -ne 0) { throw 'Optional M86 smoke failed.' }
    }
    Write-Host 'M88-A GATE PASSED: catalog + all regressions; NO normative approval implied.' -ForegroundColor Green
    Write-Host "Log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
