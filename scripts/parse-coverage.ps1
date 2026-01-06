param([string]$CoberturaPath)

[xml]$xml = Get-Content $CoberturaPath
$pkg = $xml.coverage.packages.package | Where-Object { $_.name -eq 'BobCrm.Api' }

Write-Host "=== BobCrm.Api Coverage Summary ==="
Write-Host ("Line Rate: {0:P2}" -f [double]$pkg.'line-rate')
Write-Host ("Branch Rate: {0:P2}" -f [double]$pkg.'branch-rate')
Write-Host ""

# Aggregate by file
$fileStats = @{}
foreach ($cls in $pkg.classes.class) {
    $file = $cls.filename
    if ($file -match "Migrations" -or $file -match "\.g\.cs$" -or $file -match "Program\.cs$") { continue }

    $lines = @($cls.lines.line)
    $valid = $lines.Count
    $covered = ($lines | Where-Object { [int]$_.hits -gt 0 }).Count

    if (-not $fileStats.ContainsKey($file)) {
        $fileStats[$file] = @{ Valid = 0; Covered = 0 }
    }
    $fileStats[$file].Valid += $valid
    $fileStats[$file].Covered += $covered
}

$rows = $fileStats.GetEnumerator() | ForEach-Object {
    $uncovered = $_.Value.Valid - $_.Value.Covered
    $rate = if ($_.Value.Valid -gt 0) { $_.Value.Covered / $_.Value.Valid } else { 0 }
    [pscustomobject]@{
        Uncovered = $uncovered
        Rate = [math]::Round($rate * 100, 1)
        File = $_.Key -replace '^BobCrm\.Api\\', ''
    }
}

Write-Host "=== Top 20 Uncovered Files ==="
$rows | Sort-Object Uncovered -Descending | Select-Object -First 20 | Format-Table -AutoSize

# Calculate lines needed for 90%
$totalValid = ($fileStats.Values | Measure-Object -Property Valid -Sum).Sum
$totalCovered = ($fileStats.Values | Measure-Object -Property Covered -Sum).Sum
$needed = [math]::Ceiling($totalValid * 0.90 - $totalCovered)
Write-Host ""
Write-Host "=== Gap Analysis ==="
Write-Host ("Total Lines (excl. Program/Migrations): {0}" -f $totalValid)
Write-Host ("Currently Covered: {0}" -f $totalCovered)
Write-Host ("Lines needed for 90%: {0}" -f $needed)
