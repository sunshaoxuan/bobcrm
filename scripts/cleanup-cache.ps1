#!/usr/bin/env pwsh
# 清理临时缓存和编译文件

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  清理临时缓存和编译文件" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$totalFreed = 0

# 1. 清理 bin 和 obj 目录
Write-Host "1. 清理编译输出 (bin, obj)" -ForegroundColor Yellow
$binDirs = Get-ChildItem -Path . -Recurse -Directory -Filter 'bin' -ErrorAction SilentlyContinue
$objDirs = Get-ChildItem -Path . -Recurse -Directory -Filter 'obj' -ErrorAction SilentlyContinue

foreach ($dir in ($binDirs + $objDirs)) {
    try {
        $size = (Get-ChildItem -Path $dir.FullName -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        if ($size) { $totalFreed += $size }
        Remove-Item -Path $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
    } catch {}
}

$sizeMB = [math]::Round($totalFreed / 1MB, 2)
Write-Host "  ✓ 已清理 bin/obj 目录，释放 $sizeMB MB" -ForegroundColor Green

# 2. 清理日志文件
Write-Host ""
Write-Host "2. 清理日志文件 (logs)" -ForegroundColor Yellow
if (Test-Path 'logs') {
    $logSize = (Get-ChildItem -Path 'logs' -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
    if ($logSize) {
        $logSizeMB = [math]::Round($logSize / 1MB, 2)
        $totalFreed += $logSize
        Remove-Item -Path 'logs' -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  ✓ 已清理日志文件，释放 $logSizeMB MB" -ForegroundColor Green
    } else {
        Write-Host "  - 日志目录为空" -ForegroundColor Gray
    }
} else {
    Write-Host "  - 无日志文件" -ForegroundColor Gray
}

# 3. 清理 SQLite 数据文件（开发环境）
Write-Host ""
Write-Host "3. 清理 SQLite 数据文件" -ForegroundColor Yellow
if (Test-Path 'src/BobCrm.Api/data') {
    $dbSize = (Get-ChildItem -Path 'src/BobCrm.Api/data' -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
    if ($dbSize) {
        $dbSizeMB = [math]::Round($dbSize / 1MB, 2)
        $totalFreed += $dbSize
        Remove-Item -Path 'src/BobCrm.Api/data' -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  ✓ 已清理 SQLite 数据文件，释放 $dbSizeMB MB" -ForegroundColor Green
    } else {
        Write-Host "  - SQLite 数据目录为空" -ForegroundColor Gray
    }
} else {
    Write-Host "  - 无 SQLite 数据文件（使用PostgreSQL）" -ForegroundColor Gray
}

# 4. 清理测试覆盖率报告
Write-Host ""
Write-Host "4. 清理测试覆盖率报告" -ForegroundColor Yellow
if (Test-Path 'coverage-report') {
    $covSize = (Get-ChildItem -Path 'coverage-report' -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
    if ($covSize) {
        $covSizeMB = [math]::Round($covSize / 1MB, 2)
        $totalFreed += $covSize
    }
    Remove-Item -Path 'coverage-report' -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ 已清理测试覆盖率报告" -ForegroundColor Green
} else {
    Write-Host "  - 无测试覆盖率报告" -ForegroundColor Gray
}

# 5. 清理 .dev 临时文件
Write-Host ""
Write-Host "5. 清理 .dev 临时文件" -ForegroundColor Yellow
if (Test-Path '.dev') {
    Remove-Item -Path '.dev' -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ 已清理 .dev 临时文件" -ForegroundColor Green
} else {
    Write-Host "  - 无 .dev 临时文件" -ForegroundColor Gray
}

# 6. 清理 NuGet 包缓存
Write-Host ""
Write-Host "6. 清理 NuGet 包缓存" -ForegroundColor Yellow
dotnet nuget locals all --clear 2>&1 | Out-Null
Write-Host "  ✓ 已清理 NuGet 缓存" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
$totalMB = [math]::Round($totalFreed / 1MB, 2)
Write-Host "  清理完成！总共释放 $totalMB MB" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

