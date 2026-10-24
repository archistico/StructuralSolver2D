# M92-B: versioned EC5 timber coefficients and combination-specific provenance.
[CmdletBinding()]
param([switch]$WithM86Smoke)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
    & (Join-Path $PSScriptRoot 'clean-project-build.ps1')
    . (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

    Write-Host 'M92-B 1/4: clean Release build (warnings as errors)'
    dotnet build StructuralSolver2D.sln -c Release --tl:off -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "M92-B build FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M92-B 2/4: all verification tests (including M89-M91, M92-A and M92-B)'
    dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj `
        -c Release --no-build --tl:off --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "M92-B verification FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M92-B 3/4: M88 material and section catalog regression'
    dotnet test tests/StructuralSolver2D.Catalogs.Tests/StructuralSolver2D.Catalogs.Tests.csproj `
        -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "M92-B catalogs FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M92-B 4/4: full Core/FEM/Reporting/CLI regressions'
    foreach ($project in @(
        'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj',
        'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj',
        'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj',
        'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj'
    )) {
        dotnet test $project -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "M92-B regression FAILED ($project). Log: $script:StructuralConsoleLogPath" }
    }
    if ($WithM86Smoke) {
        & (Join-Path $PSScriptRoot 'run-m86-release-gate.ps1') -SkipClean
        if ($LASTEXITCODE -ne 0) { throw 'Optional M86 smoke failed.' }
    }
    Write-Host 'M92-B GATE PASSED: EC5 coefficient selector and M92-A integration tested; NOT national-annex validation or complete member compliance.' -ForegroundColor Green
    Write-Host "Log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
