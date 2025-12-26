# Check for missing translations in i18n-resources.json
param (
    [string]$JsonPath = "src/BobCrm.Api/Resources/i18n-resources.json"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $JsonPath)) {
    Write-Error "File not found: $JsonPath"
    exit 1
}

$jsonContent = Get-Content $JsonPath -Raw | ConvertFrom-Json
$missingCount = 0

foreach ($key in $jsonContent.PSObject.Properties.Name) {
    if ($key -eq "SCHEMA" -or $key.StartsWith("_")) { continue }

    $obj = $jsonContent.$key
    
    if (-not $obj.zh) {
        Write-Host "Missing 'zh' for key: $key" -ForegroundColor Red
        $missingCount++
    }
    if (-not $obj.ja) {
        Write-Host "Missing 'ja' for key: $key" -ForegroundColor Red
        $missingCount++
    }
    if (-not $obj.en) {
        Write-Host "Missing 'en' for key: $key" -ForegroundColor Red
        $missingCount++
    }
}

if ($missingCount -gt 0) {
    Write-Error "Found $missingCount missing translations."
    exit 1
}
else {
    Write-Host "All i18n keys are complete." -ForegroundColor Green
    exit 0
}
