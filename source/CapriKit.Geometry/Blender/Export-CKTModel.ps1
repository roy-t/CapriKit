<#
.SYNOPSIS
Exports a collection, and its "<collection>.lod.<n>" LOD collections, from a .blend file to a CKT model file.

.EXAMPLE
.\Export-CKTModel.ps1 "C:\Program Files\Blender Foundation\Blender 5.2" .\assets\Parts.blend box
Writes .\assets\box.ckt
#>
param(
    [Parameter(Mandatory)][string]$BlenderPath,
    [Parameter(Mandatory)][string]$BlendFile,
    [Parameter(Mandatory)][string]$Collection,
    # Defaults to "<collection>.ckt" next to the .blend file
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'

$blender = if (Test-Path $BlenderPath -PathType Container) { Join-Path $BlenderPath 'blender.exe' } else { $BlenderPath }
if (-not (Test-Path $blender -PathType Leaf)) { throw "Blender executable not found: $blender" }

$BlendFile = (Resolve-Path $BlendFile).Path
if (-not $OutputFile) { $OutputFile = Join-Path (Split-Path $BlendFile) "$Collection.ckt" }
$OutputFile = [System.IO.Path]::GetFullPath($OutputFile, (Get-Location).Path)

$script = Join-Path $PSScriptRoot 'export_ckt.py'

# --factory-startup ignores user add-ons and preferences so exports are reproducible
& $blender --background --factory-startup $BlendFile --python-exit-code 1 --python $script -- $Collection $OutputFile
if ($LASTEXITCODE -ne 0) { throw "Export of '$Collection' from '$BlendFile' failed with exit code $LASTEXITCODE" }
