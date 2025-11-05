# BobCRM 环境验证脚本
# 用于验证系统是否正确配置并可以运行

$ErrorActionPreference = 'Continue'
$script:errors = @()
$script:warnings = @()

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

# 1. 检查前置条件
Write-Section "检查前置条件"

# 检查 .NET SDK
$dotnetVersion = $null
try {
    $dotnetVersion = (dotnet --version 2>$null)
    # 提取主版本号
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

# 2. 检查项目结构
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

# 3. 检查 PostgreSQL 连接
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
        # 测试数据库连接
        try {
            $testResult = docker exec bobcrm-pg pg_isready -U postgres 2>$null
            $dbReady = $?
            Write-Check "数据库就绪" $dbReady
        } catch {
            Write-Warn "无法测试数据库连接"
        }
    }
} else {
    Write-Warn "Docker未安装，跳过数据库检查" "请确保已安装并配置PostgreSQL"
}

# 4. 编译项目
Write-Section "编译项目"

Write-Host "正在编译..." -ForegroundColor Gray
$buildOutput = dotnet build BobCrm.sln -c Debug --nologo -v minimal 2>&1
$buildSuccess = $?

Write-Check "项目编译" $buildSuccess
if (-not $buildSuccess) {
    Write-Host "编译错误：" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor DarkRed
}

# 5. 检查端口占用
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

$port8080Free = Test-Port -port 8080
$port5200Free = Test-Port -port 5200

Write-Check "端口 8080 (前端) 可用" $port8080Free
Write-Check "端口 5200 (API) 可用" $port5200Free

# 6. 运行测试
Write-Section "运行测试"

if ((Test-Path "tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj")) {
    Write-Host "正在运行测试..." -ForegroundColor Gray
    $testOutput = dotnet test tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj --no-build --logger "console;verbosity=minimal" 2>&1
    $testSuccess = $?
    
    Write-Check "集成测试" $testSuccess
    if (-not $testSuccess) {
        Write-Host "测试输出：" -ForegroundColor Yellow
        Write-Host ($testOutput | Select-Object -Last 10) -ForegroundColor DarkYellow
    }
} else {
    Write-Warn "未找到测试项目" "跳过测试"
}

# 7. 总结
Write-Section "验证总结"

$totalChecks = $script:errors.Count + $script:warnings.Count
if ($script:errors.Count -eq 0 -and $script:warnings.Count -eq 0) {
    Write-Host "✓ 所有检查通过！系统已就绪。" -ForegroundColor Green
    Write-Host "`n下一步：" -ForegroundColor Cyan
    Write-Host "  1. 启动系统: pwsh scripts/dev.ps1 -Action start" -ForegroundColor White
    Write-Host "  2. 访问前端: http://localhost:8080" -ForegroundColor White
    Write-Host "  3. 使用管理员账号登录: admin / Admin@12345" -ForegroundColor White
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
    exit 1
}

