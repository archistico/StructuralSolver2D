# G5: NTC 2018 timber partial SLE deformation checks; G4/G3/G2/G1/EC5 retained.
[CmdletBinding()]
param([switch]$WithM86Smoke)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
    & (Join-Path $PSScriptRoot 'clean-project-build.ps1')
    . (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

    Write-Host 'G5 1/4: clean Release build (warnings as errors)'
    dotnet build StructuralSolver2D.sln -c Release --tl:off -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "G5 build FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'G5 2/4: verification regression including G5, G3, G2, G1, EC5 and steel M89-M91'
    dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj `
        -c Release --no-build --tl:off --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "G5 verification FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'G5 3/4: M88 material and section catalog regression'
    dotnet test tests/StructuralSolver2D.Catalogs.Tests/StructuralSolver2D.Catalogs.Tests.csproj `
        -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "G5 catalogs FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'G5 4/4: full Core/FEM/Reporting/CLI regressions'
    foreach ($project in @(
        'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj',
        'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj',
        'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj',
        'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj'
    )) {
        dotnet test $project -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "G5 regression FAILED ($project). Log: $script:StructuralConsoleLogPath" }
    }
    if ($WithM86Smoke) {
        & (Join-Path $PSScriptRoot 'run-m86-release-gate.ps1') -SkipClean
        if ($LASTEXITCODE -ne 0) { throw 'Optional M86 smoke failed.' }
    }
    Write-Host 'G5 GATE PASSED: NTC 2018 timber SLE separate rare-variable, rare-total and final deflections with kdef/psi and signed per-case FEM evidence; cumulative G4/G3/G2/G1/EC5 regressions; NOT full SLE envelope, joint verification, vibration assessment, generated combinations or whole-member compliance.' -ForegroundColor Green
    Write-Host "Log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
