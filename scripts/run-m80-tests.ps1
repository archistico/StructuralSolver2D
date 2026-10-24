# M80-FIX2: geometry gate, focused M78/M79 regression, per-project full regression.
# The previous `dotnet test StructuralSolver2D.sln` could display a passing
# summary for Cli.Tests even when a different test assembly had failed.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M80 1/5: clean Release compilation'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "M80 Release build FAILED (exit code $LASTEXITCODE). Log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M80 2/5: C# parametric section geometry and catalog benchmarks'
    dotnet test --tl:off tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~SectionGeometryM80Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        throw "M80 section-geometry gate FAILED (exit code $LASTEXITCODE). Log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M80 3/5: existing section factories, catalogs and unit-conversion regression'
    dotnet test --tl:off tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~StructuralSectionFactoryTests|FullyQualifiedName~StructuralSectionCatalogTests|FullyQualifiedName~StructuralSectionCatalogJsonSerializerTests|FullyQualifiedName~StructuralUnitConversionsM75Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        throw "M80 legacy section contract regression FAILED (exit code $LASTEXITCODE). Log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M80 4/5: focused M78/M79 exact-extrema and envelope regressions'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~Frame2DExactExtremaM78Tests|FullyQualifiedName~Frame2DStaticForceEnvelopeM79Tests' `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) {
        throw "M80 M78/M79 regression gate FAILED (exit code $LASTEXITCODE). Log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M80 5/5: full regression, one test project at a time'
    $testProjects = @(
        @{ Name = 'Core.Tests'; Path = 'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj' },
        @{ Name = 'Analysis.Tests'; Path = 'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj' },
        @{ Name = 'Reporting.Tests'; Path = 'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj' },
        @{ Name = 'Cli.Tests'; Path = 'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj' }
    )
    $failedProjects = New-Object 'System.Collections.Generic.List[string]'
    $completedProjects = 0
    foreach ($project in $testProjects) {
        $completedProjects++
        Write-Host ("M80 regression {0}/{1}: {2}" -f $completedProjects, $testProjects.Count, $project.Name)
        dotnet test --tl:off $project.Path -c Release --no-build --logger 'console;verbosity=minimal'
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            Write-Host ("FAILED: {0} (exit code {1})" -f $project.Name, $exitCode) -ForegroundColor Red
            $failedProjects.Add(("{0} (exit {1})" -f $project.Name, $exitCode))
        }
        else {
            Write-Host ("PASSED: {0}" -f $project.Name) -ForegroundColor Green
        }
    }
    Write-Host "Console log: $script:StructuralConsoleLogPath"
    if ($failedProjects.Count -gt 0) {
        throw ("M80 full regression FAILED in: {0}. See log: {1}" -f ($failedProjects -join ', '), $script:StructuralConsoleLogPath)
    }
    Write-Host 'M80 tests PASSED: all four test projects completed with exit code 0.' -ForegroundColor Green
}
finally { Pop-Location }
