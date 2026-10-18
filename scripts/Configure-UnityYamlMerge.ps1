[CmdletBinding()]
param(
    [string]$UnityYAMLMergePath
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($UnityYAMLMergePath)) {
    $projectVersionPath = Join-Path $PSScriptRoot '..\ProjectSettings\ProjectVersion.txt'
    $versionMatch = Select-String -LiteralPath $projectVersionPath -Pattern '^m_EditorVersion:\s*(.+)$' | Select-Object -First 1
    if ($null -eq $versionMatch) {
        throw "Could not read Unity version from $projectVersionPath."
    }

    $editorVersion = $versionMatch.Matches[0].Groups[1].Value.Trim()
    $programFiles = [Environment]::GetFolderPath('ProgramFiles')
    $programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
    $candidates = @(
        (Join-Path $programFiles "Unity\Hub\Editor\$editorVersion\Editor\Data\Tools\UnityYAMLMerge.exe"),
        (Join-Path $programFilesX86 "Unity\Hub\Editor\$editorVersion\Editor\Data\Tools\UnityYAMLMerge.exe")
    )
    $UnityYAMLMergePath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($UnityYAMLMergePath) -or -not (Test-Path -LiteralPath $UnityYAMLMergePath)) {
    throw 'UnityYAMLMerge.exe was not found. Pass its path with -UnityYAMLMergePath.'
}

$mergePath = (Resolve-Path -LiteralPath $UnityYAMLMergePath).Path
$driver = '"' + $mergePath.Replace('"', '\"') + '" merge -p %O %B %A %A'

& git config --local merge.unityyamlmerge.name 'Unity Smart Merge'
if ($LASTEXITCODE -ne 0) {
    throw 'Could not write the Unity merge driver name to this repository Git config.'
}

& git config --local merge.unityyamlmerge.driver $driver
if ($LASTEXITCODE -ne 0) {
    throw 'Could not write the Unity merge driver command to this repository Git config.'
}

Write-Output "Configured UnityYAMLMerge for this repository: $mergePath"
