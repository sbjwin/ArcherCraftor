# setup-external-assets.ps1 - Manage local Assets/ExternalAssets folder and shared archive import/export
# Allows sharing heavy assets (video, PSD, audio packs, asset store packages) via Google Drive zip archives without Git LFS bloat.

param (
    [string]$ArchivePath = "",
    [switch]$CleanExtract = $false,
    [string]$ExportArchivePath = ""
)

$ErrorActionPreference = "Stop"

function Test-IgnoredAssetSegment {
    param ([string]$Segment)
    return ($Segment.StartsWith('.') -or $Segment.EndsWith('~') -or $Segment -ieq 'desktop.ini' -or $Segment -ieq 'Thumbs.db')
}

function Get-MissingMetaRelativePaths {
    param (
        [string]$BaseDir,
        [string]$RootDir
    )

    $missing = @()
    if (-not (Test-Path -LiteralPath $BaseDir)) {
        return $missing
    }

    $items = Get-ChildItem -LiteralPath $BaseDir -Recurse -Force | Where-Object {
        if ($_.Extension -eq ".meta") { return $false }
        $rel = $_.FullName.Substring($BaseDir.Length).TrimStart('\', '/')
        $segments = $rel -split '[\\/]'
        foreach ($seg in $segments) {
            if (Test-IgnoredAssetSegment -Segment $seg) { return $false }
        }
        return $true
    }

    foreach ($item in $items) {
        if (-not (Test-Path -LiteralPath ($item.FullName + ".meta"))) {
            $missing += $item.FullName.Substring($RootDir.Length + 1)
        }
    }

    return $missing
}

if (-not [string]::IsNullOrWhiteSpace($ArchivePath) -and -not [string]::IsNullOrWhiteSpace($ExportArchivePath)) {
    Write-Error "Specify either -ArchivePath or -ExportArchivePath, not both."
    exit 1
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$assetsDir = Join-Path $projectRoot "Assets"
$linkPath = Join-Path $assetsDir "ExternalAssets"
$metaPath = Join-Path $assetsDir "ExternalAssets.meta"

# Fixed folder GUID so all team members resolve the ExternalAssets folder consistently
$fixedFolderGuid = "e8f0a1b2c3d44e5f8a9b0c1d2e3f4a5b"

if (-not (Test-Path -LiteralPath $assetsDir)) {
    Write-Error "Assets directory not found: $assetsDir"
    exit 1
}

Write-Host "=== ArcherCraftor ExternalAssets Setup ===" -ForegroundColor Cyan
Write-Host "Local Folder : $linkPath" -ForegroundColor DarkGray
if (-not [string]::IsNullOrWhiteSpace($ArchivePath)) {
    Write-Host "Import Zip   : $ArchivePath (CleanExtract: $CleanExtract)" -ForegroundColor DarkGray
}
if (-not [string]::IsNullOrWhiteSpace($ExportArchivePath)) {
    Write-Host "Export Zip   : $ExportArchivePath" -ForegroundColor DarkGray
}

# 1. Remove legacy NTFS Junction safely without touching remote target data, then ensure normal local folder
$existingItem = Get-Item -LiteralPath $linkPath -Force -ErrorAction SilentlyContinue
if ($null -ne $existingItem) {
    $isReparsePoint = ($existingItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
    if ($isReparsePoint) {
        $rmdirOutput = cmd /c rmdir "`"$linkPath`"" 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to remove existing Junction '$linkPath'. Output: $rmdirOutput"
            exit 1
        }
        Write-Host "[UNLINKED] Removed legacy Junction: $linkPath" -ForegroundColor Yellow
    } elseif (-not $existingItem.PSIsContainer) {
        Write-Error "A file already exists at '$linkPath'. Remove or move it and re-run this script."
        exit 1
    }
}

if (-not (Test-Path -LiteralPath $linkPath)) {
    New-Item -ItemType Directory -Path $linkPath -Force | Out-Null
    Write-Host "[CREATED] Local directory: $linkPath" -ForegroundColor Green
} else {
    Write-Host "[OK] Local directory exists: $linkPath" -ForegroundColor Gray
}

# 2. Ensure consistent Assets/ExternalAssets.meta exists
if (-not (Test-Path -LiteralPath $metaPath)) {
    $metaContent = @(
        "fileFormatVersion: 2",
        "guid: $fixedFolderGuid",
        "folderAsset: yes",
        "DefaultImporter:",
        "  externalObjects: {}",
        "  userData: ",
        "  assetBundleName: ",
        "  assetBundleVariant: ",
        ""
    ) -join "`n"
    [System.IO.File]::WriteAllText($metaPath, $metaContent, [System.Text.UTF8Encoding]::new($false))
    Write-Host "[CREATED] Folder meta generated with shared GUID: $fixedFolderGuid" -ForegroundColor Green
} else {
    Write-Host "[OK] ExternalAssets.meta already exists." -ForegroundColor Gray
}

# 3. Extract optional zip archive into Assets/ExternalAssets
if (-not [string]::IsNullOrWhiteSpace($ArchivePath)) {
    if (-not (Test-Path -LiteralPath $ArchivePath -PathType Leaf)) {
        Write-Error "Archive file not found: $ArchivePath"
        exit 1
    }

    if ($CleanExtract) {
        Write-Host "Cleaning existing contents inside $linkPath..." -ForegroundColor Yellow
        Get-ChildItem -LiteralPath $linkPath -Force | Remove-Item -Recurse -Force
    }

    Write-Host "Extracting archive into $linkPath..." -ForegroundColor Cyan
    Expand-Archive -LiteralPath $ArchivePath -DestinationPath $linkPath -Force
    Write-Host "[EXTRACTED] $ArchivePath -> $linkPath" -ForegroundColor Green

    $missingAfterExtract = Get-MissingMetaRelativePaths -BaseDir $linkPath -RootDir $projectRoot
    if ($missingAfterExtract.Count -gt 0) {
        Write-Host "[WARN] Some extracted assets are missing .meta files ($($missingAfterExtract.Count) items). Open Unity or ensure shared archives include .meta files to avoid GUID mismatches." -ForegroundColor Yellow
    }
}

# 4. Export Assets/ExternalAssets (including .meta files) to a zip archive for Google Drive sharing
if (-not [string]::IsNullOrWhiteSpace($ExportArchivePath)) {
    $packCandidates = Get-ChildItem -LiteralPath $linkPath -Force -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -ine 'desktop.ini' -and $_.Name -ine 'Thumbs.db' -and $_.Name -ine '.DS_Store'
    }

    if ($null -eq $packCandidates -or $packCandidates.Count -eq 0) {
        Write-Error "No files found inside '$linkPath' to export."
        exit 1
    }

    $missingBeforeExport = Get-MissingMetaRelativePaths -BaseDir $linkPath -RootDir $projectRoot
    if ($missingBeforeExport.Count -gt 0) {
        Write-Host "[WARN] Missing .meta files detected inside Assets/ExternalAssets ($($missingBeforeExport.Count) items):" -ForegroundColor Yellow
        foreach ($m in ($missingBeforeExport | Select-Object -First 10)) {
            Write-Host "  - $m" -ForegroundColor Yellow
        }
        Write-Host "[WARN] Open the Unity Editor once so Unity generates .meta files before re-exporting, otherwise teammates may see broken GUID references." -ForegroundColor Yellow
    }

    $exportParent = Split-Path -Path $ExportArchivePath -Parent
    if (-not [string]::IsNullOrWhiteSpace($exportParent) -and -not (Test-Path -LiteralPath $exportParent)) {
        New-Item -ItemType Directory -Path $exportParent -Force | Out-Null
    }

    $stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ArcherCraftor_ExtExport_" + [System.Guid]::NewGuid().ToString("N"))
    try {
        New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null
        foreach ($entry in $packCandidates) {
            Copy-Item -LiteralPath $entry.FullName -Destination $stagingDir -Recurse -Force
        }

        # Remove OS metadata files recursively from staging copy before archiving
        Get-ChildItem -LiteralPath $stagingDir -Recurse -Force | Where-Object {
            $_.Name -ieq 'desktop.ini' -or $_.Name -ieq 'Thumbs.db' -or $_.Name -ieq '.DS_Store'
        } | Remove-Item -Force -ErrorAction SilentlyContinue

        $stagingItems = Join-Path $stagingDir "*"
        if (Test-Path -LiteralPath $ExportArchivePath) {
            Remove-Item -LiteralPath $ExportArchivePath -Force
        }
        Compress-Archive -Path $stagingItems -DestinationPath $ExportArchivePath -CompressionLevel Optimal -Force
        Write-Host "[EXPORTED] $linkPath -> $ExportArchivePath" -ForegroundColor Green
    } finally {
        if (Test-Path -LiteralPath $stagingDir) {
            Remove-Item -LiteralPath $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "----------------------------------------" -ForegroundColor DarkGray
Write-Host "[DONE] Local ExternalAssets setup complete." -ForegroundColor Green
