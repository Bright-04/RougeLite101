<#
Simple Unity repo health check script.
Run from the repository root in PowerShell:
    .\tools\check_unity_repo.ps1

This script checks:
 - presence of common ProjectSettings files
 - presence of Packages/manifest.json
 - untracked files that might be missing from the repo
 - files under Assets that are missing .meta files on disk
 - prints the Unity editor version from ProjectSettings/ProjectVersion.txt
#>

Write-Host "Unity repo health check"
Write-Host "Repository root: " (Get-Location)

$errors = @()

# 1) Check ProjectSettings
$expectedPS = @(
    "ProjectSettings/ProjectSettings.asset",
    "ProjectSettings/ProjectVersion.txt",
    "ProjectSettings/EditorBuildSettings.asset",
    "ProjectSettings/TagManager.asset",
    "ProjectSettings/GraphicsSettings.asset"
)

foreach ($p in $expectedPS) {
    if (-not (Test-Path $p)) {
        $errors += "Missing ProjectSettings file: $p"
    } else {
        Write-Host "OK: $p"
    }
}

# 2) Packages manifest
if (-not (Test-Path "Packages/manifest.json")) {
    $errors += "Missing Packages/manifest.json"
} else {
    Write-Host "OK: Packages/manifest.json"
}

# 3) List untracked files (could be assets accidentally not committed)
Write-Host "Checking for untracked files (git)..."
$untracked = git ls-files --others --exclude-standard 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Git not available or not a git repo. Skipping git checks." -ForegroundColor Yellow
} elseif ($untracked) {
    Write-Host "Untracked files found (these may need to be committed):" -ForegroundColor Yellow
    $untracked | ForEach-Object { Write-Host "  $_" }
    $errors += "Untracked files present"
} else {
    Write-Host "No untracked files."
}

# 4) Check for missing .meta files for files under Assets (ignores common text/code files)
Write-Host "Scanning Assets/ for files missing .meta..."
$skipExts = @('.cs', '.asmdef', '.meta', '.md', '.txt', '.json')
$missingMeta = @()
Get-ChildItem -Path Assets -Recurse -File | Where-Object { 
    $ext = $_.Extension.ToLower();
    -not ($skipExts -contains $ext)
} | ForEach-Object {
    $meta = $_.FullName + '.meta'
    if (-not (Test-Path $meta)) {
        $rel = $_.FullName.Substring((Get-Location).Path.Length + 1)
        $missingMeta += $rel
        Write-Host "Missing .meta for: $rel" -ForegroundColor Red
    }
}
if ($missingMeta.Count -eq 0) { Write-Host "All assets have .meta files." }

# 5) Print Unity version
if (Test-Path "ProjectSettings/ProjectVersion.txt") {
    $pv = Get-Content "ProjectSettings/ProjectVersion.txt" -Raw
    Write-Host "ProjectVersion.txt contents:`n$pv"
}

Write-Host "\nSummary:" 
if ($errors.Count -eq 0 -and $missingMeta.Count -eq 0) {
    Write-Host "No obvious repo issues found." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Issues detected:" -ForegroundColor Red
    ($errors + $missingMeta) | ForEach-Object { Write-Host " - $_" }
    exit 2
}
