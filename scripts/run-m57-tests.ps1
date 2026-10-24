# M57 acceptance: assert corrected M56 source, clean build, tests and four modal CLI commands.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight: remove stale bin/obj for every .NET project before any build/test.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 8 SDK not found.'
}
$fixedSource = Join-Path $root 'tests/StructuralSolver2D.Analysis.Tests/Dynamics/ModalBenchmarkM56Tests.cs'
$modalService = Join-Path $root 'src/StructuralSolver2D.Analysis/PublicApi/StructuralModalAnalysisService.cs'
$reportSource = Join-Path $root 'src/StructuralSolver2D.Reporting/Modal/ModalReportExporter.cs'
if (-not (Test-Path $fixedSource) -or -not (Test-Path $modalService)) {
    throw 'M57 source gate failed: extract the FULL M57 ZIP to a NEW folder.'
}
if (-not (Select-String -Path $fixedSource -Pattern 'InterpolatedFlexuralMac' -Quiet)) {
    throw 'M56-FIX1 is missing from this folder. Extract the current FULL ZIP into a clean directory.'
}
if (-not (Test-Path $reportSource) -or -not (Select-String -Path $reportSource -Pattern 'M57-FIX1' -Quiet)) {
    throw 'M57-FIX1 reporting source is missing. Extract the current FULL M57 ZIP into a clean directory.'
}
# M57-FIX2: avoid running the static example regression on modal-only JSON fixtures.
$cliRegression = Join-Path $root 'tests/StructuralSolver2D.Cli.Tests/CliCommandRegressionTests.cs'
if (-not (Test-Path $cliRegression) -or
    -not (Select-String -Path $cliRegression -Pattern 'ModalOnlyExamples_ShouldUseModalCommandsAndRejectStatic' -Quiet)) {
    throw 'M57-FIX2 CLI sample classification is missing. Extract the current FULL M57-FIX2 ZIP into a clean directory.'
}
$temporary = Join-Path ([System.IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
Push-Location $root
try {
    Write-Host 'M57 1/5: clean preflight completed; build Release'
    dotnet build --tl:off StructuralSolver2D.sln --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'M57 Release build failed.' }

    Write-Host 'M57 2/5: M56 analytical convergence (incl. FIX1)'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~ModalBenchmarkM56Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M56 modal benchmarks failed.' }

    Write-Host 'M57 3/5: new M57 analysis and reporting tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~ModalPublicApiM57Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M57 public API tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~ModalReportingM57Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M57 reporting/viewer tests failed.' }

    Write-Host 'M57 4/5: CLI sample classifications and complete regression suite'
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~DistributedExamples_ArePartitionedWithoutDroppingAnySupportedFixture|FullyQualifiedName~DistributedExamples_ShouldAnalyzeAndExportViewer|FullyQualifiedName~ModalOnlyExamples_ShouldUseModalCommandsAndRejectStatic'
    if ($LASTEXITCODE -ne 0) { throw 'M57-FIX2 CLI example classification regression failed.' }
    dotnet test --tl:off StructuralSolver2D.sln --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M57 full regression suite failed.' }

    Write-Host 'M57 5/5: CLI end-to-end, modal-only input file'
    New-Item -ItemType Directory -Path $temporary | Out-Null
    $inputModel = 'examples/dynamics/m57-modal-cantilever.json'
    $commandProject = 'src/StructuralSolver2D.Cli'
    dotnet run --configuration Release --no-build --project $commandProject -- modal $inputModel 3
    if ($LASTEXITCODE -ne 0) { throw 'M57 modal CLI failed.' }
    $md = Join-Path $temporary 'modal.md'
    $html = Join-Path $temporary 'modal.html'
    $csvDir = Join-Path $temporary 'csv'
    dotnet run --configuration Release --no-build --project $commandProject -- modal-report $inputModel $md 3
    if ($LASTEXITCODE -ne 0) { throw 'M57 Markdown CLI failed.' }
    dotnet run --configuration Release --no-build --project $commandProject -- modal-csv $inputModel $csvDir 3
    if ($LASTEXITCODE -ne 0) { throw 'M57 CSV CLI failed.' }
    dotnet run --configuration Release --no-build --project $commandProject -- modal-viewer $inputModel $html 3
    if ($LASTEXITCODE -ne 0) { throw 'M57 viewer CLI failed.' }
    foreach ($file in @($md, $html, (Join-Path $csvDir 'modal-frequencies.csv'), (Join-Path $csvDir 'modal-shapes.csv'))) {
        if (-not (Test-Path $file) -or (Get-Item $file).Length -le 0) {
            throw "M57 output missing or empty: $file"
        }
    }
    if (-not (Select-String -Path $md -Pattern 'Mass: phi^T M phi = 1' -SimpleMatch -Quiet)) {
        throw 'The generated M57 Markdown report omits the mass normalization contract.'
    }
    if (-not (Select-String -Path $html -Pattern 'modal-mode' -Quiet)) {
        throw 'The generated M57 HTML viewer does not contain the modal selector.'
    }
    if (Get-Command node -ErrorAction SilentlyContinue) {
        Write-Host 'Optional M57 JavaScript smoke check (Node.js)'
        node scripts/m57-viewer-smoke.js
        if ($LASTEXITCODE -ne 0) { throw 'M57 modal viewer JavaScript smoke test failed.' }
    }
    else { Write-Host 'Node unavailable; optional modal viewer JavaScript smoke skipped.' }
    Write-Host 'M57 C# and CLI acceptance tests PASSED. Browser display review remains recommended.' 
}
finally {
    Pop-Location
    if (Test-Path $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
