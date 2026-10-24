# M87-A: isolated verification architecture, no normative equations.
# From StructuralSolver2D/: .\scripts\run-m87-tests.ps1 [-WithM86Smoke]
[CmdletBinding()]
param([switch]$WithM86Smoke)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
    & (Join-Path $PSScriptRoot 'clean-project-build.ps1')
    . (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

    Write-Host 'M87-A 1/3: clean Release build, warnings as errors'
    dotnet build StructuralSolver2D.sln -c Release --tl:off -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "M87-A build FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M87-A 2/3: isolated verification contract/fail-closed xUnit'
    dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj `
        -c Release --no-build --tl:off --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "M87-A verification tests FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M87-A 3/3: complete regression'
    $projects = @(
        'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj',
        'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj',
        'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj',
        'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj'
    )
    foreach ($project in $projects) {
        dotnet test $project -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "M87-A regression FAILED ($project). Log: $script:StructuralConsoleLogPath" }
    }
    if ($WithM86Smoke) {
        Write-Host 'Additional end-to-end M86 smoke test'
        & (Join-Path $PSScriptRoot 'run-m86-release-gate.ps1') -SkipClean
        if ($LASTEXITCODE -ne 0) { throw 'M86 smoke returned a failure.' }
    }
    Write-Host 'M87-A GATE PASSED: verification architecture + static FEM regression.' -ForegroundColor Green
    Write-Host 'No EC3/EC5/NTC normative formula or certification has been implemented.'
    Write-Host "Log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
