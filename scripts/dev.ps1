Param(
    [ValidateSet('start','stop','restart')]
    [string]$Action = 'start',
    [string]$Configuration = 'Debug',
    [switch]$NoBuild,
    [switch]$NoLogs,
    [switch]$Detached,
    [switch]$ShowWindows
)

$ErrorActionPreference = 'Stop'

# 统一控制台编码为 UTF-8，避免外部进程输出出现乱码
try {
    [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
    [Console]::InputEncoding  = [System.Text.UTF8Encoding]::new($false)
} catch {}

function Resolve-RepoRoot {
    $scriptDir = $null
    if ($PSScriptRoot) {
        $scriptDir = $PSScriptRoot
    } elseif ($PSCommandPath) {
        $scriptDir = Split-Path -Parent $PSCommandPath
    } elseif ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    } else {
        # 最后回退：当前工作目录下的 scripts 作为基准
        $scriptDir = (Resolve-Path '.').Path
    }
    return (Resolve-Path (Join-Path $scriptDir '..')).Path
}

function Get-Paths {
    param([string]$repoRoot)
    @{
        ApiProj = Join-Path $repoRoot 'src/BobCrm.Api/BobCrm.Api.csproj'
        AppProj = Join-Path $repoRoot 'src/BobCrm.App/BobCrm.App.csproj'
        WorkDir = $repoRoot
        VarDir  = Join-Path $repoRoot '.dev'
        LogDir  = Join-Path $repoRoot 'logs'
        PidFile = Join-Path $repoRoot '.dev/pids.json'
    }
}

function Initialize-Dirs {
    param($paths)
    foreach ($d in @($paths.VarDir, $paths.LogDir)) {
        if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d | Out-Null }
    }
}

function Build-SolutionIfNeeded {
    param([string]$repoRoot, [switch]$NoBuild, [string]$Configuration)
    if ($NoBuild) { return }
    Write-Host "正在构建解决方案（$Configuration）..." -ForegroundColor Cyan
    & dotnet build (Join-Path $repoRoot 'BobCrm.sln') -c $Configuration | Write-Host
}

function New-LogFiles {
    param($paths)
    $ts = (Get-Date).ToString('yyyyMMdd_HHmmss')
    @{
        ApiOut = Join-Path $paths.LogDir "api_$ts.out.log"
        ApiErr = Join-Path $paths.LogDir "api_$ts.err.log"
        AppOut = Join-Path $paths.LogDir "app_$ts.out.log"
        AppErr = Join-Path $paths.LogDir "app_$ts.err.log"
    }
}

function Restart-DockerServices {
    param([string]$repoRoot)
    $composeFile = Join-Path $repoRoot 'docker-compose.yml'
    if (-not (Test-Path $composeFile)) {
        Write-Host '未检测到 docker-compose.yml，跳过容器启动步骤。' -ForegroundColor DarkGray
        return
    }
    Write-Host '重启 Docker 容器 (postgres, minio)...' -ForegroundColor Cyan
    $baseArgs = @('compose','-f', $composeFile)
    try {
        Write-Host '清理旧容器...' -ForegroundColor DarkGray
        & docker @($baseArgs + @('down','--remove-orphans')) | Out-Null
        $baseArgs = @('compose','-f', $composeFile)
        & docker @($baseArgs + @('up','-d','--remove-orphans','--force-recreate','postgres','minio')) | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "docker compose up postgres/minio 失败" }
        & docker @($baseArgs + @('up','-d','minio-create-bucket')) | Out-Null
        Write-Host 'Docker 容器已就绪。' -ForegroundColor DarkGreen
    }
    catch {
        throw "Docker 容器启动失败：$($_.Exception.Message)`n请确认 9100/9101 端口未被占用，然后重新运行脚本。"
    }
}

