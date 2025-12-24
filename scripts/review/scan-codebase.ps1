$ErrorActionPreference = "Stop"

# Default to running from repo root
$validPaths = @("src", "../src", "../../src")
$rootPath = $null

foreach ($p in $validPaths) {
    if (Test-Path $p) {
        $rootPath = Resolve-Path $p
        break
    }
}

if (-not $rootPath) {
    Write-Error "Could not find 'src' directory."
    exit 1
}

$reportPath = Join-Path (Get-Location) "review_scan_results.csv"

$results = @()

Function Add-Finding {
    param (
        [string]$File,
        [int]$Line,
        [string]$Category,
        [string]$Issue,
        [string]$Snippet
    )
    $obj = [PSCustomObject]@{
        File     = $File
        Line     = $Line
        Category = $Category
        Issue    = $Issue
        Snippet  = $Snippet.Trim()
    }
    $script:results += $obj
    Write-Host "[$Category] $File : $Line - $Issue" -ForegroundColor Yellow
}

# 1. Scan for I18n Violations (Hardcoded strings)
# Regex for CJK characters: \p{IsCJKUnifiedIdeographs}
Write-Host "Scanning for Hardcoded Strings..."
$files = Get-ChildItem -Path $rootPath -Recurse -Include *.cs, *.razor
foreach ($file in $files) {
    # Skip generated files
    if ($file.Name.EndsWith(".g.cs") -or $file.Name.EndsWith(".Designer.cs")) { continue }

    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        
        # Simple CJK check
        if ($line -match "[\p{IsCJKUnifiedIdeographs}]") {
            # Exclude comments (simple check)
            if (-not ($line.Trim().StartsWith("//") -or $line.Trim().StartsWith("*"))) {
                Add-Finding -File $file.Name -Line ($i + 1) -Category "I18n" -Issue "Hardcoded Text" -Snippet $line
            }
        }
    }
}

# 2. Scan for Native Browser Dialogs in Razor/JS
Write-Host "Scanning for Native Dialogs..."
$razorFiles = Get-ChildItem -Path $rootPath -Recurse -Include *.razor, *.js
foreach ($file in $razorFiles) {
    if ($file.Name.Contains("jquery")) { continue } # Skip vendor lib

    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        if ($line -match "(alert\(|confirm\(|prompt\()") {
            Add-Finding -File $file.Name -Line ($i + 1) -Category "Std-01" -Issue "Native Browser Dialog" -Snippet $line
        }
    }
}

# 3. Scan for Banned APIs (Console.WriteLine, DateTime.Now)
Write-Host "Scanning for Banned APIs..."
foreach ($file in $files) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        
        if ($line -match "Console\.WriteLine") {
            Add-Finding -File $file.Name -Line ($i + 1) -Category "Std-04" -Issue "Console Logging" -Snippet $line
        }
        if ($line -match "DateTime\.Now") {
            Add-Finding -File $file.Name -Line ($i + 1) -Category "Std-04" -Issue "DateTime.Now usage (Use UtcNow)" -Snippet $line
        }
        if ($line -match "\.Result" -and -not $line -match "TaskResult") {
            # Very basic check, might have false positives
            if ($file.Extension -eq ".cs") { 
                Add-Finding -File $file.Name -Line ($i + 1) -Category "Std-04" -Issue "Sync over Async (.Result)" -Snippet $line
            }
        }
        if ($line -match "\.Wait\(\)") {
            if ($file.Extension -eq ".cs") {
                Add-Finding -File $file.Name -Line ($i + 1) -Category "Std-04" -Issue "Sync over Async (.Wait())" -Snippet $line
            }
        }
    }
}

# 4. Scan for Type Definitions (One Type Per File)
Write-Host "Scanning for One Type Per File..."
$csFiles = Get-ChildItem -Path $rootPath -Recurse -Include *.cs
foreach ($file in $csFiles) {
    if ($file.Name.EndsWith(".g.cs") -or $file.Name.EndsWith(".Designer.cs")) { continue }
    
    $content = Get-Content $file.FullName -Raw
    # Regex to capture top-level type definitions (class, interface, enum, record, struct)
    # Ignoring internal/nested types requires more complex parsing, this is a heuristic.
    # We look for "public class", "public interface", "public enum", "public record"
    
    $types = [regex]::Matches($content, "(public|internal)\s+(class|interface|enum|record|struct)\s+(\w+)")
    
    if ($types.Count -gt 1) {
        # Exclude cases where generic versions exist e.g. class Result and class Result<T> - handled manually in report usually, but let's flag it.
        Add-Finding -File $file.Name -Line 1 -Category "Std-04" -Issue "Multiple Types in File ($($types.Count))" -Snippet ($types | ForEach-Object { $_.Groups[3].Value }) -join ", "
    }
}

$results | Export-Csv -Path $reportPath -NoTypeInformation
Write-Host "Scan Complete. Results saved to $reportPath"
