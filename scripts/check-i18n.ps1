# BobCRM I18n compliance check script
# Usage: pwsh ./scripts/check-i18n.ps1 [-CI] [-Staged] [-Fix] [-Output violations.csv] [-Severity ERROR|WARNING|INFO] [-LogFile path]

[CmdletBinding()]
param(
    [switch]$CI,
    [switch]$Staged,
    [switch]$Fix,
    [string]$Output = "",
    [ValidateSet("ERROR", "WARNING", "INFO")]
    [string]$Severity = "ERROR",
    [string]$LogFile = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Severity ordering helper
$SeverityOrder = @("ERROR", "WARNING", "INFO")
function Should-CheckLevel {
    param([string]$Level)
    return $SeverityOrder.IndexOf($Level) -le $SeverityOrder.IndexOf($Severity)
}

# Logging
$script:LogEnabled = $false
$script:ResolvedLogFile = $null
if ($LogFile) {
    $script:LogEnabled = $true
    $logDir = [System.IO.Path]::GetDirectoryName($LogFile)
    if ($logDir -and -not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    $script:ResolvedLogFile = $LogFile
    "===========================================" | Out-File -FilePath $LogFile -Encoding UTF8
    "BobCRM I18n Compliance Check" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    "Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    "Severity: $Severity" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    "===========================================`n" | Out-File -FilePath $LogFile -Append -Encoding UTF8
}

# Configuration
$Config = @{
    Extensions   = @("*.cs", "*.razor")
    ExcludeDirs  = @("bin", "obj", "node_modules", ".git", "Migrations", "wwwroot")
    ExcludeFiles = @("*.Designer.cs", "*Tests.cs", "*Seed*.cs", "Program.cs")
    AllowedContexts = @(
        'logger\.Log\w+\s*\(',
        '_logger\.Log\w+\s*\(',
        'Console\.Write',
        '\[Fact\]',
        '\[Theory\]',
        'const\s+string',
        'Argument(Exception|NullException)?\s*\(',
        'InvalidOperationException\s*\(',
        'NotImplementedException\s*\(',
        'NotSupportedException\s*\(',
        'JsonException\s*\(',
        '\.WithTags\s*\(',
        '\.WithSummary\s*\(',
        '\.WithDescription\s*\(',
        '^\s*///\s*',
        'nameof\s*\(',
        '\[Description\s*\(',
        '\[DisplayName\s*\('
    )
    Violations = @{
        ERROR = @{
            Patterns = @(
                'Results\.(Ok|BadRequest|NotFound|Problem|Conflict|UnprocessableEntity)\s*\([^)]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+',
                'new\s+ErrorResponse\s*\([^)]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+',
                'ModelState\.AddModelError\s*\([^)]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+',
                '<span[^>]*>[^<]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+[^<]*</span>',
                'MessageService\.\w+\s*\([^)]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+',
                'ToastService\.\w+\s*\([^)]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+'
            )
            Message = "ERROR: user-facing text must use I18n resources."
        }
        WARNING = @{
            Patterns = @(
                '<Button[^>]*>[^<]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+[^<]*</Button>',
                '<label[^>]*>[^<]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+[^<]*</label>',
                'Placeholder\s*=\s*"[^"]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+[^"]*"',
                'Title\s*=\s*"[^"]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+[^"]*"',
                '<Divider[^>]*>[^<]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+[^<]*</Divider>',
                '<th[^>]*>[^<]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+[^<]*</th>'
            )
            Message = "WARNING: UI text should use I18n resources."
        }
        INFO = @{
            Patterns = @(
                '"[^"]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]+[^"]*"'
            )
            Message = "INFO: consider moving string literals to I18n resources."
        }
    }
}

# Global stats
$Global:Stats = @{
    FilesScanned    = 0
    ViolationsFound = 0
    ErrorCount      = 0
    WarningCount    = 0
    InfoCount       = 0
}

# Color output with optional logging
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )

    Write-Host $Message -ForegroundColor $Color

    if ($script:LogEnabled -and $script:ResolvedLogFile) {
        $Message | Out-File -FilePath $script:ResolvedLogFile -Append -Encoding UTF8
    }
}

# Check whether the match is inside an allowed context
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

    $beforeMatch = if ($CharIndex -gt 0 -and $CharIndex -le $Line.Length) {
        $Line.Substring(0, $CharIndex)
    } else {
        ""
    }

    if ($beforeMatch -match '(?<![:/])//') {
        return $true
    }

    if ($beforeMatch -match '@\*' -and $Line -notmatch '\*@.*$') {
        return $true
    }
    if ($Line -match '@\*.*\*@') {
        return $true
    }

    if ($beforeMatch -match '<!--' -and $Line -notmatch '-->') {
        return $true
    }

    return $false
}

# Detect multiline block comments (/* ... */ or @* ... *@)
function Test-InBlockComment {
    param(
        [string[]]$Lines,
        [int]$LineIndex
    )

    $depth = 0
    for ($i = $LineIndex; $i -ge 0; $i--) {
        $line = $Lines[$i]
        $opens = ([regex]::Matches($line, '/\*|@\*')).Count
        $closes = ([regex]::Matches($line, '\*/|\*@')).Count
        if ($i -eq $LineIndex) {
            $depth = $opens - $closes
        } else {
            $depth += $opens - $closes
        }

        if ($depth -gt 0) {
            return $true
        }
    }

    return $false
}

# Build a resource key suggestion
function Get-ResourceKeySuggestion {
    param(
        [string]$Text,
        [string]$Line
    )

    $sourceText = if ($Text) { $Text } else { "Text" }
    $cleanText = $sourceText -replace '["''<>]', '' -replace '\s+', '_' -replace '[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}]', 'X'
    if (-not $cleanText) { $cleanText = "TEXT" }

    $prefix = "TXT"
    if ($Line -match '(Button|Btn)\b') {
        $prefix = "BTN"
    }
    elseif ($Line -match 'Error|BadRequest|Exception') {
        $prefix = "ERR"
    }
    elseif ($Line -match 'label|Label') {
        $prefix = "LBL"
    }
    elseif ($Line -match 'Message|Toast|Success|Info') {
        $prefix = "MSG"
    }
    elseif ($Line -match 'Placeholder') {
        $prefix = "PLACEHOLDER"
    }
    elseif ($Line -match 'Title') {
        $prefix = "TITLE"
    }

    $keyName = ("{0}_{1}" -f $prefix, $cleanText.ToUpper())
    $safeText = ($sourceText -replace '"', '\"')

    return @{
        Key           = $keyName
        Suggestion    = "I18n.T(`"$keyName`")"
        ResourceEntry = @"
{
  "$keyName": {
    "zh": "$safeText",
    "en": "[TODO: English translation]",
    "ja": "[TODO: Japanese translation]"
  }
}
"@
    }
}

