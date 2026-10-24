# M87-B: FEM/verification station transfer; no normative equations in this release.
[CmdletBinding()]
param([switch]$WithM86Smoke)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
    & (Join-Path $PSScriptRoot 'clean-project-build.ps1')
    . (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

    Write-Host 'M87-B 1/3: clean Release build; warnings treated as errors'
    dotnet build StructuralSolver2D.sln -c Release --tl:off -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "M87-B build FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M87-B 2/3: verification contracts AND FEM station integration tests'
    dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj `
        -c Release --no-build --tl:off --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "M87-B verification integration FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M87-B 3/3: complete core/analysis/reporting/CLI regression'
    $projects = @(
        'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj',
        'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj',
        'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj',
        'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj'
    )
    foreach ($project in $projects) {
        dotnet test $project -c Release --no-build --tl:off --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "M87-B regression FAILED ($project). Log: $script:StructuralConsoleLogPath" }
    }
    if ($WithM86Smoke) {
        Write-Host 'Optional: repeat M86 smoke suite'
        & (Join-Path $PSScriptRoot 'run-m86-release-gate.ps1') -SkipClean
        if ($LASTEXITCODE -ne 0) { throw 'Optional M86 smoke returned a failure.' }
    }
    Write-Host 'M87-B GATE PASSED: concomitant static FEM station effects integrated.' -ForegroundColor Green
    Write-Host 'WARNING: no steel/timber normative equations; no compliance assessment.'
    Write-Host "Log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
