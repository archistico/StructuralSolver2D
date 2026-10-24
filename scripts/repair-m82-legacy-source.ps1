# M82 one-time repair for projects unpacked over an older checkout.
# The legacy class is not part of the M82 full distribution; keeping its .cs
# source in the project directory triggers CS0246 for PreparedStaticSystemM82.
# This script saves the original source outside the compilation tree.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$legacy = Join-Path $root 'src/StructuralSolver2D.Analysis/Frame2D/SecondOrder/Frame2DGlobalLinearizedM82.cs'
if (-not (Test-Path -LiteralPath $legacy -PathType Leaf)) {
    Write-Host 'M82 legacy-source repair: no obsolete Frame2DGlobalLinearizedM82.cs detected.'
    return
}

$item = Get-Item -LiteralPath $legacy -Force
if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "M82 legacy-source repair: refusing to move a linked file: $legacy"
}

$backupDirectory = Join-Path $root '.m82-legacy-backup'
if (-not (Test-Path -LiteralPath $backupDirectory)) {
    New-Item -ItemType Directory -Path $backupDirectory -ErrorAction Stop | Out-Null
}
$backupItem = Get-Item -LiteralPath $backupDirectory -Force
if (-not $backupItem.PSIsContainer -or
    ($backupItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "M82 legacy-source repair: invalid backup directory: $backupDirectory"
}

# Never overwrite an existing backup, including from an earlier repair.
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fffffff'
$backup = Join-Path $backupDirectory "Frame2DGlobalLinearizedM82.cs.$stamp.bak"
if (Test-Path -LiteralPath $backup) {
    throw "M82 legacy-source repair: unexpected existing backup: $backup"
}
Move-Item -LiteralPath $legacy -Destination $backup -ErrorAction Stop
Write-Warning "Obsolete M82 source moved out of compilation. Backup: $backup"