function Start-ServiceProcess {
    param(
        [string]$name,
        [string]$proj,
        [string]$configuration,
        [switch]$NoLogs,
        [switch]$ShowWindows,
        [hashtable]$logFiles
    )
    $runArgs = @('run','--project', $proj, '--configuration', $configuration)
    $workDir = (Split-Path $proj -Parent)
    if ($ShowWindows) {
        # 在新的 PowerShell 窗口前台运行，保留屏幕输出
        $psArgs = @('-NoLogo','-NoExit','-Command', "Set-Location '$workDir'; dotnet $($runArgs -join ' ')")
        $shell = Start-Process -FilePath 'pwsh' -ArgumentList $psArgs -PassThru -WorkingDirectory $workDir
        # 尝试捕获 dotnet 子进程 PID（轮询几秒）
        $dotnetPid = $null
        try {
            for ($i=0; $i -lt 30 -and -not $dotnetPid; $i++) {
                $children = Get-CimInstance Win32_Process -Filter "ParentProcessId = $($shell.Id)"
                $dn = $children | Where-Object { $_.Name -match '^dotnet(\.exe)?$' } | Select-Object -First 1
                if ($dn) { $dotnetPid = [int]$dn.ProcessId; break }
                Start-Sleep -Milliseconds 200
            }
        } catch {}
        return @{ ShellId = $shell.Id; DotnetId = $dotnetPid }
    }
    if ($NoLogs) {
        $p = Start-Process -FilePath 'dotnet' -ArgumentList $runArgs -PassThru -WorkingDirectory $workDir
    } else {
        $out = $logFiles["$name`Out"]
        $err = $logFiles["$name`Err"]
        $p = Start-Process -FilePath 'dotnet' -ArgumentList $runArgs -PassThru -RedirectStandardOutput $out -RedirectStandardError $err -WorkingDirectory $workDir
    }
    return @{ ShellId = $null; DotnetId = $p.Id }
}

function Save-Pids {
    param([string]$pidFile, [hashtable]$pids)
    $json = $pids | ConvertTo-Json -Depth 3
    Set-Content -Path $pidFile -Value $json -Encoding UTF8
}

function Get-Pids {
    param([string]$pidFile)
    if (-not (Test-Path $pidFile)) { return $null }
    try { return Get-Content $pidFile -Raw | ConvertFrom-Json } catch { return $null }
}

function Stop-ProcessSafe {
    param([int]$processId)
    try {
        if ($processId -and (Get-Process -Id $processId -ErrorAction SilentlyContinue)) {
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        }
    } catch {}
}

function Get-IdsFromNode {
    param($node)
    $ids = @()
    if ($null -eq $node) { return $ids }
    if ($node -is [int]) { return @([int]$node) }
    if ($node.PSObject.Properties.Name -contains 'DotnetId' -and $node.DotnetId) { $ids += [int]$node.DotnetId }
    if ($node.PSObject.Properties.Name -contains 'ShellId' -and $node.ShellId) { $ids += [int]$node.ShellId }
    return $ids
}

function Start-Services {
    param($paths, [string]$Configuration, [switch]$NoLogs)
    Restart-DockerServices -repoRoot $paths.WorkDir
    Build-SolutionIfNeeded -repoRoot $paths.WorkDir -NoBuild:$NoBuild -Configuration $Configuration

    $logs = if ($NoLogs) { @{} } else { New-LogFiles -paths $paths }

    Write-Host '启动后端 API...' -ForegroundColor Green
    $api = Start-ServiceProcess -name 'Api' -proj $paths.ApiProj -configuration $Configuration -NoLogs:$NoLogs -ShowWindows:$ShowWindows -logFiles $logs

    Start-Sleep -Seconds 1

    Write-Host '启动前端 App...' -ForegroundColor Green
    $app = Start-ServiceProcess -name 'App' -proj $paths.AppProj -configuration $Configuration -NoLogs:$NoLogs -ShowWindows:$ShowWindows -logFiles $logs

    $apiDll = Join-Path (Join-Path (Split-Path $paths.ApiProj -Parent) "bin/$Configuration/net8.0") 'BobCrm.Api.dll'
    $appDll = Join-Path (Join-Path (Split-Path $paths.AppProj -Parent) "bin/$Configuration/net8.0") 'BobCrm.App.dll'
    $pids = @{ api = $api; app = $app; startedAt = (Get-Date).ToString('s'); configuration = $Configuration; apiDll = $apiDll; appDll = $appDll }
    Save-Pids -pidFile $paths.PidFile -pids $pids

    $apiPidText = if ($api.DotnetId) { $api.DotnetId } elseif ($api.Id) { $api.Id } else { $api }
    $appPidText = if ($app.DotnetId) { $app.DotnetId } elseif ($app.Id) { $app.Id } else { $app }
    Write-Host ("已启动。API PID={0}，App PID={1}" -f $apiPidText, $appPidText) -ForegroundColor Yellow
    if (-not $NoLogs) {
        Write-Host "日志文件位于: $($paths.LogDir)" -ForegroundColor DarkGray
    }
}

