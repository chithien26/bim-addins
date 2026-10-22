param(
    [Parameter(Mandatory = $true)][string]$Slug,
    [Parameter(Mandatory = $true)][string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $root 'addins.json'
$addin = @(Get-Content $manifestPath -Raw | ConvertFrom-Json) | Where-Object { $_.slug -eq $Slug } | Select-Object -First 1
if (-not $addin) {
    throw "Không có slug '$Slug' trong addins.json."
}

$project = Join-Path $root $addin.project
$projectText = Get-Content $project -Raw
if ($projectText -notmatch '<Version>\s*([^<\s]+)\s*</Version>') {
    throw "Không thấy <Version> trong $($addin.project)."
}
$projectVersion = $Matches[1]
if ($projectVersion -ne $Version) {
    throw "Tag là $Version nhưng $($addin.project) đang là $projectVersion."
}

$stage = Join-Path $root "dist\stage\$Slug"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $root 'dist') | Out-Null

dotnet publish $project -c Release -o $stage
if ($LASTEXITCODE -ne 0) { throw "Publish add-in thất bại." }

& (Join-Path $PSScriptRoot 'obfuscate-stage.ps1') -Stage $stage -ProjectDir (Split-Path $project) -AssemblyName ([string]$addin.assemblyName)

$template = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'addin.manifest.template'))
$manifest = $template.
    Replace('__NAME__', [System.Security.SecurityElement]::Escape([string]$addin.name)).
    Replace('__ASSEMBLY__', '__ASSEMBLY__').
    Replace('__ID__', [System.Security.SecurityElement]::Escape([string]$addin.addinId)).
    Replace('__CLASS__', [System.Security.SecurityElement]::Escape([string]$addin.applicationClass))
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Join-Path $stage "$($addin.assemblyName).addin"), $manifest, $utf8)

function Escape-Iss([string]$Value) {
    return $Value.Replace('\', '\\').Replace('"', '\"')
}

$issTemplate = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'addin.iss.template'))
$iss = $issTemplate.
    Replace('__APPID__', (Escape-Iss ([string]$addin.addinId))).
    Replace('__NAME__', (Escape-Iss ([string]$addin.name))).
    Replace('__SLUG__', (Escape-Iss ([string]$addin.slug))).
    Replace('__VERSION__', (Escape-Iss $Version)).
    Replace('__ASSEMBLY__', (Escape-Iss ([string]$addin.assemblyName))).
    Replace('__YEAR__', (Escape-Iss ([string]$addin.revitYear))).
    Replace('__STAGE__', (Escape-Iss $stage)).
    Replace('__OUTPUT__', (Escape-Iss (Join-Path $root 'dist')))
$issPath = Join-Path $root "dist\$Slug.iss"
[System.IO.File]::WriteAllText($issPath, $iss, $utf8)

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Chưa có Inno Setup 6 (ISCC.exe). Cài xong chạy lại script này."
}

& $iscc $issPath
if ($LASTEXITCODE -ne 0) { throw "ISCC thất bại." }

$setup = Join-Path $root "dist\$($addin.assemblyName)-$($addin.revitYear)-Setup-$Version.exe"
$hash = (Get-FileHash $setup -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "setup=$setup"
Write-Host "sha256=$hash"
if ($env:GITHUB_REPOSITORY) {
    $tag = "$Slug-v$Version"
    $fileName = Split-Path $setup -Leaf
    Write-Host "githubReleaseUrl=https://github.com/$env:GITHUB_REPOSITORY/releases/download/$tag/$fileName"
}
