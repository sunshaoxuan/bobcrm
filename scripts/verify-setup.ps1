# BobCRM 环境验证脚本 - 增强版
# 用于验证系统是否正确配置并可以运行
# 所有输出将保存到日志文件

$ErrorActionPreference = 'Continue'
$script:errors = @()
$script:warnings = @()

# 创建日志文件（每次新建）
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logDir = "logs"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}
$logFile = "$logDir/verify-$timestamp.log"

$frontendPort = 3000
$apiPort = 5200

# 启动日志记录
Start-Transcript -Path $logFile -Force

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BobCRM 环境验证" -ForegroundColor Cyan
Write-Host "  日志文件: $logFile" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

function Write-Section {
    param([string]$title)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  $title" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Check {
    param([string]$message, [bool]$result, [string]$details = "")
    if ($result) {
        Write-Host "[✓] $message" -ForegroundColor Green
        if ($details) { Write-Host "    $details" -ForegroundColor DarkGray }
    } else {
        Write-Host "[✗] $message" -ForegroundColor Red
        if ($details) { Write-Host "    $details" -ForegroundColor Yellow }
        $script:errors += $message
    }
}

function Write-Warn {
    param([string]$message, [string]$details = "")
    Write-Host "[!] $message" -ForegroundColor Yellow
    if ($details) { Write-Host "    $details" -ForegroundColor DarkGray }
    $script:warnings += $message
}

# ========================================
# 步骤 0: Git 同步（已禁用 - 避免丢失本地修改）
# ========================================
# 警告：以下Git同步步骤已被注释，因为会导致本地未提交的修改丢失！
# 如果需要强制同步，请手动执行以下命令：
#   git reset --hard HEAD
#   git clean -fdx -e logs/
#   git pull origin main
# ========================================
<#
Write-Section "Git 同步"

Write-Host "正在放弃本地所有修改..." -ForegroundColor Gray
try {
    # 停止所有运行中的服务
    Write-Host "停止运行中的服务..." -ForegroundColor Gray
    Get-Process | Where-Object {$_.ProcessName -like "BobCrm*"} | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    # 放弃所有本地修改
    git reset --hard HEAD 2>&1 | Out-Null
    
    # 清理未跟踪的文件（排除 logs 目录）
    git clean -fdx -e logs/ 2>&1 | Out-Null
    
    Write-Host "正在拉取远程代码..." -ForegroundColor Gray
    $gitPull = git pull origin main 2>&1
    Write-Host $gitPull
    
    $currentCommit = git rev-parse --short HEAD
    Write-Check "Git 同步" $true "当前提交: $currentCommit"
} catch {
    Write-Check "Git 同步" $false $_.Exception.Message
}
#>

# ========================================
# 步骤 1: 检查前置条件
# ========================================
Write-Section "检查前置条件"

# 检查 .NET SDK
$dotnetVersion = $null
try {
    $dotnetVersion = (dotnet --version 2>$null)
    if ($dotnetVersion -match '^(\d+)\.') {
        $majorVersion = [int]$Matches[1]
        $isNet8Plus = $majorVersion -ge 8
        Write-Check ".NET 8+ SDK" $isNet8Plus "版本: $dotnetVersion"
        if (-not $isNet8Plus) {
            Write-Host "    本项目需要 .NET 8 或更高版本" -ForegroundColor Yellow
            Write-Host "    请从 https://dotnet.microsoft.com/download/dotnet 下载安装" -ForegroundColor Yellow
        } elseif ($majorVersion -gt 8) {
            Write-Host "    .NET $majorVersion 向后兼容 .NET 8 项目 ✓" -ForegroundColor DarkGray
        }
    } else {
        Write-Check ".NET 8+ SDK" $false "版本格式无法识别: $dotnetVersion"
    }
} catch {
    Write-Check ".NET 8+ SDK" $false "未安装"
}

# 检查 PowerShell 版本
$psVersion = $PSVersionTable.PSVersion
$isPwsh7 = $psVersion.Major -ge 7
Write-Check "PowerShell 7+" $isPwsh7 "版本: $psVersion"

# 检查 Docker
$dockerInstalled = $false
try {
    $dockerVersion = (docker --version 2>$null)
    $dockerInstalled = $?
    Write-Check "Docker" $dockerInstalled "版本: $dockerVersion"
} catch {
    Write-Check "Docker" $false "未安装（可选，可用本地PostgreSQL替代）"
}

# ========================================
# 步骤 2: 检查项目结构
# ========================================
Write-Section "检查项目结构"

$requiredFiles = @(
    "BobCrm.sln",
    "src/BobCrm.Api/BobCrm.Api.csproj",
    "src/BobCrm.App/BobCrm.App.csproj",
    "docker-compose.yml",
    "scripts/dev.ps1"
)

foreach ($file in $requiredFiles) {
    $exists = Test-Path $file
    Write-Check $file $exists
}

# ========================================
# 步骤 3: 检查数据库
# ========================================
Write-Section "检查数据库"

if ($dockerInstalled) {
    $pgRunning = $false
    try {
        $containers = docker ps --format "{{.Names}}" 2>$null
        $pgRunning = $containers -contains "bobcrm-pg"
        Write-Check "PostgreSQL容器运行中" $pgRunning
        
        if (-not $pgRunning) {
            Write-Host "    提示：运行 'docker compose up -d' 启动数据库" -ForegroundColor Yellow
        }
    } catch {
        Write-Warn "无法检查Docker容器状态" "运行 'docker compose up -d' 启动数据库"
    }
    
    if ($pgRunning) {
        try {
            docker exec bobcrm-pg pg_isready -U postgres 2>$null | Out-Null
            $dbReady = $?
            Write-Check "数据库就绪" $dbReady
        } catch {
            Write-Warn "无法测试数据库连接"
        }
    }
} else {
    Write-Warn "Docker未安装，跳过数据库检查" "请确保已安装并配置PostgreSQL"
}

