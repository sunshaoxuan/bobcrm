param(
    [switch] $RunTests,
    [string] $ResultsDirectory = "coverage-results",
    [int] $Top = 25
)

$ErrorActionPreference = "Stop"

function Get-LatestCoberturaPath([string] $root)
{
    if (-not (Test-Path $root))
    {
        return $null
    }

    return Get-ChildItem -Path $root -Recurse -Filter "coverage.cobertura.xml" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

function Is-IncludedFile([string] $file)
{
    if ([string]::IsNullOrWhiteSpace($file))
    {
        return $false
    }

    $normalized = $file.Trim().Replace("/", "\\")
    if ($normalized -notmatch "(^|\\)BobCrm\.Api\\")
    {
        return $false
    }

    if ($normalized.EndsWith("\Program.cs", [System.StringComparison]::OrdinalIgnoreCase)) { return $false }
    if ($normalized -match "\\Migrations\\") { return $false }
    if ($normalized -match "\\obj\\") { return $false }
    if ($normalized -match "\\bin\\") { return $false }
    if ($normalized -match "\\.g\\.cs$") { return $false }
    if ($normalized -match "ModelSnapshot\\.cs$") { return $false }

    return $true
}

if ($RunTests)
{
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $outputDir = Join-Path (Get-Location) (Join-Path $ResultsDirectory ("plan18-xplat-" + $timestamp))
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

    dotnet test tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj -c Release --collect:"XPlat Code Coverage" --results-directory $outputDir
}

$cobertura = Get-LatestCoberturaPath $ResultsDirectory
if (-not $cobertura)
{
    $cobertura = Get-LatestCoberturaPath "tests/BobCrm.Api.Tests/TestResults"
}

if (-not $cobertura)
{
    Write-Error "No coverage.cobertura.xml found under '$ResultsDirectory' or 'tests/BobCrm.Api.Tests/TestResults'."
}

[xml] $xml = Get-Content -Raw $cobertura

$classRows = foreach ($pkg in @($xml.coverage.packages.package))
{
    foreach ($cls in @($pkg.classes.class))
    {
        $file = [string]$cls.filename
        if (-not (Is-IncludedFile $file))
        {
            continue
        }

        $lines = @($cls.lines.line)
        $valid = $lines.Count
        $covered = ($lines | Where-Object { [int]$_.hits -gt 0 }).Count
        $uncovered = $valid - $covered

        [pscustomobject]@{
            File = $file
            Class = [string]$cls.name
            Valid = $valid
            Covered = $covered
            Uncovered = $uncovered
        }
    }
}

$classRows = @($classRows)
if ($classRows.Count -eq 0)
{
    Write-Output "No BobCrm.Api classes found (after exclusions). Coverage file: $cobertura"
    exit 0
}

$totalValid = ($classRows | Measure-Object -Property Valid -Sum).Sum
$totalCovered = ($classRows | Measure-Object -Property Covered -Sum).Sum
$lineRate = if ($totalValid -gt 0) { [math]::Round($totalCovered / $totalValid, 4) } else { 0 }

Write-Output "Coverage file: $cobertura"
Write-Output ("BobCrm.Api (exclusions applied): LineRate={0:P2} Covered={1} Valid={2}" -f $lineRate, $totalCovered, $totalValid)
Write-Output ""

$fileRows = $classRows |
    Group-Object File |
    ForEach-Object {
        $valid = ($_.Group | Measure-Object -Property Valid -Sum).Sum
        $covered = ($_.Group | Measure-Object -Property Covered -Sum).Sum
        $uncovered = $valid - $covered
        [pscustomobject]@{
            Uncovered = $uncovered
            Covered = $covered
            Valid = $valid
            LineRate = if ($valid -gt 0) { [math]::Round($covered / $valid, 4) } else { 0 }
            File = $_.Name
        }
    }

Write-Output "Top uncovered files (BobCrm.Api, exclusions applied):"
$fileRows |
    Sort-Object Uncovered -Descending |
    Select-Object -First $Top |
    Format-Table Uncovered, Covered, Valid, LineRate, File -AutoSize
