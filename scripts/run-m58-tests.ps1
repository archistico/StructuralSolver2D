# M58 release verification: clean build, M57-FIX2 acceptance, M58 tests and CLI exports.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight: remove stale bin/obj for every .NET project before any build/test.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'M58: .NET SDK not found.' }
$required = @(
    'tests/StructuralSolver2D.Cli.Tests/CliCommandRegressionTests.cs',
    'src/StructuralSolver2D.Analysis/PublicApi/StructuralHarmonicAnalysisService.cs',
    'src/StructuralSolver2D.Analysis/Dynamics/Harmonic/HarmonicFrequencyResponseSolver.cs',
    'tests/StructuralSolver2D.Analysis.Tests/Dynamics/M58/HarmonicResponseM58Tests.cs',
    'examples/dynamics/m58-harmonic-cantilever.json')
foreach ($file in $required) {
    if (-not (Test-Path (Join-Path $root $file))) { throw "M58 FULL ZIP missing source: $file" }
}
$previous = Join-Path $root 'tests/StructuralSolver2D.Cli.Tests/CliCommandRegressionTests.cs'
if (-not (Select-String -Path $previous -Pattern 'ModalOnlyExamples_ShouldUseModalCommandsAndRejectStatic' -Quiet)) {
    throw 'M57-FIX2 fixture separation is absent; extract the current FULL ZIP to a clean folder.'
}
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
Push-Location $root
try {
    Write-Host 'M58 1/5: clean and compile Release'
    dotnet build --tl:off StructuralSolver2D.sln --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'M58 build failed.' }

    Write-Host 'M58 2/5: M57 regression gates (not assumed previously passed)'
    dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~ModalReportingM57Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M57-FIX1 reporting tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~ModalOnlyExamples_ShouldUseModalCommandsAndRejectStatic|FullyQualifiedName~DistributedExamples_ArePartitionedWithoutDroppingAnySupportedFixture'
    if ($LASTEXITCODE -ne 0) { throw 'M57-FIX2 CLI tests failed.' }

    Write-Host 'M58 3/5: focused harmonic numerical, CLI and reporting tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~HarmonicResponseM58Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M58 harmonic core tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~HarmonicReportingM58Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M58 reporting tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~HarmonicCliM58Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M58 CLI tests failed.' }

    Write-Host 'M58 4/5: entire solution regression suite'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M58 complete solution regressions failed.' }

    Write-Host 'M58 5/5: CLI harmonic end-to-end exports'
    New-Item -ItemType Directory -Path $temp | Out-Null
    $model = 'examples/dynamics/m58-harmonic-cantilever.json'
    $project = 'src/StructuralSolver2D.Cli'
    $report = Join-Path $temp 'response.md'
    $csv = Join-Path $temp 'response.csv'
    $html = Join-Path $temp 'response.html'
    dotnet run -c Release --no-build --project $project -- harmonic $model
    if ($LASTEXITCODE -ne 0) { throw 'harmonic CLI failed.' }
    dotnet run -c Release --no-build --project $project -- harmonic-report $model $report
    if ($LASTEXITCODE -ne 0) { throw 'harmonic-report CLI failed.' }
    dotnet run -c Release --no-build --project $project -- harmonic-csv $model $csv
    if ($LASTEXITCODE -ne 0) { throw 'harmonic-csv CLI failed.' }
    dotnet run -c Release --no-build --project $project -- harmonic-viewer $model $html
    if ($LASTEXITCODE -ne 0) { throw 'harmonic-viewer CLI failed.' }
    foreach ($path in @($report, $csv, $html)) {
        if (-not (Test-Path $path) -or (Get-Item $path).Length -eq 0) { throw "M58 missing export: $path" }
    }
    if (-not (Select-String -Path $csv -Pattern 'PhaseDegrees' -Quiet)) { throw 'M58 CSV is missing phase values.' }
    if (-not (Select-String -Path $html -Pattern 'id="phase"' -SimpleMatch -Quiet)) { throw 'M58 viewer is missing its phase plot.' }
    if (Get-Command node -ErrorAction SilentlyContinue) {
        node scripts/m58-viewer-syntax-check.js $html
        if ($LASTEXITCODE -ne 0) { throw 'M58 viewer JS syntax check failed.' }
    }
    Write-Host 'M58 automated checks PASSED. Manual browser visualization inspection still required.'
}
finally {
    Pop-Location
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
