param(
    [Parameter(Mandatory = $true)][string]$Stage,
    [Parameter(Mandatory = $true)][string]$ProjectDir,
    [Parameter(Mandatory = $true)][string]$AssemblyName
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$stage = (Resolve-Path $Stage).Path
$addinDll = Join-Path $stage "$AssemblyName.dll"
$clientDll = Join-Path $stage 'AddinStore.Client.dll'
if (-not (Test-Path $addinDll)) { throw "Không thấy $addinDll" }
if (-not (Test-Path $clientDll)) { throw "Không thấy $clientDll" }

function Get-EntryTypes([string]$dir) {
    $found = New-Object System.Collections.Generic.List[string]
    Get-ChildItem $dir -Filter *.cs -Recurse | ForEach-Object {
        $text = [System.IO.File]::ReadAllText($_.FullName)
        $namespace = ''
        $namespaceMatch = [regex]::Match($text, 'namespace\s+([A-Za-z0-9_.]+)\s*;')
        if ($namespaceMatch.Success) { $namespace = $namespaceMatch.Groups[1].Value }
        foreach ($match in [regex]::Matches($text, 'class\s+([A-Za-z0-9_]+)\s*:\s*IExternal(?:Application|Command)\b')) {
            $name = $match.Groups[1].Value
            if ($namespace) { $name = "$namespace.$name" }
            if (-not $found.Contains($name)) { $found.Add($name) }
        }
    }
    if ($found.Count -eq 0) { throw "Không thấy class IExternalApplication hoặc IExternalCommand trong $dir" }
    return $found
}

$entries = @(Get-EntryTypes $ProjectDir)
$locals = & dotnet nuget locals global-packages --list
if ($LASTEXITCODE -ne 0) { throw 'Không đọc được thư mục NuGet.' }
$packagesLine = @($locals | Where-Object { $_ -match 'global-packages:' } | Select-Object -First 1)
$packages = ($packagesLine -replace '^global-packages:\s*', '').Trim()

$search = @()
foreach ($package in @('nice3point.revit.api.revitapi', 'nice3point.revit.api.revitapiui')) {
    $dllName = if ($package.EndsWith('ui')) { 'RevitAPIUI.dll' } else { 'RevitAPI.dll' }
    $dll = Get-ChildItem (Join-Path $packages $package) -Filter $dllName -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '2025\.4\.60' } |
        Select-Object -First 1
    if (-not $dll) { throw "Không thấy $dllName 2025.4.60 trong NuGet cache." }
    $search += $dll.Directory.FullName
}

$out = Join-Path $root "dist\obf\$AssemblyName"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force -Path $out | Out-Null

function ConvertTo-XmlPath([string]$path) { return ($path -replace '\\', '/') }

$skip = ($entries | ForEach-Object {
    "    <SkipType name=`"$_`" skipMethods=`"true`" skipFields=`"true`" skipProperties=`"true`" skipEvents=`"true`" skipStringHiding=`"true`" />"
}) -join "`r`n"

$config = @"
<?xml version="1.0" encoding="utf-8"?>
<Obfuscator>
  <Var name="InPath" value="$(ConvertTo-XmlPath $stage)" />
  <Var name="OutPath" value="$(ConvertTo-XmlPath $out)" />
  <Var name="KeepPublicApi" value="false" />
  <Var name="HidePrivateApi" value="true" />
  <Var name="HideStrings" value="true" />
  <Var name="RenameProperties" value="true" />
  <Var name="RenameFields" value="true" />
  <Var name="RenameEvents" value="true" />
  <Var name="ReuseNames" value="false" />
  <Var name="SkipGenerated" value="true" />
  <AssemblySearchPath path="$(ConvertTo-XmlPath $search[0])" />
  <AssemblySearchPath path="$(ConvertTo-XmlPath $search[1])" />
  <Module file="$(ConvertTo-XmlPath $addinDll)">
$skip
  </Module>
  <Module file="$(ConvertTo-XmlPath $clientDll)" />
</Obfuscator>
"@

$configPath = Join-Path $out 'obfuscar.xml'
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($configPath, $config, $utf8)

Push-Location $root
try {
    & dotnet tool restore --tool-manifest (Join-Path $root 'dotnet-tools.json')
    if ($LASTEXITCODE -ne 0) { throw 'Không cài được Obfuscar.' }
    & dotnet tool run obfuscar.console -- $configPath
    if ($LASTEXITCODE -ne 0) { throw 'Obfuscar thất bại.' }
}
finally {
    Pop-Location
}

foreach ($name in @("$AssemblyName.dll", 'AddinStore.Client.dll')) {
    $produced = Join-Path $out $name
    if (-not (Test-Path $produced)) { throw "Obfuscar không ghi $name" }
    Copy-Item $produced (Join-Path $stage $name) -Force
}

function Test-ContainsText([string]$path, [string]$value, [bool]$unicode) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $encoding = if ($unicode) { [System.Text.Encoding]::Unicode } else { [System.Text.Encoding]::UTF8 }
    return $encoding.GetString($bytes).Contains($value)
}

foreach ($entry in $entries) {
    $shortName = $entry.Split('.')[-1]
    if (-not (Test-ContainsText $addinDll $shortName $false)) {
        throw "Obfuscar đã đổi tên class Revit cần giữ: $entry"
    }
}

foreach ($dll in @($addinDll, $clientDll)) {
    if (Test-ContainsText $dll 'StoreClient' $false) {
        throw "StoreClient vẫn đọc được trong $([System.IO.Path]::GetFileName($dll))"
    }
}

if (Test-ContainsText $clientDll 'Chưa có license' $true) {
    throw 'Chuỗi license vẫn đọc được trong AddinStore.Client.dll'
}

Get-ChildItem $stage -File | Where-Object { $_.Extension -in '.pdb', '.xml' } | Remove-Item -Force
Write-Host "obfuscated=$stage"
