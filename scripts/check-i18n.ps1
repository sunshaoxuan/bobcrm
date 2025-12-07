# I18n 自动化检查脚本
# 用途：扫描代码中的硬编码字符串，报告违规位置并提供修复建议
# 运行：pwsh ./scripts/check-i18n.ps1
# CI集成：pwsh ./scripts/check-i18n.ps1 --ci (非零退出码表示失败)

param(
    [switch]$CI,              # CI模式：发现错误立即退出
    [switch]$Staged,          # 仅检查已暂存的文件
    [switch]$Fix,             # 自动修复模式（生成资源键建议）
    [string]$Output = "",     # 导出结果到CSV文件
    [string]$Severity = "ERROR",  # 最低严重级别：ERROR, WARNING, INFO
    [string]$LogFile = ""     # 日志文件路径（可选）
)

# 初始化日志
$script:logEnabled = $false
if ($LogFile) {
    $script:logEnabled = $true
    $logDir = [System.IO.Path]::GetDirectoryName($LogFile)
    if ($logDir -and -not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    # 开始记录
    "===========================================`n" | Out-File -FilePath $LogFile -Encoding UTF8
    "BobCRM I18n Compliance Check`n" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    "Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    "Severity: $Severity`n" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    "===========================================`n`n" | Out-File -FilePath $LogFile -Append -Encoding UTF8
}

# 配置
$Config = @{
    # 需要检查的文件扩展名
    Extensions      = @("*.cs", "*.razor")
    
    # 排除的目录
    ExcludeDirs     = @(
        "bin", "obj", "node_modules", ".git", 
        "Migrations",  # EF迁移文件
        "wwwroot"      # 静态资源
    )
    
    # 排除的文件模式
    ExcludeFiles    = @(
        "*Tests.cs",          # 单元测试
        "*Seed*.cs",          # 种子数据
        "Program.cs",         # 程序入口
        "*.Designer.cs"       # 自动生成文件
    )
    
    # 中文字符模式
    ChinesePattern  = '[\u4e00-\u9fa5]+'
    
    # 日文字符模式
    JapanesePattern = '[\u3040-\u309f\u30a0-\u30ff]+'
    
    # 允许的上下文模式（这些位置可以硬编码）
    AllowedContexts = @(
        'logger\.Log\w+\(',                    # 日志（开发者可见）
        'Console\.Write',                      # 控制台输出
        '//.*',                                # 注释
        '/\*.*\*/',                           # 块注释
        '\[Fact\]',                           # 单元测试
        '\[Theory\]',                         # 单元测试
        'const string',                        # 常量定义
        'ArgumentException\(',                 # 异常（开发者可见）
        'InvalidOperationException\(',        # 异常（开发者可见）
        'NotImplementedException\(',          # 异常（开发者可见）
        '\s*\.WithTags\(',                    # OpenAPI文档
        '\s*\.WithSummary\(',                 # OpenAPI文档
        '\s*\.WithDescription\('              # OpenAPI文档
    )
    
    # 违规级别定义
    Violations      = @{
        ERROR   = @{
            Patterns = @(
                'Results\.(Ok|BadRequest|NotFound)\(.*[\u4e00-\u9fa5]+',  # API响应
                'ErrorResponse\(.*[\u4e00-\u9fa5]+',                       # 错误响应
                'ModelState\.AddModelError\(.*[\u4e00-\u9fa5]+',         # 模型验证
                '<span>.*[\u4e00-\u9fa5]+.*</span>',                      # HTML文本
                'MessageService\.\w+\(.*[\u4e00-\u9fa5]+'                # 消息服务
            )
            Message  = "🔴 ERROR: User-facing text must use I18n resources"
        }
        WARNING = @{
            Patterns = @(
                '<Button>.*[\u4e00-\u9fa5]+.*</Button>',                  # 按钮
                '<label>.*[\u4e00-\u9fa5]+.*</label>',                    # 标签
                'Placeholder=".*[\u4e00-\u9fa5]+.*"',                     # 占位符
                'Title=".*[\u4e00-\u9fa5]+.*"'                            # 标题
            )
            Message  = "⚠️  WARNING: UI text should use I18n resources"
        }
        INFO    = @{
            Patterns = @(
                '"[\u4e00-\u9fa5]+"',                                     # 所有中文字符串
                '"[\u3040-\u309f\u30a0-\u30ff]+"'                        # 所有日文字符串
            )
            Message  = "ℹ️  INFO: Consider using I18n resources"
        }
    }
}

# 全局统计
$Global:Stats = @{
    FilesScanned    = 0
    ViolationsFound = 0
    ErrorCount      = 0
    WarningCount    = 0
    InfoCount       = 0
}

# 颜色输出（同时写入日志）
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
    
    # 如果启用日志，同时写入日志文件
    if ($script:logEnabled -and $script:LogFile) {
        $Message | Out-File -FilePath $script:LogFile -Append -Encoding UTF8
    }
}

# 检查字符串是否在允许的上下文中
function Test-AllowedContext {
    param(
        [string]$Line,
        [int]$CharIndex
    )
    
    foreach ($pattern in $Config.AllowedContexts) {
        if ($Line -match $pattern) {
            return $true
        }
    }
    
    # 检查是否在注释中
    $beforeMatch = $Line.Substring(0, $CharIndex)
    if ($beforeMatch -match '//' -or $beforeMatch -match '@\*' -or $beforeMatch -match '<!--') {
        return $true
    }
    
    return $false
}

# 生成资源键建议
function Get-ResourceKeySuggestion {
    param(
        [string]$Text,
        [string]$FilePath,
        [string]$Line
    )
    
    # 清理文本
    $cleanText = $Text -replace '[""''<>]', '' -replace '\s+', '_'
    
    # 根据上下文推断前缀
    $prefix = "TXT"
    if ($Line -match 'Button|Btn|btn') {
        $prefix = "BTN"
    }
    elseif ($Line -match 'Error|error|BadRequest') {
        $prefix = "ERR"
    }
    elseif ($Line -match 'label|Label') {
        $prefix = "LBL"
    }
    elseif ($Line -match 'Message|message|Success|Info') {
        $prefix = "MSG"
    }
    elseif ($Line -match 'Placeholder|placeholder') {
        $prefix = "PLACEHOLDER"
    }
    elseif ($Line -match 'Title|title') {
        $prefix = "TITLE"
    }
    
    # 生成键名
    $keyName = "${prefix}_" + ($cleanText -replace '[\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff]+', 'XXX').ToUpper()
    
    return @{
        Key           = $keyName
        Suggestion    = "I18n.T(`"$keyName`")"
        ResourceEntry = @"
{
  "$keyName": {
    "zh": "$Text",
    "en": "[TODO: English translation]",
    "ja": "[TODO: Japanese translation]"
  }
}
"@
    }
}

