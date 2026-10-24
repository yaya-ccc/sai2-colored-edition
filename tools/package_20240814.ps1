param(
    [Parameter(Mandatory = $true)][string]$SaiExecutable,
    [string]$OutputDirectory,
    [switch]$SelfContained
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$source = (Resolve-Path -LiteralPath $SaiExecutable).Path
$sourceIni = Join-Path (Split-Path -Parent $source) 'sai2.ini'
if (-not (Test-Path -LiteralPath $sourceIni -PathType Leaf)) {
    throw 'The matching sai2.ini must be present beside the input executable.'
}
$expectedHash = 'C1F2B05CF5FB875B7CEB60D3B3809363803BAE0A089E567FEA8704F892CFDEFA'
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'This executable is not the verified 2024.08.14 x64 localized build.'
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repository ('dist\S2CE-20240814-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$destination = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $destination) {
    throw 'Choose a new output directory so an existing theme or backup cannot be overwritten.'
}
$publishArguments = @('publish', (Join-Path $repository 'S2CE.csproj'), '-c', 'Release', '--output', $destination, '--nologo')
if ($SelfContained) { $publishArguments += @('-r', 'win-x64', '--self-contained', 'true') }
else { $publishArguments += @('--self-contained', 'false') }
& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$target = Join-Path $destination 'ref\sai2.exe'
Copy-Item -LiteralPath $source -Destination $target -Force
Copy-Item -LiteralPath $sourceIni -Destination (Join-Path $destination 'ref\sai2.ini') -Force
if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'Packaged SAI2 hash verification failed.'
}
Copy-Item -LiteralPath (Join-Path $repository 'docs\20240814-support.md') -Destination (Join-Path $destination '20240814-support.md')
Copy-Item -LiteralPath (Join-Path $repository 'docs\20240814-ui-validation.md') -Destination (Join-Path $destination '20240814-ui-validation.md')
Copy-Item -LiteralPath (Join-Path $repository 'docs\multiversion-copy.md') -Destination (Join-Path $destination 'multiversion-copy.md')
Copy-Item -LiteralPath (Join-Path $repository 'docs\evidence') -Destination (Join-Path $destination 'evidence') -Recurse
Write-Output "Package ready: $destination"
