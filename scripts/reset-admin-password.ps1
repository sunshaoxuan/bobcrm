#!/usr/bin/env pwsh
# 重置 admin 用户密码脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  重置 Admin 密码" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "当前 admin 用户的默认密码是: " -ForegroundColor Yellow -NoNewline
Write-Host "Admin@12345" -ForegroundColor Green
Write-Host ""
Write-Host "如果您修改过密码但忘记了，可以通过以下方式重置：" -ForegroundColor Yellow
Write-Host ""
Write-Host "方法1：使用 API 端点（需要知道当前密码）" -ForegroundColor Cyan
Write-Host "  访问个人中心页面: http://localhost:3000/profile" -ForegroundColor White
Write-Host "  点击'修改密码'，输入当前密码和新密码" -ForegroundColor White
Write-Host ""
Write-Host "方法2：重建开发数据库（会丢失所有数据）" -ForegroundColor Cyan
Write-Host "  1. 停止服务: " -ForegroundColor White -NoNewline
Write-Host "pwsh scripts/dev.ps1 -Action stop" -ForegroundColor Gray
Write-Host "  2. 删除数据库: " -ForegroundColor White -NoNewline
Write-Host "docker exec bobcrm-pg psql -U postgres -c 'DROP DATABASE IF EXISTS bobcrm'" -ForegroundColor Gray
Write-Host "  3. 创建新数据库: " -ForegroundColor White -NoNewline
Write-Host "docker exec bobcrm-pg psql -U postgres -c 'CREATE DATABASE bobcrm'" -ForegroundColor Gray
Write-Host "  4. 重启服务: " -ForegroundColor White -NoNewline
Write-Host "pwsh scripts/dev.ps1 -Action start" -ForegroundColor Gray
Write-Host ""
Write-Host "方法3：使用 Admin 端点重置（开发环境，需要登录）" -ForegroundColor Cyan
Write-Host "  如果能登录，访问: POST http://localhost:5200/api/admin/reset-password" -ForegroundColor White
Write-Host '  Body: {"newPassword":"NewPassword@123"}' -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  数据库状态检查" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查数据库中的用户
try {
    Write-Host "开发数据库 (bobcrm) 中的用户：" -ForegroundColor Yellow
    $users = docker exec bobcrm-pg psql -U postgres -d bobcrm -t -c 'SELECT "UserName", "Email", "EmailConfirmed" FROM "AspNetUsers" ORDER BY "UserName";' 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host $users -ForegroundColor Gray
        Write-Host ""
        Write-Host "✓ 数据库中有用户数据" -ForegroundColor Green
    } else {
        Write-Host "[错误] 无法读取用户数据" -ForegroundColor Red
        Write-Host $users -ForegroundColor Gray
    }
} catch {
    Write-Host "[错误] 数据库查询失败: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  重要说明" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ 开发环境的数据是持久化的" -ForegroundColor Green
Write-Host "✓ 重启服务不会重置密码" -ForegroundColor Green
Write-Host "✓ 只有首次启动或数据库为空时才会初始化" -ForegroundColor Green
Write-Host ""
Write-Host "如果您忘记了密码，建议使用方法2重建数据库。" -ForegroundColor Yellow
Write-Host ""
