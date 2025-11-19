#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$DropTestDatabases,
    [switch]$NoBuild,
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

function Assert-DockerContainer {
    $container = (docker ps --filter 'name=bobcrm-postgres' --format '{{.ID}}' 2>$null)
    if (-not $container) {
        throw "未检测到 bobcrm-postgres 容器，请先执行 docker-compose up -d"
    }
}

function Invoke-DbCommand([string]$Sql) {
    docker exec bobcrm-postgres psql -U postgres -c $Sql | Out-Null
}

function Drop-TestDatabases {
    $query = "SELECT datname FROM pg_database WHERE datname LIKE 'bobcrm_test%'"
    $raw = docker exec bobcrm-postgres psql -U postgres -t -c $query 2>$null
    if (-not $raw) { return }
    $dbs = $raw -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    foreach ($db in $dbs) {
        Write-Host "  - 删除测试库: $db" -ForegroundColor DarkGray
        Invoke-DbCommand "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$db' AND pid <> pg_backend_pid();"
        Invoke-DbCommand "DROP DATABASE IF EXISTS \"$db\";"
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  重建 bobcrm 数据库" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Assert-DockerContainer

if ($DropTestDatabases) {
    Write-Host "清理所有 bobcrm_test* 数据库..." -ForegroundColor Yellow
    Drop-TestDatabases
}

Write-Host "终止连接并删除 bobcrm..." -ForegroundColor Yellow
Invoke-DbCommand "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'bobcrm' AND pid <> pg_backend_pid();"
Invoke-DbCommand "DROP DATABASE IF EXISTS bobcrm;"
Invoke-DbCommand "CREATE DATABASE bobcrm;"
Write-Host "bobcrm 数据库已重建" -ForegroundColor Green

Write-Host "运行 DatabaseInitializer (dotnet run --reset-db)..." -ForegroundColor Yellow
$project = Join-Path $repoRoot 'src/BobCrm.Api/BobCrm.Api.csproj'
$dotnetArgs = @('run','--project', $project,'--configuration', $Configuration)
if ($NoBuild) { $dotnetArgs += '--no-build' }
$dotnetArgs += '--'
$dotnetArgs += '--reset-db'

$process = Start-Process -FilePath 'dotnet' -ArgumentList $dotnetArgs -WorkingDirectory $repoRoot -NoNewWindow -PassThru -Wait
if ($process.ExitCode -ne 0) {
    throw "dotnet run --reset-db 执行失败，退出码 $($process.ExitCode)"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  数据库重建完成" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
