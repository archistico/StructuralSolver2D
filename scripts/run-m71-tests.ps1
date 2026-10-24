# M71 feasibility and terminal-output regression. C# references only, no Python.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Mandatory FIRST operation remains cleaning all project bin/obj folders.
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    foreach ($relative in @(
        'docs/structural/m71-frame2d-plasticity-feasibility.md',
        'tests/StructuralSolver2D.Analysis.Tests/Frame2D/M71/PlasticHingeFeasibilityM71Tests.cs',
        'scripts/stable-dotnet-console.ps1')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative))) {
            throw "M71 missing required file: $relative"
        }
    }
    # The shared console wrapper must not leak process environment settings, and
    # must propagate nonzero exit codes instead of reporting false-positive PASS.
    Write-Host 'M71 console preflight: environment isolation and native exit status'
    $environmentNames = @('CI', 'TERM', 'NO_COLOR', 'DOTNET_NOLOGO')
    $environmentBefore = @{}
    foreach ($name in $environmentNames) {
        $environmentBefore[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
    }
    $null = dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'M71 console preflight: dotnet --version did not succeed.' }
    $null = dotnet __m71_expected_invalid_command__
    if ($LASTEXITCODE -eq 0) { throw 'M71 console preflight: nonzero dotnet exit code was lost.' }
    foreach ($name in $environmentNames) {
        $after = [Environment]::GetEnvironmentVariable($name, 'Process')
        if ($after -cne $environmentBefore[$name]) {
            throw "M71 console preflight: process environment variable $name was not restored."
        }
    }
    if (-not (Test-Path -LiteralPath $script:StructuralConsoleLogPath) -or
        (Get-Item -LiteralPath $script:StructuralConsoleLogPath).Length -eq 0) {
        throw 'M71 console preflight: plain-text log was not created.'
    }
    $global:LASTEXITCODE = 0

    Write-Host 'M71 1/3: clean Release compilation (plain-text console)'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M71 build failed.' }

    Write-Host 'M71 2/3: C# analytical references for the future Frame2D hinge model'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~PlasticHingeFeasibilityM71Tests' `
        --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M71 analytical benchmark failed.' }

    Write-Host 'M71 3/3: complete regression (including validated M70 CQC)'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M71 full regression failed.' }
    Write-Host 'M71 automated checks PASSED. M71 is a design gate, NOT a nonlinear Frame2D production solver.'
    Write-Host "Plain-text log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
