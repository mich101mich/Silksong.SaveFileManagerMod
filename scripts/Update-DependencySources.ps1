[CmdletBinding()]
param(
    [string]$PropsPath = (Join-Path $PSScriptRoot "..\SilksongPath.props"),
    [switch]$Full,
    [switch]$CloneRepos,
    [switch]$Decompile,
    [switch]$ForceReclone,
    [switch]$Pull
)

$ErrorActionPreference = "Stop"

function Expand-MsBuildProperties {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][hashtable]$PropertyMap
    )

    $expanded = $Value

    for ($i = 0; $i -lt 10; $i++) {
        $next = [regex]::Replace($expanded, "\$\(([^\)]+)\)", {
                param($match)
                $propertyName = $match.Groups[1].Value

                if ($PropertyMap.ContainsKey($propertyName)) {
                    return $PropertyMap[$propertyName]
                }

                return $match.Value
            })

        if ($next -eq $expanded) {
            break
        }

        $expanded = $next
    }

    return $expanded
}

function Get-PropertyValue {
    param(
        [Parameter(Mandatory = $true)][hashtable]$PropertyMap,
        [Parameter(Mandatory = $true)][string]$Name,
        [string]$Fallback = ""
    )

    if (-not $PropertyMap.ContainsKey($Name)) {
        return $Fallback
    }

    $value = $PropertyMap[$Name]
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $Fallback
    }

    return (Expand-MsBuildProperties -Value $value.Trim() -PropertyMap $PropertyMap)
}

function Ensure-GitRepository {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$RepositoryUrl,
        [Parameter(Mandatory = $true)][string]$Destination,
        [switch]$ForceReclone,
        [switch]$Pull
    )

    $gitFolder = Join-Path $Destination ".git"

    if (Test-Path $gitFolder) {
        if ($Pull) {
            Write-Host "Updating $Name in $Destination ..."
            git -C $Destination pull --ff-only
        }
        else {
            Write-Host "$Name already exists at $Destination"
        }

        return
    }

    if ((Test-Path $Destination) -and $ForceReclone) {
        Write-Host "Removing existing non-git folder at $Destination ..."
        Remove-Item -Path $Destination -Recurse -Force
    }

    if (Test-Path $Destination) {
        Write-Warning "$Destination exists but is not a git repository. Use -ForceReclone to replace it."
        return
    }

    New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null

    Write-Host "Cloning $Name into $Destination ..."
    git clone --depth 1 $RepositoryUrl $Destination
}

function Decompile-DllToProject {
    param(
        [Parameter(Mandatory = $true)][string]$AssemblyName,
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$OutputRoot
    )

    if (-not (Test-Path $DllPath)) {
        Write-Warning "Skipping $AssemblyName because DLL is missing: $DllPath"
        return
    }

    $outputDir = Join-Path $OutputRoot $AssemblyName
    if (Test-Path $outputDir) {
        Remove-Item -Path $outputDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

    Write-Host "Decompiling $AssemblyName ..."
    & ilspycmd -p -o $outputDir --languageversion CSharp10_0 $DllPath

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to decompile $AssemblyName from $DllPath"
    }
}

$resolvedPropsPath = Resolve-Path -LiteralPath $PropsPath -ErrorAction Stop
[xml]$props = Get-Content -LiteralPath $resolvedPropsPath
$propertyGroup = $props.Project.PropertyGroup | Select-Object -First 1

if ($null -eq $propertyGroup) {
    throw "Could not find a <PropertyGroup> in $resolvedPropsPath"
}

$propertyMap = @{}
foreach ($node in $propertyGroup.ChildNodes) {
    if ($node.NodeType -eq [System.Xml.XmlNodeType]::Element) {
        $propertyMap[$node.Name] = $node.InnerText
    }
}

