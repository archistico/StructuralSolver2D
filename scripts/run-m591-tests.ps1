# M59.1: validates the new time-step convergence analyzer and all earlier functionality.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight: remove stale bin/obj for every .NET project before any build/test.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'M59.1 requires the .NET 8 SDK.' }
$required = @(
 'src/StructuralSolver2D.Analysis/PublicApi/StructuralTimeConvergenceAnalysisService.cs',
 'src/StructuralSolver2D.Reporting/Transient/TimeConvergenceReportExporter.cs',
 'tests/StructuralSolver2D.Analysis.Tests/Dynamics/M591/TimeConvergenceM591Tests.cs',
 'tests/StructuralSolver2D.Reporting.Tests/Transient/TimeConvergenceReportingM591Tests.cs',
 'tests/StructuralSolver2D.Cli.Tests/TimeConvergenceCliM591Tests.cs',
 'examples/dynamics/m59-time-cantilever.json'
)
foreach ($file in $required) { if (-not (Test-Path (Join-Path $root $file))) { throw "M59.1 file missing: $file. Extract archive into a clean directory." } }
if (-not (Select-String -Path (Join-Path $root 'src/StructuralSolver2D.Cli/Program.cs') -Pattern 'RunTimeConvergenceCommand' -Quiet)) {
 throw 'M59.1 CLI entry point missing; remove any old source tree and extract the new ZIP.'
}
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
Push-Location $root
try {
 Write-Host 'M59.1 1/4: Release build after cleanup'
 dotnet build --tl:off StructuralSolver2D.sln -c Release
 if ($LASTEXITCODE -ne 0) { throw 'M59.1 build failed.' }
 Write-Host 'M59.1 2/4: focused new analysis, reporting and CLI tests'
 dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~TimeConvergenceM591Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M59.1 analysis tests failed.' }
 dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~TimeConvergenceReportingM591Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M59.1 report tests failed.' }
 dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~TimeConvergenceCliM591Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M59.1 CLI tests failed.' }
 Write-Host 'M59.1 3/4: full regression (including M51..M59)'
 dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
 if ($LASTEXITCODE -ne 0) { throw 'M59.1 full regression failed.' }
 Write-Host 'M59.1 4/4: export convergence report and CSV from distributed time-history example'
 New-Item -ItemType Directory -Path $temp | Out-Null
 $model='examples/dynamics/m59-time-cantilever.json'
 $project='src/StructuralSolver2D.Cli'
 $report=Join-Path $temp 'convergence.md'
 $csv=Join-Path $temp 'convergence.csv'
 dotnet run -c Release --no-build --project $project -- time-convergence-report $model $report
 if ($LASTEXITCODE -ne 0) { throw 'M59.1 Markdown export failed.' }
 dotnet run -c Release --no-build --project $project -- time-convergence-csv $model $csv
 if ($LASTEXITCODE -ne 0) { throw 'M59.1 CSV export failed.' }
 if (-not (Select-String -LiteralPath $report -Pattern 'Natural-frequency resolution' -Quiet)) { throw 'M59.1 frequency diagnostics missing.' }
 if (-not (Select-String -LiteralPath $report -Pattern 'dt/4' -SimpleMatch -Quiet)) { throw 'M59.1 finest-grid description missing.' }
 if (-not (Select-String -LiteralPath $csv -Pattern 'MediumFineRelative' -Quiet)) { throw 'M59.1 comparison CSV missing.' }
 Write-Host 'M59.1 automated checks PASSED. Review timestep convergence and M591_FINE_RESOLUTION warnings before using the response.'
}
finally {
 Pop-Location
 if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
