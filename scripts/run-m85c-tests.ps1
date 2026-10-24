# M85-C final static-elastic numerical reference gate. Run from project root:
# .\scripts\run-m85c-tests.ps1
# No benchmark timing threshold, no solver or production-code modification.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M85-C 1/4: clean Release build'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw "M85-C build FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M85-C 2/4: independent references and robustness (8 xUnit cases)'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~FinalReferenceAndRobustnessM85CTests' `
        --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "M85-C numerical reference gate FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M85-C 3/4: M85-A/M82/M83 and M85-B targeted regression'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~IndependentStaticInvariantsM85Tests|FullyQualifiedName~EdgeCaseValidationTests|FullyQualifiedName~Frame2DGlobalSecondOrderM82Tests|FullyQualifiedName~Frame2DScalabilityM83Tests' `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "M85-C previous numerical gates FAILED. Log: $script:StructuralConsoleLogPath" }

    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~IntegratedResultsM85BTests' `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "M85-C M85-B pipeline gate FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M85-C 4/4: complete regression (Core / Analysis / Reporting / Cli)'
    $projects = @(
        'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj',
        'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj',
        'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj',
        'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj'
    )
    $failed = @()
    foreach ($project in $projects) {
        Write-Host "Regression: $project"
        dotnet test --tl:off $project -c Release --no-build --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { $failed += $project }
    }
    if ($failed.Count -gt 0) { throw "M85-C full regression FAILED: $($failed -join ', '). Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M85-C PASSED: independent references, robustness and full regression.' -ForegroundColor Green
    Write-Host 'Note: no external commercial FEM comparison, PDF numeric audit or normative certification is implied.'
    Write-Host "Full console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
