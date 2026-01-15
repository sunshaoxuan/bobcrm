param(
    [string]$ResultId = "PC-001",
    [string[]]$PytestArgs = @()
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\\..")
$e2eRoot = Join-Path $repoRoot "tests\\e2e"
$resultsRoot = Join-Path $repoRoot "docs\\history\\test-results"

New-Item -ItemType Directory -Path $resultsRoot -Force | Out-Null

1..9 | ForEach-Object {
    $id = "PC-{0:D3}" -f $_
    $dir = Join-Path $resultsRoot $id
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $keep = Join-Path $dir ".gitkeep"
    if (-not (Test-Path $keep)) {
        New-Item -ItemType File -Path $keep | Out-Null
    }
}

if ([string]::IsNullOrWhiteSpace($ResultId)) {
    $ResultId = "PC-001"
}

$targetDir = Join-Path $resultsRoot $ResultId
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$runDir = Join-Path $targetDir $timestamp
New-Item -ItemType Directory -Path $runDir -Force | Out-Null

$artifactSources = @(
    Join-Path $e2eRoot "screenshots"
    Join-Path $e2eRoot "videos"
    Join-Path $e2eRoot "reports"
)

$exitCode = 0
try {
    Push-Location $repoRoot
    python -m pytest $e2eRoot @PytestArgs
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
    foreach ($src in $artifactSources) {
        if (Test-Path $src) {
            Copy-Item -Path $src -Destination $runDir -Recurse -Force
        }
    }
}

exit $exitCode
