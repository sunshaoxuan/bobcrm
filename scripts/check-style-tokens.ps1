<#
.SYNOPSIS
Utility that inspects CSS layers for non-token color/shadow syntax.

.DESCRIPTION
`src/BobCrm.App/wwwroot/css/components` 和 `.razor.css` 文件应该优先使用 `var(--*)` Token，避免 `#xxx`/`rgba`/`hsla` 直接量。
这个脚本会列出命中的行，供 Stage 5 的样式 lint 审查和后续替换。

.PARAMETER FailOnMatch
如果指定，发现直接色值会使脚本以退出码 1 终止，方便集成到 CI。
>

param(
    [switch]$FailOnMatch,
    [string[]]$ScanPaths = @("src/BobCrm.App/wwwroot/css/components", "src/BobCrm.App/Components")
)

$patterns = @(
    '#[0-9A-Fa-f]{3}(?:[0-9A-Fa-f]{3})?\b',           # 纯 hex
    'rgba?\([^)]*\d+\s*\)',                         # rgb(a) 数值
    'hsla?\([^)]*\d+\s*\)'                           # hsl(a) 数值
)

$violations = [System.Collections.Generic.List[string]]::new()

foreach ($scanPath in $ScanPaths) {
    if (-not (Test-Path $scanPath)) {
        continue
    }

    Get-ChildItem -Path $scanPath -Recurse -Include *.css,*.razor.css -File | ForEach-Object {
        # Skip the token definition file itself
        if ($_.Name -match 'design-tokens\.css') {
            return
        }

        $lines = Get-Content -LiteralPath $_.FullName -Encoding utf8
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            foreach ($pattern in $patterns) {
                $match = [regex]::Matches($line, $pattern)
                if ($match.Count -gt 0 -and ($line -notmatch 'var\(--')) {
                    $violations.Add("$($_.FullName):$($i + 1): $line".Trim())
                }
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Found $($violations.Count) token violations. Please convert them into `var(--*)` tokens defined in `wwwroot/css/design-tokens.css`."
    $violations | Select-Object -Unique | ForEach-Object { Write-Host $_ }
    if ($FailOnMatch) {
        exit 1
    }
} else {
    Write-Host "No hard-coded colors/shadows detected in the scanned layers."
}
