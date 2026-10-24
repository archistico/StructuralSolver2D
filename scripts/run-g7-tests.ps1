# G7 selected validation + full G6 regressions. No certificate of normative completeness.
[CmdletBinding()]
param([switch]$WithM86Smoke)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
    & (Join-Path $PSScriptRoot 'repair-g8b-legacy-examples.ps1')
    & (Join-Path $PSScriptRoot 'clean-project-build.ps1')
    . (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

    Write-Host 'G7 1/5: clean Release solution build; warnings as errors'
    dotnet build StructuralSolver2D.sln -c Release --tl:off -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "G7 BUILD FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'G7 2/5: independent overhang FEM focused tests'
    dotnet test tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --tl:off --filter 'FullyQualifiedName~G7OverhangIndependentEquilibriumTests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "G7 FEM GOLDEN FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'G7 3/5: real G1/G2/G3/G6 integrated validation tests'
    dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj `
        -c Release --no-build --tl:off --filter 'FullyQualifiedName~NtcTimberValidationPipelineG7Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "G7 NTC PIPELINE FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'G7 4/5: all normative verification + catalog tests'
    foreach ($project in @(
        'tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj',
        'tests/StructuralSolver2D.Catalogs.Tests/StructuralSolver2D.Catalogs.Tests.csproj'
    )) {
        dotnet test $project -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "G7 VERIFICATION FAILED ($project). Log: $script:StructuralConsoleLogPath" }
    }

    Write-Host 'G7 5/5: Core/FEM/Reporting/CLI complete regression'
    foreach ($project in @(
        'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj',
        'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj',
        'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj',
        'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj'
    )) {
        dotnet test $project -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "G7 REGRESSION FAILED ($project). Log: $script:StructuralConsoleLogPath" }
    }
    if ($WithM86Smoke) {
        & (Join-Path $PSScriptRoot 'run-m86-release-gate.ps1') -SkipClean
        if ($LASTEXITCODE -ne 0) { throw 'Optional M86 smoke failed.' }
    }
    Write-Host 'G7 GATE PASSED: selected independent FEM benchmarks B13/B14 and real NTC G1/G2/G3 -> G6 audit verified, complete regressions. NOT normative completeness, real uplift/contact proof, or member compliance.' -ForegroundColor Green
    Write-Host "Log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
