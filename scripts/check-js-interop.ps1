
param (
    [string]$SearchPath = "src/BobCrm.App"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking for JS Interop violations in $SearchPath..." -ForegroundColor Cyan

$files = Get-ChildItem -Path $SearchPath -Recurse -Include *.razor, *.cs |
    Where-Object {
        $_.FullName -notmatch "obj[\\/]" -and
        $_.FullName -notmatch "bin[\\/]" -and
        $_.FullName -notlike "*\IJsInteropService.cs" -and
        $_.FullName -notlike "*\JsInteropService.cs"
    }

$violationCount = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName
    $lineNum = 0
    
    foreach ($line in $content) {
        $lineNum++
        
        # Check for direct IJSRuntime injection
        if ($line -match "@inject\s+IJSRuntime(\s|$)" -or $line -match "\[Inject\][^\r\n]*\bIJSRuntime\b") {
            Write-Host "VIOLATION [IJSRuntime]: $($file.Name):$lineNum - Direct IJSRuntime usage prohibited. Use IJsInteropService." -ForegroundColor Red
            $violationCount++
        }

        # Check for JS invocation bypassing IJsInteropService (IJSObjectReference module calls are allowed)
        if (
            $line -match "\.Invoke(Async)?<" -or
            $line -match "\.InvokeVoid(Async)?\("
        ) {
            if (
                $line -notmatch "\b_module\b" -and
                $line -notmatch "\bmodule\b" -and
                $line -notmatch "\bIJSObjectReference\b"
            ) {
                Write-Host "VIOLATION [DirectInvoke]: $($file.Name):$lineNum - Direct JS invocation detected. Use IJsInteropService." -ForegroundColor Red
                $violationCount++
            }
        }

        # Check for empty catch blocks (single-line form)
        if ($line -match "catch\s*(\([^\)]*\))?\s*\{\s*\}") {
            $allowedEmptyCatchTypes = @(
                "ObjectDisposedException",
                "InvalidOperationException",
                "OperationCanceledException",
                "TaskCanceledException"
            )

            $caughtType = $null
            if ($line -match "catch\s*\(\s*([^\s\)]+)") {
                $caughtType = $Matches[1]
            }

            if (-not $caughtType) {
                Write-Host "VIOLATION [EmptyCatch]: $($file.Name):$lineNum - Empty catch block found." -ForegroundColor Red
                $violationCount++
            }
            elseif ($allowedEmptyCatchTypes -notcontains $caughtType) {
                Write-Host "VIOLATION [EmptyCatch]: $($file.Name):$lineNum - Empty catch block found (type=$caughtType)." -ForegroundColor Red
                $violationCount++
            }
        }
    }
}

if ($violationCount -gt 0) {
    Write-Host "Found $violationCount violations." -ForegroundColor Red
    exit 1
}
else {
    Write-Host "No JS Interop violations found." -ForegroundColor Green
    exit 0
}
