# sync-assets.ps1 - Path-preserving binary asset Export/Import for Google Drive sharing
# Scans all of Assets/ for git-ignored binary media/art files and packages or restores them with matching .meta files.

param (
    [switch]$Export,
    [switch]$Import,
    [switch]$Status,
    [string]$ArchivePath = "",
    [string]$SetDefaultArchive = "",
    [switch]$CleanBeforeImport = $false
)

$ErrorActionPreference = "Stop"

$BinaryExtensions = @(
    ".png", ".jpg", ".jpeg", ".psd", ".psb", ".tga", ".tif", ".tiff", ".bmp", ".exr", ".hdr", ".webp", ".gif", ".svg",
    ".fbx", ".obj", ".blend", ".dae", ".3ds", ".max", ".glb", ".gltf",
    ".wav", ".mp3", ".ogg", ".aif", ".aiff", ".flac", ".m4a",
    ".mp4", ".mov", ".webm", ".avi", ".mkv",
    ".ttf", ".otf", ".fon", ".fnt",
    ".zip", ".7z", ".tar", ".gz", ".rar", ".bytes", ".bin", ".unitypackage"
)

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$assetsDir = Join-Path $projectRoot "Assets"
$configPath = Join-Path $projectRoot ".asset-sync.local.json"

if (-not (Test-Path -LiteralPath $assetsDir)) {
    Write-Error "Assets directory not found: $assetsDir"
    exit 1
}

function Test-IgnoredSegment {
    param ([string]$Segment)
    return ($Segment.StartsWith('.') -or $Segment.EndsWith('~') -or $Segment -ieq 'desktop.ini' -or $Segment -ieq 'Thumbs.db')
}