# 扫描文件
function Scan-File {
    param([string]$FilePath)
    
    $violations = @()
    $content = Get-Content $FilePath -Raw -Encoding UTF8
    $lines = $content -split "`n"
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]
        
        # 跳过注释行
        if ($line.Trim() -match '^//|^/\*|^\*|^@\*|^<!--') {
            continue
        }
        
        # 检查各个严重级别
        foreach ($level in @("ERROR", "WARNING", "INFO")) {
            # 跳过低于指定严重级别的检查
            $levels = @("ERROR", "WARNING", "INFO")
            if ($levels.IndexOf($level) -gt $levels.IndexOf($Severity)) {
                continue
            }
            
            foreach ($pattern in $Config.Violations[$level].Patterns) {
                if ($line -match $pattern) {
                    $match = $Matches[0]
                    
                    # 检查是否在允许的上下文中
                    $charIndex = $line.IndexOf($match)
                    if (Test-AllowedContext -Line $line -CharIndex $charIndex) {
                        continue
                    }
                    
                    # 提取硬编码文本
                    $hardcodedText = ""
                    if ($match -match '["'']([\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\s]+)["'']') {
                        $hardcodedText = $Matches[1]
                    }
                    
                    # 生成修复建议
                    $suggestion = Get-ResourceKeySuggestion -Text $hardcodedText -FilePath $FilePath -Line $line
                    
                    $violation = @{
                        File    = $FilePath
                        Line    = $lineNum
                        Column  = $charIndex + 1
                        Level   = $level
                        Text    = $hardcodedText
                        Context = $line.Trim()
                        Message = $Config.Violations[$level].Message
                        Fix     = $suggestion
                    }
                    
                    $violations += $violation
                    $Global:Stats.ViolationsFound++
                    
                    switch ($level) {
                        "ERROR" { $Global:Stats.ErrorCount++ }
                        "WARNING" { $Global:Stats.WarningCount++ }
                        "INFO" { $Global:Stats.InfoCount++ }
                    }
                }
            }
        }
    }
    
    return $violations
}

# 获取要扫描的文件
function Get-FilesToScan {
    $files = @()
    
    if ($Staged) {
        # 仅检查Git暂存的文件
        $gitFiles = git diff --cached --name-only --diff-filter=ACM
        foreach ($file in $gitFiles) {
            $ext = [System.IO.Path]::GetExtension($file)
            if ($Config.Extensions -contains "*$ext") {
                $files += $file
            }
        }
    }
    else {
        # 扫描所有源代码文件
        foreach ($ext in $Config.Extensions) {
            $found = Get-ChildItem -Path "src" -Filter $ext -Recurse -File | 
            Where-Object {
                $path = $_.FullName
                $shouldExclude = $false
                    
                # 排除目录
                foreach ($dir in $Config.ExcludeDirs) {
                    if ($path -like "*\$dir\*") {
                        $shouldExclude = $true
                        break
                    }
                }
                    
                # 排除文件模式
                if (-not $shouldExclude) {
                    foreach ($pattern in $Config.ExcludeFiles) {
                        if ($_.Name -like $pattern) {
                            $shouldExclude = $true
                            break
                        }
                    }
                }
                    
                -not $shouldExclude
            }
            
            $files += $found
        }
    }
    
    return $files
}

