# M61 research milestone: validate shared PowerShell cleanup preflight and all M1-M60 regressions.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight, before checking dotnet, building, or running tests.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$cleaner = Join-Path $PSScriptRoot 'clean-project-build.ps1'
$temporary = Join-Path ([System.IO.Path]::GetTempPath()) ('m61-clean-check-' + [Guid]::NewGuid().ToString('N'))
try {
    Write-Host 'M61 1/3: audit all PowerShell test preflights'
    $scripts = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'run-m*-tests.ps1' -File)
    if ($scripts.Count -lt 12) { throw "Expected at least 12 test runners, found $($scripts.Count)." }
    foreach ($script in $scripts) {
        $lines = @(Get-Content -LiteralPath $script.FullName)
        $firstAction = @($lines | Where-Object { $_.Trim() -ne '' -and -not $_.TrimStart().StartsWith('#') })
        if ($firstAction.Count -lt 2 -or $firstAction[0].Trim() -ne "`$ErrorActionPreference = 'Stop'" -or
            $firstAction[1].Trim() -ne "& (Join-Path `$PSScriptRoot 'clean-project-build.ps1')") {
            throw "Missing or misplaced mandatory cleanup preflight in $($script.Name)."
        }
    }
    Write-Host "Verified cleanup-first policy in $($scripts.Count) test scripts."

    Write-Host 'M61 2/3: scratch-directory cleanup safety, -WhatIf and idempotency'
    $src = Join-Path $temporary 'src/Demo'
    $tests = Join-Path $temporary 'tests/Demo.Tests'
    $unrelated = Join-Path $temporary 'docs/bin'
    foreach ($path in @($src, $tests, $unrelated)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
    Set-Content -LiteralPath (Join-Path $temporary 'Demo.sln') -Value 'Fixture only'
    Set-Content -LiteralPath (Join-Path $src 'Demo.csproj') -Value '<Project />'
    Set-Content -LiteralPath (Join-Path $tests 'Demo.Tests.csproj') -Value '<Project />'
    foreach ($dir in @($src, $tests)) {
        foreach ($folder in @('bin', 'obj')) {
            $target = Join-Path $dir $folder
            New-Item -ItemType Directory -Path $target -Force | Out-Null
            Set-Content -LiteralPath (Join-Path $target 'old.dll') -Value 'old'
        }
    }
    Set-Content -LiteralPath (Join-Path $unrelated 'keep.txt') -Value 'preserved'
    & $cleaner -Root $temporary -WhatIf
    if (-not (Test-Path -LiteralPath (Join-Path $src 'bin/old.dll'))) { throw '-WhatIf unexpectedly deleted a folder.' }
    & $cleaner -Root $temporary
    & $cleaner -Root $temporary # must be safe to run twice
    foreach ($dir in @($src, $tests)) {
        foreach ($folder in @('bin', 'obj')) {
            if (Test-Path -LiteralPath (Join-Path $dir $folder)) { throw "Cleanup left behind $dir/$folder" }
        }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $unrelated 'keep.txt'))) { throw 'Cleanup deleted a non-project folder.' }
    if (-not (Test-Path -LiteralPath (Join-Path $src 'Demo.csproj'))) { throw 'Cleanup deleted project source.' }

    Write-Host 'M61 3/3: clean Release build + all M1-M60 regression tests'
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 8 SDK is required for the full M61 regression test.' }
    Push-Location $root
    try {
        dotnet build --tl:off StructuralSolver2D.sln -c Release
        if ($LASTEXITCODE -ne 0) { throw 'M61 full build failed.' }
        dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw 'M61 full regression failed.' }
    }
    finally { Pop-Location }
    Write-Host 'M61 automated checks PASSED. Nonlinear structural analysis remains OUT OF SCOPE.'
}
finally {
    if (Test-Path -LiteralPath $temporary) {
        Remove-Item -LiteralPath $temporary -Recurse -Force
    }
}