function Get-BinaryAssetFiles {
    param ([string]$BaseAssetsDir)

    $results = @()
    $allFiles = Get-ChildItem -LiteralPath $BaseAssetsDir -Recurse -File -Force -ErrorAction SilentlyContinue
    foreach ($f in $allFiles) {
        if ($f.Extension -ieq ".meta") { continue }
        $rel = $f.FullName.Substring($BaseAssetsDir.Length).TrimStart('\', '/')
        $segments = $rel -split '[\\/]'
        $skip = $false
        foreach ($seg in $segments) {
            if (Test-IgnoredSegment -Segment $seg) {
                $skip = $true
                break
            }
        }
        if ($skip) { continue }
        $ext = $f.Extension.ToLowerInvariant()
        if ($BinaryExtensions -contains $ext) {
            $results += $f
        }
    }
    return $results
}

function Get-LocalDefaultArchivePath {
    param ([string]$CfgPath)
    if (Test-Path -LiteralPath $CfgPath) {
        try {
            $cfg = Get-Content -LiteralPath $CfgPath -Raw | ConvertFrom-Json
            if ($null -ne $cfg -and -not [string]::IsNullOrWhiteSpace($cfg.defaultArchivePath)) {
                return [string]$cfg.defaultArchivePath
            }
        } catch {
            Write-Host "[WARN] Failed to parse ${CfgPath}: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    return ""
}

function Save-LocalDefaultArchivePath {
    param (
        [string]$CfgPath,
        [string]$PathValue
    )
    $obj = [PSCustomObject]@{
        defaultArchivePath = $PathValue
    }
    $json = $obj | ConvertTo-Json -Depth 2
    [System.IO.File]::WriteAllText($CfgPath, $json + "`n", [System.Text.UTF8Encoding]::new($false))
    Write-Host "[SAVED] Default archive path saved to .asset-sync.local.json -> $PathValue" -ForegroundColor Green
}

$actionCount = 0
if ($Export) { $actionCount++ }
if ($Import) { $actionCount++ }
if ($Status) { $actionCount++ }

if ($actionCount -gt 1) {
    Write-Error "Specify only one action among -Export, -Import, or -Status."
    exit 1
}

if (-not [string]::IsNullOrWhiteSpace($SetDefaultArchive)) {
    Save-LocalDefaultArchivePath -CfgPath $configPath -PathValue $SetDefaultArchive
    if ($actionCount -eq 0) {
        exit 0
    }
}

if ($actionCount -eq 0) {
    $Status = $true
}

$resolvedArchivePath = $ArchivePath
if ([string]::IsNullOrWhiteSpace($resolvedArchivePath)) {
    $resolvedArchivePath = Get-LocalDefaultArchivePath -CfgPath $configPath
}

if ($Status) {
    Write-Host "=== ArcherCraftor Binary Asset Sync Status ===" -ForegroundColor Cyan
    Write-Host "Project Root    : $projectRoot" -ForegroundColor DarkGray
    if ([string]::IsNullOrWhiteSpace($resolvedArchivePath)) {
        Write-Host "Default Archive : (not set - use -SetDefaultArchive <path>)" -ForegroundColor DarkGray
    } else {
        Write-Host "Default Archive : $resolvedArchivePath" -ForegroundColor DarkGray
    }

    $files = Get-BinaryAssetFiles -BaseAssetsDir $assetsDir
    $totalBytes = 0L
    $missingMeta = 0
    foreach ($f in $files) {
        $totalBytes += $f.Length
        if (-not (Test-Path -LiteralPath ($f.FullName + ".meta"))) {
            $missingMeta++
        }
    }

    $sizeKB = [math]::Round($totalBytes / 1KB, 2)
    $sizeMB = [math]::Round($totalBytes / 1MB, 2)
    Write-Host "Binary Assets   : $($files.Count) files ($sizeKB KB / $sizeMB MB)" -ForegroundColor Green
    if ($missingMeta -gt 0) {
        Write-Host "[WARN] Missing .meta files for $missingMeta binary assets. Open Unity Editor to generate them." -ForegroundColor Yellow
    }

    foreach ($f in $files) {
        $relPath = $f.FullName.Substring($projectRoot.Length + 1)
        $hasMeta = Test-Path -LiteralPath ($f.FullName + ".meta")
        $metaTag = if ($hasMeta) { "meta:OK" } else { "meta:MISSING" }
        Write-Host "  - $relPath ($($f.Length) B, $metaTag)" -ForegroundColor Gray
    }
    exit 0
}

if ([string]::IsNullOrWhiteSpace($resolvedArchivePath)) {
    Write-Error "ArchivePath is required. Pass -ArchivePath <zip_path> or configure one with -SetDefaultArchive <zip_path>."
    exit 1
}

if ($Export) {
    Write-Host "=== ArcherCraftor Binary Asset Export ===" -ForegroundColor Cyan
    Write-Host "Target Zip : $resolvedArchivePath" -ForegroundColor DarkGray

    $files = Get-BinaryAssetFiles -BaseAssetsDir $assetsDir
    if ($files.Count -eq 0) {
        Write-Error "No binary assets found under '$assetsDir' to export."
        exit 1
    }

    $missingMetaList = @()
    $totalBytes = 0L
    $manifestRelPaths = @()
    foreach ($f in $files) {
        $totalBytes += $f.Length
        $relPath = $f.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
        $manifestRelPaths += $relPath
        if (-not (Test-Path -LiteralPath ($f.FullName + ".meta"))) {
            $missingMetaList += $relPath
        }
    }

    if ($missingMetaList.Count -gt 0) {
        Write-Host "[WARN] $($missingMetaList.Count) binary assets are missing .meta files:" -ForegroundColor Yellow
        foreach ($m in ($missingMetaList | Select-Object -First 10)) {
            Write-Host "  - $m" -ForegroundColor Yellow
        }
        Write-Host "[WARN] Open Unity Editor once to generate .meta files before sharing, otherwise GUID references may break." -ForegroundColor Yellow
    }

    $exportParent = Split-Path -Path $resolvedArchivePath -Parent
    if (-not [string]::IsNullOrWhiteSpace($exportParent) -and -not (Test-Path -LiteralPath $exportParent)) {
        New-Item -ItemType Directory -Path $exportParent -Force | Out-Null
    }

    $stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ArcherCraftor_AssetExport_" + [System.Guid]::NewGuid().ToString("N"))
    try {
        New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

        foreach ($f in $files) {
            $relFromRoot = $f.FullName.Substring($projectRoot.Length + 1)
            $destFile = Join-Path $stagingDir $relFromRoot
            $destDir = Split-Path -Path $destFile -Parent
            if (-not (Test-Path -LiteralPath $destDir)) {
                New-Item -ItemType Directory -Path $destDir -Force | Out-Null
            }
            Copy-Item -LiteralPath $f.FullName -Destination $destFile -Force

            $metaSrc = $f.FullName + ".meta"
            if (Test-Path -LiteralPath $metaSrc) {
                Copy-Item -LiteralPath $metaSrc -Destination ($destFile + ".meta") -Force
            }
        }

        $manifestObj = [PSCustomObject]@{
            exportedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
            fileCount     = $files.Count
            totalBytes    = $totalBytes
            files         = $manifestRelPaths
        }
        $manifestJson = $manifestObj | ConvertTo-Json -Depth 4
        $manifestPath = Join-Path $stagingDir "asset-sync-manifest.json"
        [System.IO.File]::WriteAllText($manifestPath, $manifestJson + "`n", [System.Text.UTF8Encoding]::new($false))

        if (Test-Path -LiteralPath $resolvedArchivePath) {
            Remove-Item -LiteralPath $resolvedArchivePath -Force
        }

        $stagingPattern = Join-Path $stagingDir "*"
        Compress-Archive -Path $stagingPattern -DestinationPath $resolvedArchivePath -CompressionLevel Optimal -Force

        $zipSizeKB = [math]::Round((Get-Item -LiteralPath $resolvedArchivePath).Length / 1KB, 2)
        Write-Host "[EXPORTED] $($files.Count) binary assets (+ matching .meta) -> $resolvedArchivePath ($zipSizeKB KB)" -ForegroundColor Green
    } finally {
        if (Test-Path -LiteralPath $stagingDir) {
            Remove-Item -LiteralPath $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    exit 0
}

if ($Import) {
    Write-Host "=== ArcherCraftor Binary Asset Import ===" -ForegroundColor Cyan
    Write-Host "Source Zip : $resolvedArchivePath (CleanBeforeImport: $CleanBeforeImport)" -ForegroundColor DarkGray

    if (-not (Test-Path -LiteralPath $resolvedArchivePath -PathType Leaf)) {
        Write-Error "Archive file not found: $resolvedArchivePath"
        exit 1
    }

    if ($CleanBeforeImport) {
        $existingBinaries = Get-BinaryAssetFiles -BaseAssetsDir $assetsDir
        $removedCount = 0
        foreach ($oldFile in $existingBinaries) {
            $oldMeta = $oldFile.FullName + ".meta"
            Remove-Item -LiteralPath $oldFile.FullName -Force
            if (Test-Path -LiteralPath $oldMeta) {
                Remove-Item -LiteralPath $oldMeta -Force
            }
            $removedCount++
        }
        Write-Host "[CLEANED] Removed $removedCount existing binary assets (and their .meta pairs) under Assets/" -ForegroundColor Yellow
    }

    $stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ArcherCraftor_AssetImport_" + [System.Guid]::NewGuid().ToString("N"))
    try {
        New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null
        Expand-Archive -LiteralPath $resolvedArchivePath -DestinationPath $stagingDir -Force

        $stagingAssets = Join-Path $stagingDir "Assets"
        if (-not (Test-Path -LiteralPath $stagingAssets -PathType Container)) {
            Write-Error "Invalid archive structure: 'Assets/' directory not found inside '$resolvedArchivePath'."
            exit 1
        }

        $extractedFiles = Get-ChildItem -LiteralPath $stagingAssets -Recurse -File -Force
        $restoredAssetCount = 0
        $restoredTotalCount = 0
        foreach ($ef in $extractedFiles) {
            $relInAssets = $ef.FullName.Substring($stagingAssets.Length).TrimStart('\', '/')
            $targetPath = Join-Path $assetsDir $relInAssets
            $targetDir = Split-Path -Path $targetPath -Parent
            if (-not (Test-Path -LiteralPath $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }
            Copy-Item -LiteralPath $ef.FullName -Destination $targetPath -Force
            $restoredTotalCount++
            if ($ef.Extension -ine ".meta") {
                $restoredAssetCount++
            }
        }

        Write-Host "[IMPORTED] Restored $restoredAssetCount asset files ($restoredTotalCount total files including .meta) into Assets/" -ForegroundColor Green

        $restoredBinaries = Get-BinaryAssetFiles -BaseAssetsDir $assetsDir
        $missingAfterImport = 0
        foreach ($rb in $restoredBinaries) {
            if (-not (Test-Path -LiteralPath ($rb.FullName + ".meta"))) {
                $missingAfterImport++
            }
        }
        if ($missingAfterImport -gt 0) {
            Write-Host "[WARN] $missingAfterImport binary assets are missing .meta files after import. Open Unity Editor to regenerate them." -ForegroundColor Yellow
        }
    } finally {
        if (Test-Path -LiteralPath $stagingDir) {
            Remove-Item -LiteralPath $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    exit 0
}
