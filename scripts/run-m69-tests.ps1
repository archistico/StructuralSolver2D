# M69: C# / xUnit tests only. Mandatory cleanup BEFORE build, tests and exports.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'M69 requires .NET 8 SDK.' }
    foreach ($relative in @(
        'src/StructuralSolver2D.Reporting/Nonlinear/InteractiveHtmlNonlinearTruss2DViewerExporter.cs',
        'src/StructuralSolver2D.Reporting/Nonlinear/Assets/nonlinear-viewer.js',
        'tests/StructuralSolver2D.Reporting.Tests/Nonlinear/InteractiveNonlinearViewerM69Tests.cs',
        'tests/StructuralSolver2D.Cli.Tests/NonlinearCliM69Tests.cs',
        'docs/structural/fixtures/m68-nonlinear-static.json',
        'docs/structural/fixtures/m68-nonlinear-time.json')) {
        if (-not (Test-Path (Join-Path $root $relative))) {
            throw "M69 file missing: $relative. Use the latest FULL ZIP in a clean directory."
        }
    }
    Write-Host 'M69 1/4: clean Release build'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M69 build failed.' }

    Write-Host 'M69 2/4: dedicated C# viewer and CLI tests'
    dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj -c Release --no-build `
        --filter 'FullyQualifiedName~InteractiveNonlinearViewerM69Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M69 reporting tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj -c Release --no-build `
        --filter 'FullyQualifiedName~NonlinearCliM69Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M69 CLI tests failed.' }

    Write-Host 'M69 3/4: full regression suite'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M69 full regression failed.' }

    Write-Host 'M69 4/4: end-to-end standalone HTML exports'
    $cli = 'src/StructuralSolver2D.Cli/StructuralSolver2D.Cli.csproj'
    $out = Join-Path $env:TEMP ('StructuralSolver2D-M69-' + [guid]::NewGuid().ToString('N'))
    try {
        New-Item -ItemType Directory -Path $out | Out-Null
        foreach ($mode in @('static','time')) {
            $file = Join-Path $out "$mode.html"
            dotnet run --no-build -c Release --project $cli -- "nonlinear-$mode-viewer" "docs/structural/fixtures/m68-nonlinear-$mode.json" $file
            if ($LASTEXITCODE -ne 0) { throw "M69 $mode CLI viewer export failed." }
            $html = Get-Content -LiteralPath $file -Raw -Encoding UTF8
            if ($html -notmatch '"completed":true' -or $html -notmatch 'id="structure"' -or $html -notmatch 'id="plastic-chart"') {
                throw "M69 $mode viewer missing completed result or interactive controls."
            }
        }
    }
    finally { if (Test-Path $out) { Remove-Item -LiteralPath $out -Recurse -Force } }
    Write-Host 'M69 automated checks PASSED. Visual inspection in a real browser still required.'
}
finally { Pop-Location }
