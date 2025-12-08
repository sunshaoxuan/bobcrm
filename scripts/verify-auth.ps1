<#
.SYNOPSIS
    Verifies the authentication flow and connectivity for BobCRM.

.DESCRIPTION
    This script performs a series of checks to ensure the BobCRM environment is healthy:
    1. Checks if critical ports (3000, 5200) are listening.
    2. Attempts direct login to the Backend API (5200).
    3. Attempts login via the Frontend Proxy (3000).

.EXAMPLE
    ./scripts/verify-auth.ps1
#>

$ErrorActionPreference = "Stop"

function Test-Port {
    param($Port)
    $tcptest = Test-NetConnection -ComputerName localhost -Port $Port -InformationLevel Quiet
    if ($tcptest) {
        Write-Host "✅ Port $Port is OPEN" -ForegroundColor Green
        return $true
    }
    else {
        Write-Host "❌ Port $Port is CLOSED or UNREACHABLE" -ForegroundColor Red
        return $false
    }
}

function Test-Login {
    param($BaseUrl, $Name)
    
    Write-Host "Testing Login via $Name ($BaseUrl)..." -NoNewline
    
    $loginUrl = "$BaseUrl/api/auth/login"
    $body = @{
        username = "admin"
        password = "Admin@12345"
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Method Post -Uri $loginUrl -Body $body -ContentType "application/json" -ErrorAction Stop
        
        if ($response.success -eq $true -and !([string]::IsNullOrWhiteSpace($response.data.accessToken))) {
            Write-Host " SUCCESS" -ForegroundColor Green
            Write-Host "   Token received: $($response.data.accessToken.Substring(0, 15))..." -ForegroundColor DarkGray
            return $true
        }
        else {
            Write-Host " FAILED (Logic)" -ForegroundColor Red
            Write-Host "   Response: $($response | ConvertTo-Json -Depth 2)"
            return $false
        }
    }
    catch {
        Write-Host " FAILED (Exception)" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            Write-Host "   Body: $($reader.ReadToEnd())"
        }
        return $false
    }
}

Write-Host "=== BobCRM Integration Verification ===" -ForegroundColor Cyan
Write-Host "Time: $(Get-Date)"
Write-Host ""

# 1. Port Check
$portsOk = $true
if (-not (Test-Port 5200)) { $portsOk = $false }
if (-not (Test-Port 3000)) { $portsOk = $false }

if (-not $portsOk) {
    Write-Host "`n⚠️  Critical ports are not open. Ensure the application is running via 'scripts/dev.ps1'." -ForegroundColor Yellow
    exit 1
}

# 2. Backend Direct Auth
$backendOk = Test-Login "http://localhost:5200" "Backend API"

# 3. Frontend Proxy Auth
$frontendOk = Test-Login "http://localhost:3000" "Frontend Proxy"

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
if ($backendOk -and $frontendOk) {
    Write-Host "✅ ALL SYSTEMS GO. Authentication is working correctly." -ForegroundColor Green
    exit 0
}
elseif ($backendOk -and -not $frontendOk) {
    Write-Host "⚠️  BACKEND OK, FRONTEND FAILING." -ForegroundColor Yellow
    Write-Host "   The API is working, but the Frontend Proxy is not forwarding requests correctly."
    Write-Host "   Check 'src/BobCrm.App/appsettings.Development.json' and 'Program.cs'."
    exit 1
}
else {
    Write-Host "❌ SYSTEM FAILURE." -ForegroundColor Red
    Write-Host "   The Backend API is not accepting logins. Check database seeding / logs."
    exit 1
}