# Scan a single file for violations
function Scan-File {
    param([string]$FilePath)

    $violations = @()
    $content = Get-Content -Path $FilePath -Raw -Encoding UTF8
    $lines = $content -split "`n"

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]
        $trimmedLine = $line.Trim()

        if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
            continue
        }

        if ($trimmedLine -match '^//(?!/)|^/\*|^\*\s|^\*$|^@\*|^<!--|^\*\s*@') {
            continue
        }

        if ($trimmedLine -match '^///') {
            continue
        }

        if (Test-InBlockComment -Lines $lines -LineIndex $i) {
            continue
        }

        foreach ($level in $SeverityOrder) {
            if (-not (Should-CheckLevel $level)) {
                continue
            }

            foreach ($pattern in $Config.Violations[$level].Patterns) {
                if ($line -match $pattern) {
                    $matchText = $Matches[0]
                    $charIndex = $line.IndexOf($matchText)
                    if ($charIndex -lt 0) { $charIndex = 0 }

                    if (Test-AllowedContext -Line $line -CharIndex $charIndex) {
                        continue
                    }

                    $hardcodedText = ""
                    if ($matchText -match '"([^"]*[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}][^"]*)"') {
                        $hardcodedText = $Matches[1]
                    }

                    $suggestion = Get-ResourceKeySuggestion -Text $hardcodedText -Line $line

                    $violations += @{
                        File    = $FilePath
                        Line    = $lineNum
                        Column  = $charIndex + 1
                        Level   = $level
                        Text    = $hardcodedText
                        Context = $trimmedLine
                        Message = $Config.Violations[$level].Message
                        Fix     = $suggestion
                    }

                    $Global:Stats.ViolationsFound++
                    switch ($level) {
                        "ERROR"   { $Global:Stats.ErrorCount++ }
                        "WARNING" { $Global:Stats.WarningCount++ }
                        "INFO"    { $Global:Stats.InfoCount++ }
                    }
                }
            }
        }
    }

    return $violations
}

