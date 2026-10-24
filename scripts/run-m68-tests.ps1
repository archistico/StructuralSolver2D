# M68 — ALL tests and benchmarks C#/.NET. Mandatory first operation: purge per-project bin/obj.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'M68 requires .NET 8 SDK.' }
    foreach ($relative in @(
        'src/StructuralSolver2D.Analysis/PublicApi/StructuralNonlinearTruss2DContracts.cs',
        'src/StructuralSolver2D.Analysis/PublicApi/StructuralNonlinearTruss2DService.cs',
        'src/StructuralSolver2D.Reporting/Nonlinear/NonlinearTruss2DReportExporter.cs',
        'src/StructuralSolver2D.Cli/Nonlinear/NonlinearTruss2DCliM68.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Truss2D/NonlinearPublicApiM68Tests.cs',
        'tests/StructuralSolver2D.Cli.Tests/NonlinearCliM68Tests.cs',
        'docs/structural/fixtures/m68-nonlinear-static.json',
        'docs/structural/fixtures/m68-nonlinear-time.json')) {
        if (-not (Test-Path (Join-Path $root $relative))) {
            throw "M68 file missing: $relative. Unpack the FULL ZIP in a clean directory."
        }
    }
    Write-Host 'M68 1/4: full Release build after bin/obj cleanup'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M68 Release build failed.' }

    Write-Host 'M68 2/4: C# API, CLI and regression tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~NonlinearPublicApiM68Tests|FullyQualifiedName~NonlinearValidationM66Tests|FullyQualifiedName~DynamicForceRecoveryM67Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M68 Analysis tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~NonlinearCliM68Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M68 CLI tests failed.' }

    Write-Host 'M68 3/4: full regression'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M68 full regression failed.' }

    Write-Host 'M68 4/4: end-to-end example exports'
    $cli = 'src/StructuralSolver2D.Cli/StructuralSolver2D.Cli.csproj'
    $out = Join-Path $env:TEMP ('StructuralSolver2D-M68-' + [guid]::NewGuid().ToString('N'))
    try {
        New-Item -ItemType Directory -Path $out | Out-Null
        foreach ($mode in @('static','time')) {
            $source = "docs/structural/fixtures/m68-nonlinear-$mode.json"
            $md = Join-Path $out "$mode.md"
            $csvDir = Join-Path $out "$mode-csv"
            dotnet run --no-build -c Release --project $cli -- "nonlinear-$mode-report" $source $md
            if ($LASTEXITCODE -ne 0) { throw "M68 $mode markdown command failed." }
            dotnet run --no-build -c Release --project $cli -- "nonlinear-$mode-csv" $source $csvDir
            if ($LASTEXITCODE -ne 0) { throw "M68 $mode CSV command failed." }
            if (-not (Select-String -Path $md -Pattern 'Analysis completed:\*\* YES' -Quiet)) {
                throw "M68 $mode report did not confirm completion."
            }
            foreach ($file in @('nonlinear-nodes.csv','nonlinear-members.csv','nonlinear-status.md')) {
                if (-not (Test-Path (Join-Path $csvDir $file))) { throw "M68 export missing: $file" }
            }
        }
    }
    finally { if (Test-Path $out) { Remove-Item -LiteralPath $out -Recurse -Force } }
    Write-Host 'M68 C#/.NET automated checks PASSED. Research-only nonlinear Truss2D; real-browser QA remains M69.'
}
finally { Pop-Location }
