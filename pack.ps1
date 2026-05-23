#requires -Version 5
# Packs all shippable projects in Debug to ./artifacts with a SINGLE shared version, computed once
# here and passed as a global property. This guarantees every package -- and the inter-package
# dependency versions (e.g. Super.AppInsights.Activities -> Super.Extensions) -- get the exact same
# version, and that it auto-increments so a local feed never serves a stale cached package.
$ErrorActionPreference = 'Stop'

$revision = [int]([DateTime]::UtcNow - [DateTime]::new(2020, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)).TotalSeconds
$suffix   = "alpha.$revision"   # version becomes <VersionPrefix>-alpha.<revision>, e.g. 1.0.1-alpha.201640000

Write-Host "Packing prerelease suffix -$suffix ..."
dotnet pack -c Debug -o artifacts -p:VersionSuffix=$suffix

Write-Host ''
Write-Host 'Packed:'
Get-ChildItem (Join-Path $PSScriptRoot 'artifacts') -Filter *.nupkg |
    Sort-Object Name |
    ForEach-Object { Write-Host "  $($_.FullName)" }
