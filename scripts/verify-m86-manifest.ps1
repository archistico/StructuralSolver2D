# M87-A contains a NEW source manifest; M86 RC1 original hashes are preserved in its own ZIP.
# Historical script name kept as compatibility alias; verifies the CURRENT M87-A archive.
& (Join-Path $PSScriptRoot 'verify-m87-manifest.ps1')
