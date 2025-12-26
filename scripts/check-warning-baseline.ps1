param(
    [string]$Target = "BobCrm.sln",
    [string]$Configuration = "Release",
    [string]$BaselinePath = "warning-baseline.json",
    [switch]$UpdateBaseline
)

$ErrorActionPreference = "Stop"

function Normalize-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RepoRoot
    )

    $p = $Path.Trim()
    if ([string]::IsNullOrWhiteSpace($p)) { return $null }

    if ($p -match "^[a-zA-Z]:\\") {
        $p = $p.Replace("\", "/")
        $root = $RepoRoot.Replace("\", "/").TrimEnd("/")
        if ($p.StartsWith($root + "/", [System.StringComparison]::OrdinalIgnoreCase)) {
            $p = $p.Substring($root.Length + 1)
        }
        return $p
    }

    $p = $p.Replace("\", "/").TrimStart("./")
    return $p
}

function Get-WarningKey {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][string]$File,
        [Parameter(Mandatory = $true)][int]$Line,
        [Parameter(Mandatory = $true)][int]$Column
    )

    return "$Code|$File|$Line|$Column"
}

$repoRoot = (Resolve-Path ".").Path
$baselineFile = Join-Path $repoRoot $BaselinePath

Write-Host "Running warning baseline check..." -ForegroundColor Cyan
Write-Host "  Target: $Target"
Write-Host "  Configuration: $Configuration"
Write-Host "  Baseline: $BaselinePath"
Write-Host "  UpdateBaseline: $UpdateBaseline"

$dotnetArgs = @(
    "build",
    "-t:Rebuild",
    "-c", $Configuration,
    $Target,
    "-v", "minimal",
    "-nologo"
)

Write-Host ("dotnet " + ($dotnetArgs -join " ")) -ForegroundColor DarkGray

$output = & dotnet @dotnetArgs 2>&1 | Out-String
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Host $output
    throw "dotnet build failed (exit=$exitCode). Fix build errors before updating warning baseline."
}

# Example format:
# C:\repo\src\Foo.cs(10,20): warning CS0618: 'X' is obsolete [C:\repo\Foo.csproj]
$warningRegex = '^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s+warning\s+(?<code>[A-Z]{2}\d{4}|BL\d{4}|ASP\d{4}|NU\d{4})\s*:\s*(?<message>.+?)\s*\[(?<project>.+?)\]\s*$'

$warnings = @()
foreach ($line in ($output -split "`r?`n")) {
    $m = [regex]::Match($line, $warningRegex)
    if (-not $m.Success) { continue }

    $fileRaw = $m.Groups["file"].Value
    $file = Normalize-RepoPath -Path $fileRaw -RepoRoot $repoRoot
    if (-not $file) { continue }

    # Ignore transient build outputs.
    if ($file -match '(^|/)(obj|bin)/') { continue }

    $warnings += [pscustomobject]@{
        Code   = $m.Groups["code"].Value
        File   = $file
        Line   = [int]$m.Groups["line"].Value
        Column = [int]$m.Groups["col"].Value
    }
}

$warningKeys = $warnings |
    ForEach-Object { Get-WarningKey -Code $_.Code -File $_.File -Line $_.Line -Column $_.Column } |
    Sort-Object -Unique

if ($UpdateBaseline) {
    $unique = @{}
    foreach ($w in $warnings) {
        $k = Get-WarningKey -Code $w.Code -File $w.File -Line $w.Line -Column $w.Column
        if (-not $unique.ContainsKey($k)) {
            $unique[$k] = $w
        }
    }
    $uniqueWarnings = $unique.Values | Sort-Object Code, File, Line, Column

    $payload = [pscustomobject]@{
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        target = $Target
        configuration = $Configuration
        warnings = $uniqueWarnings
    }

    $json = $payload | ConvertTo-Json -Depth 10
    Set-Content -Path $baselineFile -Value $json -Encoding utf8
    Write-Host "Baseline updated: $BaselinePath (warnings=$($uniqueWarnings.Count))" -ForegroundColor Green
    exit 0
}

if (-not (Test-Path $baselineFile)) {
    Write-Host "Baseline file not found: $BaselinePath" -ForegroundColor Red
    Write-Host "Create it by running: pwsh scripts/check-warning-baseline.ps1 -UpdateBaseline" -ForegroundColor Yellow
    exit 1
}

$baseline = Get-Content $baselineFile -Raw -Encoding utf8 | ConvertFrom-Json
if (-not $baseline.warnings) {
    throw "Baseline file is invalid: missing 'warnings' array."
}

$baselineKeys = @()
foreach ($w in $baseline.warnings) {
    if (-not $w.Code -or -not $w.File -or -not $w.Line -or -not $w.Column) { continue }
    $baselineKeys += Get-WarningKey -Code $w.Code -File $w.File -Line ([int]$w.Line) -Column ([int]$w.Column)
}
$baselineKeys = $baselineKeys | Sort-Object -Unique

$newKeys = Compare-Object -ReferenceObject $baselineKeys -DifferenceObject $warningKeys -PassThru |
    Where-Object { $_ -in $warningKeys }

if ($newKeys -and $newKeys.Count -gt 0) {
    Write-Host "New warnings detected (not in baseline): $($newKeys.Count)" -ForegroundColor Red
    foreach ($k in $newKeys) {
        $parts = $k -split '\|', 4
        $code = $parts[0]; $file = $parts[1]; $lineNo = $parts[2]; $colNo = $parts[3]
        Write-Host ("  {0} {1}:{2}:{3}" -f $code, $file, $lineNo, $colNo) -ForegroundColor Red
    }
    exit 1
}

$removedKeys = Compare-Object -ReferenceObject $baselineKeys -DifferenceObject $warningKeys -PassThru |
    Where-Object { $_ -in $baselineKeys }

if ($removedKeys -and $removedKeys.Count -gt 0) {
    Write-Host "Warnings removed since baseline: $($removedKeys.Count) (consider updating baseline)" -ForegroundColor Yellow
}

Write-Host "Warning baseline check passed. warnings=$($warningKeys.Count), baseline=$($baselineKeys.Count)" -ForegroundColor Green
exit 0
