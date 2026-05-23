#requires -Version 5
# Local convenience pack: builds all shippable projects in Debug to ./artifacts with a SINGLE shared
# version, computed once here and passed as a global property. This guarantees every package -- and the
# inter-package dependency versions (e.g. M.Super.AppInsights.Activities -> M.Super.Extensions) -- get
# the exact same version, and that it auto-increments so a local feed never serves a stale cached package.
#
# Versioning is otherwise owned by MinVer (git tags) -- see Directory.Build.props/.targets. The override
# below is for fast local-feed iteration only; CI/release derive the real version from per-package tags.
$ErrorActionPreference = 'Stop'

$revision = [int]([DateTime]::UtcNow - [DateTime]::new(2020, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)).TotalSeconds
$version  = "1.0.1-alpha.$revision"   # e.g. 1.0.1-alpha.201640000

Write-Host "Packing local version $version (MinVerVersionOverride) ..."
dotnet pack -c Debug -o artifacts -p:MinVerVersionOverride=$version

Write-Host ''
Write-Host 'Packed:'
Get-ChildItem (Join-Path $PSScriptRoot 'artifacts') -Filter *.nupkg |
    Sort-Object Name |
    ForEach-Object { Write-Host "  $($_.FullName)" }
