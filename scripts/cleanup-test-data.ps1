#!/usr/bin/env pwsh
# 清理测试数据库脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  清理测试数据和重建开发数据库" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查Docker是否运行
$dockerRunning = docker ps 2>$null
if (!$dockerRunning) {
    Write-Host "[错误] Docker未运行或bobcrm-postgres容器未启动" -ForegroundColor Red
    Write-Host "请先启动Docker和PostgreSQL容器：docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

# 1. 删除所有测试数据库
Write-Host "1. 删除所有测试数据库（bobcrm_test*）" -ForegroundColor Yellow
try {
    $query = "SELECT datname FROM pg_database WHERE datname LIKE 'bobcrm_test%'"
    $result = docker exec bobcrm-postgres psql -U postgres -t -c $query 2>&1
    
    if ($LASTEXITCODE -eq 0 -and $result) {
        $databases = $result -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
        
        if ($databases.Count -gt 0) {
            Write-Host "  找到 $($databases.Count) 个测试数据库" -ForegroundColor Gray
            foreach ($dbName in $databases) {
                Write-Host "    删除: $dbName" -ForegroundColor Gray
                docker exec bobcrm-postgres psql -U postgres -c "DROP DATABASE IF EXISTS `"$dbName`"" 2>&1 | Out-Null
            }
            Write-Host "  ✓ 测试数据库已清理" -ForegroundColor Green
        } else {
            Write-Host "  未找到测试数据库" -ForegroundColor Gray
        }
    } else {
        Write-Host "  未找到测试数据库" -ForegroundColor Gray
    }
} catch {
    Write-Host "  [警告] 清理测试数据库时出错: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  清理完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "说明：" -ForegroundColor Yellow
Write-Host "  - 测试数据库（bobcrm_test）已清理" -ForegroundColor White
Write-Host "  - 开发数据库（bobcrm）未受影响" -ForegroundColor White
Write-Host "  - 如需重建开发数据库，请手动运行：" -ForegroundColor White
Write-Host "    docker exec bobcrm-postgres psql -U postgres -c 'DROP DATABASE IF EXISTS bobcrm'" -ForegroundColor Gray
Write-Host "    docker exec bobcrm-postgres psql -U postgres -c 'CREATE DATABASE bobcrm'" -ForegroundColor Gray
Write-Host "    pwsh scripts/dev.ps1 -Action restart" -ForegroundColor Gray
Write-Host ""

