param(
    [Parameter(Mandatory = $true)][string]$Name,
    [Parameter(Mandatory = $true)][string]$AssemblyPath,
    [Parameter(Mandatory = $true)][string]$AddinId,
    [Parameter(Mandatory = $true)][string]$ClassName,
    [Parameter(Mandatory = $true)][string]$FileName,
    [Parameter(Mandatory = $true)][string]$RevitYear
)

$ErrorActionPreference = 'Stop'
$templatePath = Join-Path $PSScriptRoot 'addin.manifest.template'
$template = [System.IO.File]::ReadAllText($templatePath)
$text = $template.
    Replace('__NAME__', [System.Security.SecurityElement]::Escape($Name)).
    Replace('__ASSEMBLY__', [System.Security.SecurityElement]::Escape($AssemblyPath)).
    Replace('__ID__', [System.Security.SecurityElement]::Escape($AddinId)).
    Replace('__CLASS__', [System.Security.SecurityElement]::Escape($ClassName))

$dir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitYear"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Join-Path $dir $FileName), $text, $utf8)
