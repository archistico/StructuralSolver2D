# M70 CQC modal combination: C# / xUnit only; clean outputs BEFORE every other action.
# --tl:off uses a non-interactive, line-oriented MSBuild console logger (avoids redraw/mixed rows).
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'M70 requires .NET 8 SDK.' }
    foreach ($relative in @(
        'src/StructuralSolver2D.Analysis/Dynamics/Seismic/EqualDampingCqc.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Dynamics/M70/SeismicCqcM70Tests.cs',
        'tests/StructuralSolver2D.Reporting.Tests/Seismic/SeismicReportingM70Tests.cs',
        'tests/StructuralSolver2D.Cli.Tests/SeismicCliM70Tests.cs',
        'examples/dynamics/m70-seismic-cqc-cantilever.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative))) {
            throw "M70 file missing: $relative. Extract the newest FULL ZIP to a clean directory."
        }
    }
    Write-Host 'M70 1/4: clean Release build'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M70 build failed.' }

    Write-Host 'M70 2/4: focused C# numerical, reporting and CLI tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build `
        --filter 'FullyQualifiedName~SeismicCqcM70Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M70 numerical tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj -c Release --no-build `
        --filter 'FullyQualifiedName~SeismicReportingM70Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M70 report tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj -c Release --no-build `
        --filter 'FullyQualifiedName~SeismicCliM70Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M70 CLI tests failed.' }

    Write-Host 'M70 3/4: complete regression suite (including M60 SRSS and M69)'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M70 complete regression failed.' }

    Write-Host 'M70 4/4: real CLI export validation with both SRSS and CQC'
    $cli = 'src/StructuralSolver2D.Cli/StructuralSolver2D.Cli.csproj'
    $outputDir = Join-Path $env:TEMP ('StructuralSolver2D-M70-' + [guid]::NewGuid().ToString('N'))
    try {
        New-Item -Path $outputDir -ItemType Directory | Out-Null
        foreach ($item in @(
            @{ Label = 'CQC'; Input = 'examples/dynamics/m70-seismic-cqc-cantilever.json' },
            @{ Label = 'SRSS'; Input = 'examples/dynamics/m60-seismic-cantilever.json' }
        )) {
            $target = Join-Path $outputDir $item.Label
            dotnet run --no-build -c Release --project $cli -- seismic-spectrum-csv $item.Input $target
            if ($LASTEXITCODE -ne 0) { throw "M70 $($item.Label) CLI CSV export failed." }
            $header = (Get-Content -LiteralPath (Join-Path $target 'seismic-nodes.csv') -TotalCount 1)
            if ($header -notmatch "$($item.Label)AbsUx_m") {
                throw "M70 $($item.Label) CSV header missing method distinction: $header"
            }
            $report = Join-Path $outputDir "$($item.Label).md"
            dotnet run --no-build -c Release --project $cli -- seismic-spectrum-report $item.Input $report
            if ($LASTEXITCODE -ne 0) { throw "M70 $($item.Label) Markdown export failed." }
            $markdown = Get-Content -LiteralPath $report -Raw -Encoding UTF8
            if ($markdown -notmatch 'NON-NORMATIVE' -or $markdown -notmatch $item.Label) {
                throw "M70 $($item.Label) missing method or non-normative warning."
            }
        }
    }
    finally { if (Test-Path -LiteralPath $outputDir) { Remove-Item -LiteralPath $outputDir -Recurse -Force } }
    Write-Host 'M70 automated checks PASSED. Non-normative output only; no code-based safety certification.'
}
finally { Pop-Location }
