# CLAUDE.md

Guidance for working in this repo. This file documents the **versioning, CI, and release process**.

## Packages

Three NuGet packages ship from this repo. Their PackageId carries an `M.` prefix; the project
folder/assembly name does **not** (e.g. PackageId `M.Super.Activities` ↔ `src/Super.Activities/`).

| PackageId | Project | Notes |
| --- | --- | --- |
| `M.Super.Activities` | `src/Super.Activities` | Building-block activities. |
| `M.Super.Extensions` | `src/Super.Extensions` | Extensibility framework + `Super Initialize`. |
| `M.Super.AppInsights.Activities` | `src/Super.AppInsights.Activities` | App Insights telemetry. **Depends on `M.Super.Extensions`.** |

## Versioning (MinVer)

Versions come from **git tags**, computed by [MinVer]. There is no hand-edited version number.

- Each packable project sets its own `<MinVerTagPrefix>` (in its `.csproj`), so the packages version
  **independently**:
  - `M.Super.Activities-v`
  - `M.Super.Extensions-v`
  - `M.Super.AppInsights.Activities-v`
- Untagged/CI builds are floored at `1.0` (`<MinVerMinimumMajorMinor>` in `Directory.Build.props`), so a
  pre-tag build reads `1.0.0-alpha.0` rather than `0.0.0`.
- On a tagged commit the version is exactly the tag's version; commits after a tag auto-increment to the
  next patch as `-alpha.0.<height>`.
- MinVer is added once for all packable projects in `Directory.Build.targets`
  (`PackageReference Include="MinVer" PrivateAssets="all"`, gated on `IsPackable != false`).

## Tagging convention → release

Release a single package by pushing a tag `\<PackageId\>-v\<version\>`:

```
git tag M.Super.Activities-v1.2.0
git push origin M.Super.Activities-v1.2.0
```

| Push this tag | Releases |
| --- | --- |
| `M.Super.Activities-v1.2.0` | `M.Super.Activities` 1.2.0 |
| `M.Super.Extensions-v1.1.0` | `M.Super.Extensions` 1.1.0 |
| `M.Super.AppInsights.Activities-v2.0.1` | `M.Super.AppInsights.Activities` 2.0.1 |

Prerelease tags work too, e.g. `M.Super.Extensions-v1.1.0-rc.1`.

The tag prefix selects the package; the `release.yml` workflow builds the whole solution (tests must
pass), then packs and publishes **only** the tagged package.

## Workflows

- **`.github/workflows/ci.yml`** — runs on push/PR to `main`. Restores, builds (`Release`), runs all
  tests, uploads test results. Runs on `windows-latest` (these are UiPath/Studio-targeted packages).
- **`.github/workflows/release.yml`** — triggered by the package tags above. Three jobs:
  1. `build` — resolves the package from the tag, builds + tests the solution, packs only the tagged
     project, and verifies the packed version equals the tag.
  2. `publish` — pushes to NuGet.org via **trusted publishing** (`NuGet/login@v1`, OIDC; no API key
     secret). Gated on a real tag push.
  3. `github-release` — creates a GitHub release for the tag with auto-generated notes and attaches the
     `.nupkg`.

`release.yml` also supports **`workflow_dispatch`** as a *dry run*: pick a package, and it builds + packs
and uploads the `.nupkg` as a workflow artifact **without publishing** (the publish/release jobs only run
on tag pushes). Use it to exercise the pipeline safely.

## Cross-package dependency (important)

`M.Super.AppInsights.Activities` has a `ProjectReference` to `Super.Extensions`. Left to MinVer, the
emitted package dependency would float to Extensions' *in-between* prerelease version (e.g.
`1.1.1-alpha.0.3`), which is never published and would break consumers' restore.

To prevent this, releasing AppInsights **pins** the dependency to the **last published**
`M.Super.Extensions` version:

- `release.yml` finds the latest `M.Super.Extensions-v*` tag and passes
  `-p:SuperExtensionsPackageVersion=<that version>` to `dotnet pack`.
- `src/Super.Extensions/Super.Extensions.csproj` reads that property into `MinVerVersionOverride` when set.

Consequences:

- **Release `M.Super.Extensions` before `M.Super.AppInsights.Activities`** whenever Extensions changed.
  AppInsights will depend on whatever the newest `M.Super.Extensions-v*` tag is.
- If you tag AppInsights with no `M.Super.Extensions-v*` tag in existence, the release **fails fast** with
  a message telling you to release Extensions first.

## Local packing

`pack.ps1` builds all three packages to `./artifacts` with a **single shared** version
(`1.0.1-alpha.<seconds-since-2020>`) via `-p:MinVerVersionOverride`, so every package and the inter-package
dependency get the exact same version — handy for a local feed. This bypasses MinVer's tag logic on
purpose; it is for local iteration only. CI/release derive the real version from tags.

```powershell
.\pack.ps1
dotnet nuget add source <repo>\artifacts -n super-local   # one-time, to consume locally
```

## One-time setup (already required for publishing)

1. **GitHub** → repo Settings → Environments → create an environment named **`nuget-publish`**.
2. **NuGet.org** → account → Trusted Publishing → add a policy for **each** of the three package IDs:
   - Owner `marius-bughiu`, repository `super-activities`, workflow `release.yml`,
     environment `nuget-publish`, user `marius.bughiu`.

## Build / test cheatsheet

```powershell
dotnet build -c Release      # build all
dotnet test  -c Release      # run all tests
.\pack.ps1                   # local nupkgs to .\artifacts
```

[MinVer]: https://github.com/adamralph/minver
