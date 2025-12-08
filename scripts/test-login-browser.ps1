<#
.SYNOPSIS
    浏览器自动化登录测试脚本
    
.DESCRIPTION
    按照 STD-06 集成测试规范和 TEST-05 登录认证集成测试规划，
    使用 Playwright 进行浏览器自动化测试，验证用户登录功能。
    
    测试用例：
    - TC-01: 后端 API 直接登录 - 正确凭据
    - TC-02: 后端 API 直接登录 - 错误密码
    - TC-03: 前端代理登录 - 正确凭据
    - TC-04: 前端代理登录 - 错误用户名
    - TC-05: 浏览器 UI 登录流程
    
.EXAMPLE
    ./scripts/test-login-browser.ps1
#>

$ErrorActionPreference = "Stop"

# 检查 Playwright 是否已安装
$playwrightInstalled = $false
try {
    $null = Get-Command playwright -ErrorAction Stop
    $playwrightInstalled = $true
} catch {
    Write-Host "Playwright 未安装，将尝试安装..." -ForegroundColor Yellow
}

# 如果未安装，尝试安装 Playwright
if (-not $playwrightInstalled) {
    Write-Host "正在安装 Playwright..." -ForegroundColor Cyan
    try {
        npm install -g playwright
        playwright install chromium
        $playwrightInstalled = $true
    } catch {
        Write-Host "无法安装 Playwright，将使用 PowerShell 直接打开浏览器进行手动测试" -ForegroundColor Yellow
    }
}

Write-Host "=== BobCRM 登录集成测试（浏览器自动化）===" -ForegroundColor Cyan
Write-Host "时间: $(Get-Date)"
Write-Host ""

# 测试结果记录
$testResults = @{}

