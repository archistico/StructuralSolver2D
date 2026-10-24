# M81 — isolated Frame2D initial-stress stiffness and pre-buckling gate.
# Existing linear, seismic and nonlinear solvers remain unchanged.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M81 1/3: clean Release build'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "M81 Release build FAILED (exit $LASTEXITCODE). Log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M81 2/3: geometric-stiffness and linearized-buckling C# benchmarks'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~Frame2DInitialStressM81Tests' `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) {
        throw "M81 geometric-stiffness gate FAILED (exit $LASTEXITCODE). Log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M81 3/3: complete regression by independent test project'
    $testProjects = @(
        @{ Name = 'Core.Tests'; Path = 'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj' },
        @{ Name = 'Analysis.Tests'; Path = 'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj' },
        @{ Name = 'Reporting.Tests'; Path = 'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj' },
        @{ Name = 'Cli.Tests'; Path = 'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj' }
    )
    $failed = New-Object 'System.Collections.Generic.List[string]'
    $index = 0
    foreach ($project in $testProjects) {
        $index++
        Write-Host ("M81 regression {0}/4: {1}" -f $index, $project.Name)
        dotnet test --tl:off $project.Path -c Release --no-build `
            --logger 'console;verbosity=minimal'
        $code = $LASTEXITCODE
        if ($code -ne 0) {
            Write-Host ("FAILED: {0} (exit {1})" -f $project.Name, $code) -ForegroundColor Red
            $failed.Add($project.Name)
        }
        else {
            Write-Host ("PASSED: {0}" -f $project.Name) -ForegroundColor Green
        }
    }

    Write-Host "Console log: $script:StructuralConsoleLogPath"
    if ($failed.Count -gt 0) {
        throw ("M81 regression FAILED in: {0}. See log: {1}" -f ($failed -join ', '), $script:StructuralConsoleLogPath)
    }
    Write-Host 'M81 tests PASSED: all four test assemblies successful.' -ForegroundColor Green
}
finally { Pop-Location }
