# 启动 API 并输出到日志文件
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "logs\api_$timestamp.log"

Write-Host "启动 API，日志输出到: $logFile"

cd src\BobCrm.Api
dotnet run --no-launch-profile --urls "http://0.0.0.0:5200" *>&1 | Tee-Object -FilePath "..\..\$logFile"