# Collect files to scan
function Get-FilesToScan {
    $files = @()

    if ($Staged) {
        $gitFiles = git diff --cached --name-only --diff-filter=ACM
        foreach ($file in $gitFiles) {
            $ext = [System.IO.Path]::GetExtension($file)
            if ($Config.Extensions -contains "*$ext") {
                if (Test-Path $file) {
                    $files += (Get-Item $file).FullName
                }
            }
        }
    }
    else {
        foreach ($ext in $Config.Extensions) {
            $found = Get-ChildItem -Path "src" -Filter $ext -Recurse -File |
                Where-Object {
                    $path = $_.FullName
                    $shouldExclude = $false

                    foreach ($dir in $Config.ExcludeDirs) {
                        if ($path -like "*\$dir\*") {
                            $shouldExclude = $true
                            break
                        }
                    }

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

            $files += $found.FullName
        }
    }

    return $files
}

# Main entrypoint
function Main {
    Write-ColorOutput "`n== BobCRM I18n Compliance Checker ==" -Color Cyan

    $files = Get-FilesToScan

    if ($files.Count -eq 0) {
        Write-ColorOutput "No files to scan." -Color Yellow
        exit 0
    }

    Write-ColorOutput "Scanning $($files.Count) files...`n" -Color White

    $allViolations = @()

    foreach ($file in $files) {
        $Global:Stats.FilesScanned++
        $violations = Scan-File -FilePath $file

        if ($null -eq $violations) {
            $violations = @()
        }

        if ($violations.Count -gt 0) {
            $allViolations += $violations

            if (-not $CI) {
                Write-ColorOutput "`n>> $file" -Color Yellow

                foreach ($v in $violations) {
                    $color = switch ($v.Level) {
                        "ERROR"   { "Red" }
                        "WARNING" { "Yellow" }
                        "INFO"    { "Cyan" }
                    }

                    Write-ColorOutput ("  Line {0}:{1} - {2}" -f $v.Line, $v.Column, $v.Message) -Color $color
                    Write-ColorOutput ("    Text: {0}" -f $v.Text) -Color Gray
                    Write-ColorOutput ("    Context: {0}" -f $v.Context) -Color DarkGray

                    if ($Fix) {
                        Write-ColorOutput "    Suggested fix:" -Color Green
                        Write-ColorOutput ("       Replace with: {0}" -f $v.Fix.Suggestion) -Color Green
                        Write-ColorOutput ("       Add to resources:`n{0}" -f $v.Fix.ResourceEntry) -Color Green
                    }
                }
            }
        }
    }

    Write-ColorOutput "`nSummary" -Color Cyan
    Write-ColorOutput "-------" -Color Cyan
    Write-ColorOutput "Files scanned: $($Global:Stats.FilesScanned)" -Color White
    Write-ColorOutput "Violations found: $($Global:Stats.ViolationsFound)" -Color White
    Write-ColorOutput "  Errors:   $($Global:Stats.ErrorCount)" -Color Red
    Write-ColorOutput "  Warnings: $($Global:Stats.WarningCount)" -Color Yellow
    Write-ColorOutput "  Info:     $($Global:Stats.InfoCount)" -Color Cyan

    if ($Output) {
        $allViolations |
            Select-Object File, Line, Column, Level, Text, Context, @{ Name = 'Fix'; Expression = { $_.Fix.Suggestion } } |
            Export-Csv -Path $Output -NoTypeInformation -Encoding UTF8
        Write-ColorOutput "`nResults exported to: $Output" -Color Green
    }

    if ($CI) {
        if ($Global:Stats.ErrorCount -gt 0) {
            Write-ColorOutput "`nI18n compliance check FAILED" -Color Red
            exit 1
        }
        elseif ($Global:Stats.WarningCount -gt 0) {
            Write-ColorOutput "`nI18n compliance check passed with warnings" -Color Yellow
            exit 0
        }
        else {
            Write-ColorOutput "`nI18n compliance check PASSED" -Color Green
            exit 0
        }
    }

    if ($Global:Stats.ViolationsFound -gt 0) {
        Write-ColorOutput "`nNext steps:" -Color Cyan
        Write-ColorOutput "  1. Run with --Fix to get automated suggestions" -Color White
        Write-ColorOutput "  2. Run with --Output violations.csv to export results" -Color White
        Write-ColorOutput "  3. See docs/process/STD-05-多语言合规规范.md for guidance" -Color White
    }

    if ($script:LogEnabled -and $script:ResolvedLogFile) {
        Write-ColorOutput "`nLog saved to: $script:ResolvedLogFile" -Color Cyan
    }
}

Main