function Stop-Services {
    param($paths)
    $p = Get-Pids -pidFile $paths.PidFile
    if (-not $p) {
        Write-Host '未找到 PID 记录，可能服务未运行。' -ForegroundColor DarkYellow
        return
    }
    function Get-DotnetPidsByDll([string]$dllPath) {
        if (-not $dllPath) { return @() }
        try {
            $escaped = [Regex]::Escape($dllPath)
            return @(Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe' OR Name = 'dotnet'" | Where-Object {
                $_.CommandLine -and ($_.CommandLine -match $escaped)
            } | Select-Object -ExpandProperty ProcessId)
        } catch { return @() }
    }
    # 按顺序停止：先前端，再后端（优先杀 dotnet，再杀外壳窗口）
    if ($p.app) {
        $appIds = Get-IdsFromNode -node $p.app
        if (-not $appIds -or $appIds.Count -eq 0) { $appIds = Get-DotnetPidsByDll -dllPath $p.appDll }
        if ($appIds.Count -gt 0) { Write-Host ("停止前端 App (PID={0})..." -f ($appIds -join ',')) -ForegroundColor Green }
        foreach ($id in $appIds) { Stop-ProcessSafe -processId $id }
    }
    Start-Sleep -Milliseconds 300
    if ($p.api) {
        $apiIds = Get-IdsFromNode -node $p.api
        if (-not $apiIds -or $apiIds.Count -eq 0) { $apiIds = Get-DotnetPidsByDll -dllPath $p.apiDll }
        if ($apiIds.Count -gt 0) { Write-Host ("停止后端 API (PID={0})..." -f ($apiIds -join ',')) -ForegroundColor Green }
        foreach ($id in $apiIds) { Stop-ProcessSafe -processId $id }
    }

    Remove-Item -Path $paths.PidFile -ErrorAction SilentlyContinue
    Write-Host '服务已停止。' -ForegroundColor Yellow
}

function Wait-InteractiveStop {
    param($paths)
    Write-Host '按 Ctrl+C 或任意键停止服务...' -ForegroundColor Cyan
    $cancel = $false
    $null = Register-EngineEvent -SourceIdentifier ConsoleCancel -Action { $script:cancel = $true }
    try {
        while (-not $cancel) {
            try {
                if ($Host -and $Host.UI -and $Host.UI.RawUI -and $Host.UI.RawUI.KeyAvailable) {
                    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
                    break
                }
            } catch {}
            Start-Sleep -Milliseconds 200
        }
    } finally {
        Unregister-Event -SourceIdentifier ConsoleCancel -ErrorAction SilentlyContinue
    }
    Stop-Services -paths $paths
}

$repo = Resolve-RepoRoot
$paths = Get-Paths -repoRoot $repo
Initialize-Dirs -paths $paths

switch ($Action) {
    'start' {
        Start-Services -paths $paths -Configuration $Configuration -NoLogs:$NoLogs
        if (-not $Detached) { Wait-InteractiveStop -paths $paths }
    }
    'stop' {
        Stop-Services -paths $paths
    }
    'restart' {
        Stop-Services -paths $paths
        Start-Services -paths $paths -Configuration $Configuration -NoLogs:$NoLogs
        if (-not $Detached) { Wait-InteractiveStop -paths $paths }
    }
}