$silksongDllFolder = Get-PropertyValue -PropertyMap $propertyMap -Name "SilksongDllFolder"
$bepInExDllFolder = Get-PropertyValue -PropertyMap $propertyMap -Name "BepInExDllFolder"
$decompiledRoot = Get-PropertyValue -PropertyMap $propertyMap -Name "SilksongDecompiledFolder"
$silksongSourceFolder = Get-PropertyValue -PropertyMap $propertyMap -Name "SilksongSourceFolder" -Fallback (Join-Path $decompiledRoot "Silksong")
$bepInExSourceFolder = Get-PropertyValue -PropertyMap $propertyMap -Name "BepInExSourceFolder" -Fallback (Join-Path $decompiledRoot "BepInEx")
$harmonyXSourceFolder = Get-PropertyValue -PropertyMap $propertyMap -Name "HarmonyXSourceFolder" -Fallback (Join-Path $decompiledRoot "HarmonyX")

$doClone = $Full -or $CloneRepos
$doDecompile = $Full -or $Decompile

if (-not $doClone -and -not $doDecompile) {
    throw "No action selected. Use -Full, -CloneRepos, -Decompile, or a combination of them."
}

if ($doClone) {
    Ensure-GitRepository -Name "BepInEx" -RepositoryUrl "https://github.com/BepInEx/BepInEx.git" -Destination $bepInExSourceFolder -ForceReclone:$ForceReclone -Pull:$Pull
    Ensure-GitRepository -Name "HarmonyX" -RepositoryUrl "https://github.com/BepInEx/HarmonyX.git" -Destination $harmonyXSourceFolder -ForceReclone:$ForceReclone -Pull:$Pull
}

if ($doDecompile) {
    dotnet tool install --global ilspycmd

    $assemblies = @(
        @{ Name = "0Harmony"; DllPath = (Join-Path $bepInExDllFolder "0Harmony.dll") }
        @{ Name = "BepInEx"; DllPath = (Join-Path $bepInExDllFolder "BepInEx.dll") }
        @{ Name = "Assembly-CSharp"; DllPath = (Join-Path $silksongDllFolder "Assembly-CSharp.dll") }
        @{ Name = "Assembly-CSharp-firstpass"; DllPath = (Join-Path $silksongDllFolder "Assembly-CSharp-firstpass.dll") }
        @{ Name = "TeamCherry.BuildBot"; DllPath = (Join-Path $silksongDllFolder "TeamCherry.BuildBot.dll") }
        @{ Name = "TeamCherry.Cinematics"; DllPath = (Join-Path $silksongDllFolder "TeamCherry.Cinematics.dll") }
        @{ Name = "TeamCherry.Localization"; DllPath = (Join-Path $silksongDllFolder "TeamCherry.Localization.dll") }
        @{ Name = "TeamCherry.NestedFadeGroup"; DllPath = (Join-Path $silksongDllFolder "TeamCherry.NestedFadeGroup.dll") }
        @{ Name = "TeamCherry.SharedUtils"; DllPath = (Join-Path $silksongDllFolder "TeamCherry.SharedUtils.dll") }
        @{ Name = "TeamCherry.Splines"; DllPath = (Join-Path $silksongDllFolder "TeamCherry.Splines.dll") }
        @{ Name = "TeamCherry.TK2D"; DllPath = (Join-Path $silksongDllFolder "TeamCherry.TK2D.dll") }
        @{ Name = "UnityEngine"; DllPath = (Join-Path $silksongDllFolder "UnityEngine.dll") }
        @{ Name = "UnityEngine.CoreModule"; DllPath = (Join-Path $silksongDllFolder "UnityEngine.CoreModule.dll") }
        @{ Name = "UnityEngine.InputLegacyModule"; DllPath = (Join-Path $silksongDllFolder "UnityEngine.InputLegacyModule.dll") }
        @{ Name = "UnityEngine.UI"; DllPath = (Join-Path $silksongDllFolder "UnityEngine.UI.dll") }
    )

    New-Item -ItemType Directory -Path $silksongSourceFolder -Force | Out-Null

    foreach ($assembly in $assemblies) {
        Decompile-DllToProject -AssemblyName $assembly.Name -DllPath $assembly.DllPath -OutputRoot $silksongSourceFolder
    }
}

Write-Host "Done."
Write-Host "Silksong decompiled project output: $silksongSourceFolder"
Write-Host "BepInEx source repo path: $bepInExSourceFolder"
Write-Host "HarmonyX source repo path: $harmonyXSourceFolder"