# ========================================
# 步骤 4: 清理并编译项目
# ========================================
Write-Section "清理并编译项目"

Write-Host "正在清理..." -ForegroundColor Gray
$cleanOutput = dotnet clean BobCrm.sln --nologo -v minimal 2>&1
Write-Host $cleanOutput

Write-Host "`n正在编译..." -ForegroundColor Gray
$buildOutput = dotnet build BobCrm.sln -c Debug --nologo 2>&1
$buildSuccess = $LASTEXITCODE -eq 0

Write-Check "项目编译" $buildSuccess
if (-not $buildSuccess) {
    Write-Host "`n完整编译输出：" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor DarkRed
}

# ========================================
# 步骤 5: 检查端口占用
# ========================================
Write-Section "检查端口"

function Test-Port {
    param([int]$port)
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)
        $listener.Start()
        $listener.Stop()
        return $true
    } catch {
        return $false
    }
}

$portFrontendFree = Test-Port -port $frontendPort
$portApiFree = Test-Port -port $apiPort

Write-Check "端口 $frontendPort (前端) 可用" $portFrontendFree
Write-Check "端口 $apiPort (API) 可用" $portApiFree

# ========================================
# 步骤 6: 运行测试
# ========================================
Write-Section "运行测试"

if ((Test-Path "tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj")) {
    Write-Host "正在运行测试..." -ForegroundColor Gray
    $testOutput = dotnet test tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj --no-build --logger "console;verbosity=normal" 2>&1
    $testSuccess = $LASTEXITCODE -eq 0
    
    Write-Check "集成测试" $testSuccess
    if (-not $testSuccess) {
        Write-Host "`n完整测试输出：" -ForegroundColor Yellow
        Write-Host $testOutput -ForegroundColor DarkYellow
    }
} else {
    Write-Warn "未找到测试项目" "跳过测试"
}

# ========================================
# 步骤 7: I18n 多语言合规性检查
# ========================================
Write-Section "I18n 多语言合规性检查"

if (Test-Path "scripts/check-i18n.ps1") {
    Write-Host "正在检查硬编码字符串...`n" -ForegroundColor Gray
    
    # 运行 i18n 检查，仅检查 ERROR 级别
    try {
        # 生成 i18n 日志文件路径
        $i18nLogFile = "$logDir/i18n-check-$timestamp.log"
        
        # 捕获输出并检查退出码
        $i18nOutput = & "scripts/check-i18n.ps1" -Severity ERROR -CI -LogFile $i18nLogFile 2>&1
        $i18nSuccess = $LASTEXITCODE -eq 0
        
        # 显示输出（包含统计信息）
        Write-Host $i18nOutput
        
        if ($i18nSuccess) {
            Write-Check "I18n 合规性（ERROR 级别）" $true "无 ERROR 级别违规"
        } else {
            Write-Check "I18n 合规性（ERROR 级别）" $false "发现 ERROR 级别违规（硬编码字符串）"
            Write-Host "`n  💡 修复建议：" -ForegroundColor Yellow
            Write-Host "     1. 运行: pwsh scripts/check-i18n.ps1 --Fix" -ForegroundColor Gray
            Write-Host "     2. 查看: docs/process/STD-05-多语言开发规范.md" -ForegroundColor Gray
            Write-Host "     3. 导出清单: pwsh scripts/check-i18n.ps1 --Output violations.csv" -ForegroundColor Gray
            Write-Host "     4. 查看详细日志: $i18nLogFile`n" -ForegroundColor Gray
        }
    } catch {
        Write-Warn "I18n 检查执行失败" $_.Exception.Message
    }
} else {
    Write-Warn "未找到 I18n 检查脚本" "跳过多语言检查"
}

# ========================================
# 步骤 8: 总结
# ========================================
Write-Section "验证总结"
# ========================================
Write-Section "验证总结"

if ($script:errors.Count -eq 0 -and $script:warnings.Count -eq 0) {
    Write-Host "✓ 所有检查通过！系统已就绪。" -ForegroundColor Green
    Write-Host "`n下一步：" -ForegroundColor Cyan
    Write-Host "  1. 启动系统: pwsh scripts/run.ps1" -ForegroundColor White
    Write-Host "  2. 访问前端: http://localhost:$frontendPort" -ForegroundColor White
    Write-Host "  3. 使用管理员账号登录: admin / Admin@12345" -ForegroundColor White
    Write-Host "`n日志已保存到: $logFile" -ForegroundColor Cyan
    Stop-Transcript
    exit 0
} else {
    if ($script:errors.Count -gt 0) {
        Write-Host "`n发现 $($script:errors.Count) 个错误：" -ForegroundColor Red
        foreach ($err in $script:errors) {
            Write-Host "  - $err" -ForegroundColor Red
        }
    }
    
    if ($script:warnings.Count -gt 0) {
        Write-Host "`n发现 $($script:warnings.Count) 个警告：" -ForegroundColor Yellow
        foreach ($warn in $script:warnings) {
            Write-Host "  - $warn" -ForegroundColor Yellow
        }
    }
    
    Write-Host "`n请解决上述问题后再次运行此脚本。" -ForegroundColor Yellow
    Write-Host "完整日志已保存到: $logFile" -ForegroundColor Cyan
    Stop-Transcript
    exit 1
}
