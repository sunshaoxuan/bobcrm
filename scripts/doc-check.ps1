param(
  [string]$ContractsRoot = "src/BobCrm.Api.Contracts",
  [string]$DocsRoot = "docs/design",
  [string]$TypeFilter = ".*",
  [switch]$Quiet
)

$ErrorActionPreference = "Stop"

function Get-CodeTypes([string]$root, [string]$filter) {
  $results = @()
  $files = Get-ChildItem -Path $root -Recurse -File -Filter *.cs
  foreach ($f in $files) {
    $text = Get-Content -Raw -Path $f.FullName
    $typeMatch = [regex]::Match($text, 'public\s+(?:sealed\s+)?(?:partial\s+)?(?:class|record|struct)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    if (-not $typeMatch.Success) { continue }
    $typeName = $typeMatch.Groups["name"].Value
    if ($typeName -notmatch $filter) { continue }

    $props = @()
    foreach ($m in [regex]::Matches($text, 'public\s+[\w\<\>\[\],\.\?\s]+\s+(?<prop>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;')) {
      $props += $m.Groups["prop"].Value
    }

    if ($props.Count -eq 0) { continue }
    $results += [pscustomobject]@{
      Name = $typeName
      File = $f.FullName
      Properties = ($props | Sort-Object -Unique)
    }
  }
  return $results
}

function Parse-MarkdownTableProperties([string[]]$lines, [int]$startLine) {
  for ($i = $startLine; $i -lt $lines.Length - 1; $i++) {
    $header = $lines[$i].Trim()
    if (-not $header.StartsWith("|")) { continue }
    $sep = $lines[$i + 1].Trim()
    if (-not ($sep.StartsWith("|") -and $sep -match '\|[\s:]*-')) { continue }

    $headerCells = ($header.Trim('|') -split '\|') | ForEach-Object { $_.Trim() }
    $propertyCol = 0
    for ($c = 0; $c -lt $headerCells.Count; $c++) {
      $cell = $headerCells[$c].ToLowerInvariant()
      if ($cell -match '字段|属性|property|field|name') { $propertyCol = $c; break }
    }

    $props = @()
    for ($r = $i + 2; $r -lt $lines.Length; $r++) {
      $row = $lines[$r].Trim()
      if (-not $row.StartsWith("|")) { break }
      $cells = ($row.Trim('|') -split '\|') | ForEach-Object { $_.Trim() }
      if ($cells.Count -le $propertyCol) { continue }
      $value = $cells[$propertyCol] -replace '^`|`$', ''
      $value = $value.Trim()
      if (-not [string]::IsNullOrWhiteSpace($value)) { $props += $value }
    }

    return ($props | Sort-Object -Unique)
  }

  return @()
}

function Get-DocProperties([string]$docsRoot, [string]$typeName) {
  $docs = Get-ChildItem -Path $docsRoot -Recurse -File -Filter *.md
  foreach ($doc in $docs) {
    $text = Get-Content -Raw -Path $doc.FullName
    if ($text -notmatch [regex]::Escape($typeName)) { continue }

    $lines = $text -split "`r?`n"
    for ($i = 0; $i -lt $lines.Length; $i++) {
      if ($lines[$i] -match "^\s*#{1,6}\s+.*\b$([regex]::Escape($typeName))\b") {
        $props = Parse-MarkdownTableProperties $lines ($i + 1)
        if ($props.Count -gt 0) {
          return [pscustomobject]@{ File = $doc.FullName; Properties = $props }
        }
      }
    }
  }

  return $null
}

$types = Get-CodeTypes $ContractsRoot $TypeFilter
if ($types.Count -eq 0) {
  Write-Host "No contract types found under $ContractsRoot (filter=$TypeFilter)."
  exit 0
}

$missingDoc = @()
$diffs = @()

foreach ($t in $types) {
  $doc = Get-DocProperties $DocsRoot $t.Name
  if ($null -eq $doc) {
    $missingDoc += $t
    continue
  }

  $codeProps = $t.Properties
  $docProps = $doc.Properties
  $missing = $codeProps | Where-Object { $_ -notin $docProps }
  $extra = $docProps | Where-Object { $_ -notin $codeProps }

  if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
    $diffs += [pscustomobject]@{
      Type = $t.Name
      CodeFile = $t.File
      DocFile = $doc.File
      MissingInDoc = $missing
      ExtraInDoc = $extra
    }
  }
}

if (-not $Quiet) {
  Write-Host "Doc check result:"
  Write-Host "  Types analyzed: $($types.Count)"
  Write-Host "  Missing docs:   $($missingDoc.Count)"
  Write-Host "  Diffs:          $($diffs.Count)"
  if ($missingDoc.Count -gt 0) {
    Write-Host "`n[MissingDoc]"
    foreach ($m in $missingDoc) { Write-Host "  - $($m.Name) ($($m.File))" }
  }
  if ($diffs.Count -gt 0) {
    Write-Host "`n[Diffs]"
    foreach ($d in $diffs) {
      Write-Host "  - $($d.Type)"
      Write-Host "    Code: $($d.CodeFile)"
      Write-Host "    Doc:  $($d.DocFile)"
      if ($d.MissingInDoc.Count -gt 0) { Write-Host "    MissingInDoc: $($d.MissingInDoc -join ', ')" }
      if ($d.ExtraInDoc.Count -gt 0) { Write-Host "    ExtraInDoc:   $($d.ExtraInDoc -join ', ')" }
    }
  }
}

if ($missingDoc.Count -gt 0 -or $diffs.Count -gt 0) {
  throw "doc-check failed: missingDoc=$($missingDoc.Count), diffs=$($diffs.Count)"
}

Write-Host "doc-check passed."

