# check-meta.ps1 - Unity .meta file integrity check
# Validates that all files and folders in Assets have 1:1 matching .meta files.

param (
    [string]$AssetsPath = "Assets",
    [switch]$FixMissing = $false
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetAssets = Join-Path $root $AssetsPath

if (-not (Test-Path $targetAssets)) {
    Write-Error "Assets directory not found: $targetAssets"
    exit 1
}

Write-Host "=== Unity .meta Integrity Check ===" -ForegroundColor Cyan
Write-Host "Target: $targetAssets" -ForegroundColor DarkGray

$missingMetaCount = 0
$orphanedMetaCount = 0

# 1. Check for missing .meta files (skip Unity-ignored dotfiles, hidden folders, and ~ temp entries)
$items = Get-ChildItem -Path $targetAssets -Recurse | Where-Object {
    if ($_.Extension -eq ".meta") { return $false }
    $rel = $_.FullName.Substring($targetAssets.Length).TrimStart('\', '/')
    $segments = $rel -split '[\\/]'
    foreach ($seg in $segments) {
        if ($seg.StartsWith('.') -or $seg.EndsWith('~') -or $seg -ieq 'desktop.ini' -or $seg -ieq 'Thumbs.db') { return $false }
    }
    return $true
}

foreach ($item in $items) {
    $expectedMeta = $item.FullName + ".meta"
    if (-not (Test-Path $expectedMeta)) {
        Write-Host "  [MISSING META] $($item.FullName.Substring($root.Path.Length + 1))" -ForegroundColor Red
        $missingMetaCount++

        if ($FixMissing) {
            $guid = [System.Guid]::NewGuid().ToString("N")
            if ($item.PSIsContainer) {
                @"
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Out-File -FilePath $expectedMeta -Encoding utf8
            } else {
                @"
fileFormatVersion: 2
guid: $guid
"@ | Out-File -FilePath $expectedMeta -Encoding utf8
            }
            Write-Host "    -> .meta generated (GUID: $guid)" -ForegroundColor Green
        }
    }
}

# 2. Check for orphaned .meta files
$metaFiles = Get-ChildItem -Path $targetAssets -Recurse -Filter "*.meta"

foreach ($meta in $metaFiles) {
    $targetPath = $meta.FullName.Substring(0, $meta.FullName.Length - 5)
    if (-not (Test-Path $targetPath)) {
        Write-Host "  [ORPHANED META] $($meta.FullName.Substring($root.Path.Length + 1))" -ForegroundColor Yellow
        $orphanedMetaCount++
    }
}

Write-Host "----------------------------------------" -ForegroundColor DarkGray
if ($missingMetaCount -eq 0 -and $orphanedMetaCount -eq 0) {
    Write-Host "[PASS] All .meta files are verified and consistent." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Found $missingMetaCount missing and $orphanedMetaCount orphaned .meta files." -ForegroundColor Red
    if (-not $FixMissing) {
        Write-Host "Tip: Run with -FixMissing switch to automatically generate missing .meta files." -ForegroundColor Gray
    }
    exit 1
}
