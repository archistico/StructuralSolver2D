# M60 clean acceptance: independent tests, full regression, CLI exports.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight: remove stale bin/obj for every .NET project before any build/test.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'M60 requires the .NET 8 SDK.' }
$required = @(
 'src/StructuralSolver2D.Analysis/Dynamics/Seismic/SeismicParticipation.cs',
 'src/StructuralSolver2D.Analysis/PublicApi/StructuralSeismicAnalysisService.cs',
 'src/StructuralSolver2D.Reporting/Seismic/SeismicReportExporter.cs',
 'tests/StructuralSolver2D.Analysis.Tests/Dynamics/M60/SeismicM60Tests.cs',
 'tests/StructuralSolver2D.Reporting.Tests/Seismic/SeismicReportingM60Tests.cs',
 'tests/StructuralSolver2D.Cli.Tests/SeismicCliM60Tests.cs',
 'examples/dynamics/m60-seismic-cantilever.json'
)
foreach ($file in $required) { if (-not (Test-Path (Join-Path $root $file))) { throw "M60 source missing: $file. Extract in a clean directory." } }
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
Push-Location $root
try {
 Write-Host 'M60 1/4: Release build after cleanup'
 dotnet build --tl:off StructuralSolver2D.sln -c Release
 if ($LASTEXITCODE -ne 0) { throw 'M60 build failed.' }
 Write-Host 'M60 2/4: focused seismic tests'
 dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~SeismicM60Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M60 analysis tests failed.' }
 dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~SeismicReportingM60Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M60 report tests failed.' }
 dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~SeismicCliM60Tests'
 if ($LASTEXITCODE -ne 0) { throw 'M60 CLI tests failed.' }
 Write-Host 'M60 3/4: full M1-M59.1 regression'
 dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
 if ($LASTEXITCODE -ne 0) { throw 'M60 full regression failed.' }
 Write-Host 'M60 4/4: end-to-end exported seismic files'
 New-Item -ItemType Directory -Path $temp | Out-Null
 $model = 'examples/dynamics/m60-seismic-cantilever.json'
 $project = 'src/StructuralSolver2D.Cli'
 $time = Join-Path $temp 'seismic-time.md'
 $csv = Join-Path $temp 'seismic-time.csv'
 $spectrum = Join-Path $temp 'seismic-spectrum.md'
 $scsv = Join-Path $temp 'spectral-csv'
 dotnet run -c Release --no-build --project $project -- seismic-time-report $model $time
 if ($LASTEXITCODE -ne 0) { throw 'M60 time Markdown CLI failed.' }
 dotnet run -c Release --no-build --project $project -- seismic-time-csv $model $csv
 if ($LASTEXITCODE -ne 0) { throw 'M60 time CSV CLI failed.' }
 dotnet run -c Release --no-build --project $project -- seismic-spectrum-report $model $spectrum
 if ($LASTEXITCODE -ne 0) { throw 'M60 spectrum Markdown CLI failed.' }
 dotnet run -c Release --no-build --project $project -- seismic-spectrum-csv $model $scsv
 if ($LASTEXITCODE -ne 0) { throw 'M60 spectrum CSV CLI failed.' }
 foreach ($file in @($time, $csv, $spectrum, (Join-Path $scsv 'seismic-modes.csv'), (Join-Path $scsv 'seismic-nodes.csv'))) {
   if (-not (Test-Path $file) -or (Get-Item $file).Length -eq 0) { throw "M60 output missing or empty: $file" }
 }
 if (-not (Select-String -Path $spectrum -Pattern 'NON-NORMATIVE' -SimpleMatch -Quiet)) { throw 'M60 safety disclaimer missing.' }
 if (-not (Select-String -Path $csv -Pattern 'AbsoluteAy_ms2' -SimpleMatch -Quiet)) { throw 'M60 acceleration units missing.' }
 Write-Host 'M60 automated verification PASSED. Review time-step sensitivity, spectral input provenance, mass participation and browser viewer separately.'
}
finally {
 Pop-Location
 if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