# 主函数
function Main {
    Write-ColorOutput "`n🔍 BobCRM I18n Compliance Checker" -Color Cyan
    Write-ColorOutput "================================`n" -Color Cyan
    
    # 获取文件列表
    $files = Get-FilesToScan
    
    if ($files.Count -eq 0) {
        Write-ColorOutput "No files to scan." -Color Yellow
        exit 0
    }
    
    Write-ColorOutput "Scanning $($files.Count) files...`n" -Color White
    
    # 扫描所有文件
    $allViolations = @()
    
    foreach ($file in $files) {
        $Global:Stats.FilesScanned++
        $violations = Scan-File -FilePath $file.FullName
        
        if ($violations.Count -gt 0) {
            $allViolations += $violations
            
            # 实时输出违规（非CI模式）
            if (-not $CI) {
                Write-ColorOutput "`n📄 $($file.FullName)" -Color Yellow
                
                foreach ($v in $violations) {
                    $color = switch ($v.Level) {
                        "ERROR" { "Red" }
                        "WARNING" { "Yellow" }
                        "INFO" { "Cyan" }
                    }
                    
                    Write-ColorOutput "  Line $($v.Line):$($v.Column) - $($v.Message)" -Color $color
                    Write-ColorOutput "    Text: $($v.Text)" -Color Gray
                    Write-ColorOutput "    Context: $($v.Context)" -Color DarkGray
                    
                    if ($Fix) {
                        Write-ColorOutput "    💡 Suggested fix:" -Color Green
                        Write-ColorOutput "       Replace with: $($v.Fix.Suggestion)" -Color Green
                        Write-ColorOutput "       Add to resources:`n$($v.Fix.ResourceEntry)" -Color Green
                    }
                }
            }
        }
    }
    
    # 输出统计
    Write-ColorOutput "`n" -Color White
    Write-ColorOutput "📊 Summary" -Color Cyan
    Write-ColorOutput "==========" -Color Cyan
    Write-ColorOutput "Files scanned: $($Global:Stats.FilesScanned)" -Color White
    Write-ColorOutput "Violations found: $($Global:Stats.ViolationsFound)" -Color White
    Write-ColorOutput "  🔴 Errors: $($Global:Stats.ErrorCount)" -Color Red
    Write-ColorOutput "  ⚠️  Warnings: $($Global:Stats.WarningCount)" -Color Yellow
    Write-ColorOutput "  ℹ️  Info: $($Global:Stats.InfoCount)" -Color Cyan
    
    # 导出到CSV
    if ($Output) {
        $allViolations | Select-Object File, Line, Column, Level, Text, Context, @{Name = 'Fix'; Expression = { $_.Fix.Suggestion } } |
        Export-Csv -Path $Output -NoTypeInformation -Encoding UTF8
        Write-ColorOutput "`n📋 Results exported to: $Output" -Color Green
    }
    
    # CI模式：根据错误数决定退出码
    if ($CI) {
        if ($Global:Stats.ErrorCount -gt 0) {
            Write-ColorOutput "`n❌ I18n compliance check FAILED" -Color Red
            Write-ColorOutput "   Please fix all ERROR-level violations before committing." -Color Red
            exit 1
        }
        elseif ($Global:Stats.WarningCount -gt 0) {
            Write-ColorOutput "`n⚠️  I18n compliance check passed with warnings" -Color Yellow
            Write-ColorOutput "   Consider fixing WARNING-level violations." -Color Yellow
            exit 0  # 警告不阻止构建
        }
        else {
            Write-ColorOutput "`n✅ I18n compliance check PASSED" -Color Green
            exit 0
        }
    }
    
    # 交互模式：提供后续操作建议
    if ($Global:Stats.ViolationsFound -gt 0) {
        Write-ColorOutput "`n💡 Next steps:" -Color Cyan
        Write-ColorOutput "  1. Run with --Fix to get automated suggestions" -Color White
        Write-ColorOutput "  2. Run with --Output violations.csv to export results" -Color White
        Write-ColorOutput "  3. See $([System.IO.Path]::Combine($PWD, 'docs\process\STD-05-多语言开发规范.md')) for guidelines" -Color White
    }
    
    # 显示日志文件位置
    if ($script:logEnabled -and $script:LogFile) {
        Write-ColorOutput "`n📋 Log saved to: $script:LogFile" -Color Cyan
    }
}

# 执行主函数
Main