# TC-01: 后端 API 直接登录 - 正确凭据
Write-Host "[TC-01] 测试后端 API 直接登录（正确凭据）..." -ForegroundColor Yellow
try {
    $body = @{ username = "admin"; password = "Admin@12345" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "http://localhost:5200/api/auth/login" -Method POST -Body $body -ContentType "application/json"
    
    if ($response.data.accessToken -and $response.data.user.userName -eq "admin") {
        Write-Host "  ✓ TC-01 通过: 后端登录成功" -ForegroundColor Green
        $testResults["TC-01"] = "PASS"
    } else {
        Write-Host "  ✗ TC-01 失败: 响应格式不正确" -ForegroundColor Red
        $testResults["TC-01"] = "FAIL"
    }
} catch {
    Write-Host "  ✗ TC-01 失败: $($_.Exception.Message)" -ForegroundColor Red
    $testResults["TC-01"] = "FAIL"
}

# TC-02: 后端 API 直接登录 - 错误密码
Write-Host "[TC-02] 测试后端 API 直接登录（错误密码）..." -ForegroundColor Yellow
try {
    $body = @{ username = "admin"; password = "wrongpassword" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "http://localhost:5200/api/auth/login" -Method POST -Body $body -ContentType "application/json"
    Write-Host "  ✗ TC-02 失败: 应该返回 401 错误" -ForegroundColor Red
    $testResults["TC-02"] = "FAIL"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Write-Host "  ✓ TC-02 通过: 错误密码被正确拒绝" -ForegroundColor Green
        $testResults["TC-02"] = "PASS"
    } else {
        Write-Host "  ✗ TC-02 失败: 返回状态码 $statusCode，期望 401" -ForegroundColor Red
        $testResults["TC-02"] = "FAIL"
    }
}

# TC-03: 前端代理登录 - 正确凭据
Write-Host "[TC-03] 测试前端代理登录（正确凭据）..." -ForegroundColor Yellow
try {
    $body = @{ username = "admin"; password = "Admin@12345" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "http://localhost:3000/api/auth/login" -Method POST -Body $body -ContentType "application/json"
    
    if ($response.data.accessToken -and $response.data.user) {
        Write-Host "  ✓ TC-03 通过: 前端代理登录成功" -ForegroundColor Green
        $testResults["TC-03"] = "PASS"
    } else {
        Write-Host "  ✗ TC-03 失败: 响应格式不正确" -ForegroundColor Red
        $testResults["TC-03"] = "FAIL"
    }
} catch {
    Write-Host "  ✗ TC-03 失败: $($_.Exception.Message)" -ForegroundColor Red
    $testResults["TC-03"] = "FAIL"
}

# TC-04: 前端代理登录 - 错误用户名
Write-Host "[TC-04] 测试前端代理登录（错误用户名）..." -ForegroundColor Yellow
try {
    $body = @{ username = "nonexistent"; password = "anypassword" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "http://localhost:3000/api/auth/login" -Method POST -Body $body -ContentType "application/json"
    Write-Host "  ✗ TC-04 失败: 应该返回 401 错误" -ForegroundColor Red
    $testResults["TC-04"] = "FAIL"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Write-Host "  ✓ TC-04 通过: 错误用户名被正确拒绝" -ForegroundColor Green
        $testResults["TC-04"] = "PASS"
    } else {
        Write-Host "  ✗ TC-04 失败: 返回状态码 $statusCode，期望 401" -ForegroundColor Red
        $testResults["TC-04"] = "FAIL"
    }
}

# TC-05: 浏览器 UI 登录流程
Write-Host "[TC-05] 测试浏览器 UI 登录流程..." -ForegroundColor Yellow

if ($playwrightInstalled) {
    # 使用 Playwright 进行自动化测试
    Write-Host "  使用 Playwright 进行浏览器自动化测试..." -ForegroundColor Cyan
    
    $playwrightScript = @"
const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false });
    const page = await browser.newPage();
    
    try {
        // 导航到登录页面
        await page.goto('http://localhost:3000/login');
        await page.waitForLoadState('networkidle');
        
        // 输入用户名
        await page.fill('input[type="text"], input[name="username"], input[placeholder*="用户名"], input[placeholder*="username"]', 'admin');
        
        // 输入密码
        await page.fill('input[type="password"], input[name="password"], input[placeholder*="密码"], input[placeholder*="password"]', 'Admin@12345');
        
        // 点击登录按钮
        await page.click('button[type="submit"], button:has-text("登录"), button:has-text("Login"), button:has-text("ログイン")');
        
        // 等待页面跳转（最多等待10秒）
        await page.waitForURL('**/dashboard', { timeout: 10000 }).catch(() => page.waitForURL('**/', { timeout: 10000 }));
        
        // 检查是否成功登录（检查URL或页面元素）
        const currentUrl = page.url();
        const hasUserInfo = await page.locator('[class*="user"], [class*="User"], [id*="user"], [id*="User"]').count() > 0;
        
        if (currentUrl.includes('/dashboard') || currentUrl === 'http://localhost:3000/' || hasUserInfo) {
            console.log('SUCCESS: 登录成功，页面已跳转');
            process.exit(0);
        } else {
            console.log('FAILED: 登录后页面未正确跳转');
            process.exit(1);
        }
    } catch (error) {
        console.log('FAILED: ' + error.message);
        process.exit(1);
    } finally {
        await browser.close();
    }
})();
"@
    
    $scriptPath = Join-Path $env:TEMP "playwright-login-test.js"
    $playwrightScript | Out-File -FilePath $scriptPath -Encoding UTF8
    
    try {
        node $scriptPath
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ TC-05 通过: 浏览器 UI 登录流程成功" -ForegroundColor Green
            $testResults["TC-05"] = "PASS"
        } else {
            Write-Host "  ✗ TC-05 失败: 浏览器自动化测试失败" -ForegroundColor Red
            $testResults["TC-05"] = "FAIL"
        }
    } catch {
        Write-Host "  ✗ TC-05 失败: $($_.Exception.Message)" -ForegroundColor Red
        $testResults["TC-05"] = "FAIL"
    } finally {
        if (Test-Path $scriptPath) {
            Remove-Item $scriptPath -Force
        }
    }
} else {
    # 使用 PowerShell 打开浏览器进行手动测试
    Write-Host "  打开浏览器进行手动测试..." -ForegroundColor Cyan
    Write-Host "  请在浏览器中完成以下步骤：" -ForegroundColor Yellow
    Write-Host "    1. 导航到 http://localhost:3000/login" -ForegroundColor White
    Write-Host "    2. 输入用户名: admin" -ForegroundColor White
    Write-Host "    3. 输入密码: Admin@12345" -ForegroundColor White
    Write-Host "    4. 点击登录按钮" -ForegroundColor White
    Write-Host "    5. 验证页面跳转到仪表板" -ForegroundColor White
    Write-Host "    6. 验证用户名显示在页面右上角" -ForegroundColor White
    
    Start-Process "http://localhost:3000/login"
    
    $response = Read-Host "`n测试完成后，请输入结果 (PASS/FAIL)"
    if ($response -eq "PASS" -or $response -eq "pass") {
        Write-Host "  ✓ TC-05 通过: 浏览器 UI 登录流程成功" -ForegroundColor Green
        $testResults["TC-05"] = "PASS"
    } else {
        Write-Host "  ✗ TC-05 失败: 浏览器 UI 登录流程失败" -ForegroundColor Red
        $testResults["TC-05"] = "FAIL"
    }
}

# 输出测试总结
Write-Host ""
Write-Host "=== 测试总结 ===" -ForegroundColor Cyan
foreach ($test in $testResults.Keys | Sort-Object) {
    $result = $testResults[$test]
    $color = if ($result -eq "PASS") { "Green" } else { "Red" }
    Write-Host "  $test : $result" -ForegroundColor $color
}

$passedCount = ($testResults.Values | Where-Object { $_ -eq "PASS" }).Count
$totalCount = $testResults.Count

Write-Host ""
if ($passedCount -eq $totalCount) {
    Write-Host "✓ 所有测试通过 ($passedCount/$totalCount)" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ 部分测试失败 ($passedCount/$totalCount 通过)" -ForegroundColor Red
    exit 1
}

