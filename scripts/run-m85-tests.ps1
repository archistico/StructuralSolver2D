# M85-A: independent closed-form and metamorphic validation.
# Compiles and tests the same FEM production code as M83-A, without modifying it.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M85-A 1/3: clean Release build'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "M85-A Release build FAILED. Log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M85-A 2/3: independent static invariants, M82 and M83 numerical gates'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~IndependentStaticInvariantsM85Tests|FullyQualifiedName~Frame2DGlobalSecondOrderM82Tests|FullyQualifiedName~Frame2DScalabilityM83Tests|FullyQualifiedName~StaticEnergyVerificationTests' `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) {
        throw "M85-A focused validation FAILED. Log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M85-A 3/3: complete regression per project'
    $projects = @(
        @{ Name = 'Core.Tests'; Path = 'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj' },
        @{ Name = 'Analysis.Tests'; Path = 'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj' },
        @{ Name = 'Reporting.Tests'; Path = 'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj' },
        @{ Name = 'Cli.Tests'; Path = 'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj' }
    )
    $failed = New-Object 'System.Collections.Generic.List[string]'
    foreach ($item in $projects) {
        Write-Host ("M85-A regression: {0}" -f $item.Name)
        dotnet test --tl:off $item.Path -c Release --no-build --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) {
            $failed.Add($item.Name)
            Write-Host ("FAILED: {0}" -f $item.Name) -ForegroundColor Red
        } else {
            Write-Host ("PASSED: {0}" -f $item.Name) -ForegroundColor Green
        }
    }
    if ($failed.Count -gt 0) {
        throw ("M85-A regression FAILED in: {0}. Log: {1}" -f ($failed -join ', '), $script:StructuralConsoleLogPath)
    }

    Write-Host 'M85-A PASSED: independent numerical gate and full regression.' -ForegroundColor Green
    Write-Host 'Scope: linear elastic 2D FEM, not certification and not design-code verification.'
    Write-Host "Console log: $script:StructuralConsoleLogPath"
    Write-Host 'M83 performance reports remain separate: artifacts/benchmarks/m83.'
}
finally { Pop-Location }
