#!/usr/bin/env pwsh
<#
.SYNOPSIS
    运行测试套件并处理超时和僵尸进程

.DESCRIPTION
    这个脚本用于运行 .NET 测试，提供以下功能：
    - 设置超时时间（默认10分钟）
    - 自动清理僵尸 testhost 进程
    - 传递正确的退出码给 CI/CD
    - 支持指定测试项目或完整解决方案

.PARAMETER Target
    测试目标，可以是：
    - "solution" (默认): 运行整个解决方案的测试
    - "api": 只运行 API 测试
    - "app": 只运行 App 测试
    - 或指定完整的 .csproj 路径

.PARAMETER TimeoutMinutes
    超时时间（分钟），默认10分钟

.PARAMETER Verbose
    显示详细输出

.EXAMPLE
    # 运行所有测试，默认10分钟超时
    pwsh scripts/run-tests-with-timeout.ps1

.EXAMPLE
    # 只运行 API 测试，15分钟超时
    pwsh scripts/run-tests-with-timeout.ps1 -Target api -TimeoutMinutes 15

.EXAMPLE
    # 运行所有测试，显示详细输出
    pwsh scripts/run-tests-with-timeout.ps1 -Verbose
#>

param(
    [string]$Target = "solution",
    [int]$TimeoutMinutes = 10,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $PSScriptRoot
Set-Location $scriptRoot

# 颜色输出函数
function Write-ColorOutput {
    param([string]$Message, [ConsoleColor]$Color = [ConsoleColor]::White)
    Write-Host $Message -ForegroundColor $Color
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "========================================" Cyan
    Write-ColorOutput "  $Title" Cyan
    Write-ColorOutput "========================================" Cyan
    Write-Host ""
}

# 清理僵尸进程
function Stop-TestHostProcesses {
    Write-ColorOutput "清理 testhost 进程..." Yellow
    $processes = Get-Process -Name testhost -ErrorAction SilentlyContinue
    if ($processes) {
        Write-ColorOutput "发现 $($processes.Count) 个 testhost 进程，正在终止..." Yellow
        $processes | ForEach-Object {
            try {
                Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
                Write-ColorOutput "  已终止进程 PID=$($_.Id)" Gray
            }
            catch {
                Write-ColorOutput "  无法终止进程 PID=$($_.Id): $_" Red
            }
        }
    }
    else {
        Write-ColorOutput "没有发现残留的 testhost 进程" Green
    }
}

# 确定测试目标
function Get-TestTarget {
    param([string]$Target)

    switch ($Target.ToLower()) {
        "solution" { return "BobCrm.sln" }
        "api" { return "tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj" }
        "app" { return "tests/BobCrm.App.Tests/BobCrm.App.Tests.csproj" }
        default {
            if (Test-Path $Target) {
                return $Target
            }
            else {
                throw "无效的测试目标: $Target"
            }
        }
    }
}

Write-Section "测试执行配置"
Write-ColorOutput "目标: $Target" White
Write-ColorOutput "超时: $TimeoutMinutes 分钟" White
Write-ColorOutput "工作目录: $scriptRoot" White

# 第一步：清理之前的僵尸进程
Write-Section "清理残留进程"
Stop-TestHostProcesses

# 第二步：确定测试目标
$testTarget = Get-TestTarget -Target $Target
Write-Section "运行测试"
Write-ColorOutput "测试目标: $testTarget" White
Write-ColorOutput "超时设置: $TimeoutMinutes 分钟 ($($TimeoutMinutes * 60) 秒)" White

# 构建测试命令
$testArgs = @(
    "test"
    $testTarget
    "--no-build"
    "--no-restore"
    "-v"
    "normal"
)

if ($Verbose) {
    $testArgs += "-v"
    $testArgs += "detailed"
}

# 创建临时输出文件
$tempOutput = Join-Path $env:TEMP "bobcrm-test-output-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

Write-ColorOutput "测试输出将记录到: $tempOutput" Gray
Write-Host ""

# 启动测试进程
$timeoutSeconds = $TimeoutMinutes * 60
$startTime = Get-Date

try {
    # 使用 Start-Process 以便可以控制超时
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = "dotnet"
    $processInfo.Arguments = $testArgs -join " "
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.WorkingDirectory = $scriptRoot

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo

    # 输出事件处理
    $outputBuilder = New-Object System.Text.StringBuilder
    $errorBuilder = New-Object System.Text.StringBuilder

    $outputHandler = {
        if ($EventArgs.Data) {
            Write-Host $EventArgs.Data
            [void]$Event.MessageData.AppendLine($EventArgs.Data)
        }
    }

    $errorHandler = {
        if ($EventArgs.Data) {
            Write-Host $EventArgs.Data -ForegroundColor Red
            [void]$Event.MessageData.AppendLine($EventArgs.Data)
        }
    }

    $outputEvent = Register-ObjectEvent -InputObject $process -EventName OutputDataReceived -Action $outputHandler -MessageData $outputBuilder
    $errorEvent = Register-ObjectEvent -InputObject $process -EventName ErrorDataReceived -Action $errorHandler -MessageData $errorBuilder

    # 启动进程
    [void]$process.Start()
    $process.BeginOutputReadLine()
    $process.BeginErrorReadLine()

    # 等待进程完成或超时
    $completed = $process.WaitForExit($timeoutSeconds * 1000)

    # 取消事件注册
    Unregister-Event -SourceIdentifier $outputEvent.Name -Force
    Unregister-Event -SourceIdentifier $errorEvent.Name -Force

    if (-not $completed) {
        # 超时
        Write-ColorOutput "" Red
        Write-ColorOutput "========================================" Red
        Write-ColorOutput "  测试超时！（$TimeoutMinutes 分钟）" Red
        Write-ColorOutput "========================================" Red
        Write-ColorOutput "" Red

        # 杀掉测试进程
        if (-not $process.HasExited) {
            Write-ColorOutput "正在终止测试进程..." Yellow
            $process.Kill($true)  # Kill entire process tree
        }

        # 保存输出到文件
        $outputBuilder.ToString() | Out-File -FilePath $tempOutput -Encoding UTF8
        Write-ColorOutput "测试输出已保存到: $tempOutput" Gray

        $exitCode = -1
    }
    else {
        # 正常完成
        $endTime = Get-Date
        $duration = $endTime - $startTime

        Write-Host ""
        Write-ColorOutput "========================================" Cyan
        Write-ColorOutput "  测试执行完成" Cyan
        Write-ColorOutput "========================================" Cyan
        Write-ColorOutput "执行时间: $($duration.ToString('mm\:ss'))" White
        Write-ColorOutput "退出码: $($process.ExitCode)" $(if ($process.ExitCode -eq 0) { [ConsoleColor]::Green } else { [ConsoleColor]::Red })

        # 保存输出到文件
        $outputBuilder.ToString() | Out-File -FilePath $tempOutput -Encoding UTF8
        Write-ColorOutput "测试输出已保存到: $tempOutput" Gray

        $exitCode = $process.ExitCode
    }
}
catch {
    Write-ColorOutput "" Red
    Write-ColorOutput "========================================" Red
    Write-ColorOutput "  测试执行失败" Red
    Write-ColorOutput "========================================" Red
    Write-ColorOutput "错误: $_" Red
    Write-ColorOutput "" Red
    $exitCode = 1
}
finally {
    # 第三步：无论如何都清理僵尸进程
    Write-Section "清理测试进程"
    Stop-TestHostProcesses

    # 清理临时文件（可选，如果测试失败则保留）
    if ($exitCode -eq 0 -and (Test-Path $tempOutput)) {
        Write-ColorOutput "删除临时输出文件..." Gray
        Remove-Item $tempOutput -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
if ($exitCode -eq 0) {
    Write-ColorOutput "✓ 测试全部通过" Green
}
else {
    Write-ColorOutput "✗ 测试失败或超时" Red
    Write-ColorOutput "退出码: $exitCode" Red
}
Write-Host ""

exit $exitCode
