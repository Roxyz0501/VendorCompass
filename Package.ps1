$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build VendorCompass/VendorCompass.csproj -c Release -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $out = Join-Path $PSScriptRoot 'VendorCompass/bin/Release'
    $version = (Get-Content (Join-Path $out 'VendorCompass.json') -Raw -Encoding UTF8 | ConvertFrom-Json).AssemblyVersion
    $dir = New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot 'artifacts')
    $zip = Join-Path $dir.FullName "VendorCompass-$version.zip"
    $stream = [IO.File]::Open($zip, [IO.FileMode]::Create)
    try {
        $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($name in 'VendorCompass.dll','VendorCompass.json','images/icon.png','LICENSE','NOTICE') {
                $base = if ($name -in 'LICENSE','NOTICE') { $PSScriptRoot } else { $out }
                [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $base $name), $name)
            }
        } finally { $archive.Dispose() }
    } finally { $stream.Dispose() }
    Get-FileHash $zip
} finally { Pop-Location }
