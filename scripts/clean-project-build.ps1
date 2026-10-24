# Remove bin/obj immediately under each MSBuild project directory.
# This script intentionally leaves non-project folders (including reports/examples) alone.
# Usage: .\scripts\clean-project-build.ps1 [-WhatIf] [-Root path]
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [string]$Root = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$resolvedRoot = (Resolve-Path -LiteralPath $Root -ErrorAction Stop).Path
$solutionFiles = @(Get-ChildItem -LiteralPath $resolvedRoot -File -ErrorAction Stop |
    Where-Object { $_.Extension -in @('.sln', '.slnx') })
if ($solutionFiles.Count -eq 0) {
    throw "Safety guard: no .sln/.slnx solution found at '$resolvedRoot'. Nothing was deleted."
}

$projects = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File -ErrorAction Stop |
    Where-Object {
        $segments = $_.FullName.Substring($resolvedRoot.Length) -split '[\\/]'
        $_.Extension -in @('.csproj', '.vbproj', '.fsproj') -and
        'bin' -notin $segments -and 'obj' -notin $segments
    } | Sort-Object -Property FullName -Unique)
if ($projects.Count -eq 0) {
    throw "Safety guard: no MSBuild project files found under '$resolvedRoot'. Nothing was deleted."
}

$removed = 0
foreach ($project in $projects) {
    $projectDirectory = $project.Directory
    $ancestor = $projectDirectory
    while ($null -ne $ancestor) {
        if (($ancestor.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to clean a project inside a linked directory: $($ancestor.FullName)"
        }
        if ($ancestor.FullName -eq $resolvedRoot) { break }
        $ancestor = $ancestor.Parent
    }
    foreach ($name in @('bin', 'obj')) {
        $target = Join-Path $projectDirectory.FullName $name
        if (-not (Test-Path -LiteralPath $target)) { continue }
        $item = Get-Item -LiteralPath $target -Force -ErrorAction Stop
        if (-not $item.PSIsContainer -or ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to remove a non-directory or a linked build path: $target"
        }
        if ($PSCmdlet.ShouldProcess($target, 'Remove build output directory recursively')) {
            try {
                Remove-Item -LiteralPath $target -Recurse -Force -ErrorAction Stop
            }
            catch {
                throw "Cannot remove '$target'. Close processes using the build output and retry. $($_.Exception.Message)"
            }
            $removed++
        }
    }
}
Write-Host "Build cleanup: scanned $($projects.Count) MSBuild project(s), removed $removed bin/obj folder(s)."
