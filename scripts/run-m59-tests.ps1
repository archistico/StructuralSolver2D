# M59 clean Windows validation: build, all predecessors, new transient tests, CLI exports.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight: remove stale bin/obj for every .NET project before any build/test.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'M59: .NET SDK not available.' }
$required = @(
 'src/StructuralSolver2D.Analysis/PublicApi/StructuralTimeHistoryAnalysisService.cs',
 'src/StructuralSolver2D.Analysis/Dynamics/Transient/NewmarkAverageAcceleration.cs',
 'src/StructuralSolver2D.Reporting/Transient/InteractiveHtmlTimeHistoryViewerExporter.cs',
 'tests/StructuralSolver2D.Analysis.Tests/Dynamics/M59/TimeHistoryM59Tests.cs',
 'examples/dynamics/m59-time-cantilever.json')
foreach ($file in $required) { if (-not (Test-Path (Join-Path $root $file))) { throw "M59 source missing: $file. Extract the complete release to a CLEAN folder." } }
if (-not (Select-String -Path (Join-Path $root 'src/StructuralSolver2D.Cli/Program.cs') -Pattern 'RunTimeCommand' -Quiet)) {
 throw 'M59 CLI commands missing: remove prior folders and extract the current ZIP.'
}
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
Push-Location $root
try {
 Write-Host 'M59 1/5: Release build after cleanup'
 dotnet build --tl:off StructuralSolver2D.sln -c Release
 if ($LASTEXITCODE -ne 0) { throw 'M59 build failed.' }
 Write-Host 'M59 2/5: M57/M58 focused predecessor regressions'
 dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~HarmonicResponseM58Tests|FullyQualifiedName~ModalBenchmarkM56Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M56/M58 predecessor numeric tests failed.' }
 dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~ModalOnlyExamples_ShouldUseModalCommandsAndRejectStatic|FullyQualifiedName~HarmonicCliM58Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M57/M58 predecessor CLI tests failed.' }
 Write-Host 'M59 3/5: focused time-domain numeric, reporting and CLI tests'
 dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~TimeHistoryM59Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M59 time-domain numeric tests failed.' }
 dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~TimeHistoryReportingM59Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M59 reporting tests failed.' }
 dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~TimeHistoryCliM59Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M59 CLI tests failed.' }
 Write-Host 'M59 4/5: complete solution regression suite'
 dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
 if ($LASTEXITCODE -ne 0) { throw 'M59 full regression failed.' }
 Write-Host 'M59 5/5: CLI end-to-end time exports and JS check'
 New-Item -ItemType Directory -Path $temp | Out-Null
 $model='examples/dynamics/m59-time-cantilever.json'
 $project='src/StructuralSolver2D.Cli'
 $report=Join-Path $temp 'time.md'
 $csv=Join-Path $temp 'csv'
 $html=Join-Path $temp 'time.html'
 dotnet run -c Release --no-build --project $project -- time $model
 if ($LASTEXITCODE -ne 0) { throw 'time CLI failed.' }
 dotnet run -c Release --no-build --project $project -- time-report $model $report
 if ($LASTEXITCODE -ne 0) { throw 'time-report CLI failed.' }
 dotnet run -c Release --no-build --project $project -- time-csv $model $csv
 if ($LASTEXITCODE -ne 0) { throw 'time-csv CLI failed.' }
 dotnet run -c Release --no-build --project $project -- time-viewer $model $html
 if ($LASTEXITCODE -ne 0) { throw 'time-viewer CLI failed.' }
 foreach ($p in @($report,$html,(Join-Path $csv 'time-nodes.csv'),(Join-Path $csv 'time-energy.csv'))) {
  if (-not (Test-Path $p) -or (Get-Item $p).Length -eq 0) { throw "Missing M59 export: $p" }
 }
 if (-not (Select-String -Path (Join-Path $csv 'time-energy.csv') -Pattern 'BalanceResidual_kNm' -Quiet)) { throw 'Energy residual not exported.' }
 if (Get-Command node -ErrorAction SilentlyContinue) {
  node scripts/m59-viewer-syntax-check.js $html
  if ($LASTEXITCODE -ne 0) { throw 'M59 HTML viewer JavaScript failed.' }
 }
 Write-Host 'M59 automated verification PASSED. Visual browser inspection and timestep convergence review remain necessary.'
}
finally {
 Pop-Location
 if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
