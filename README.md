<img src="assets/super-activities.png" alt="Super Activities" width="120" align="left" />

# Super Activities for UiPath

**Production-grade monitoring and extensibility for your UiPath automations — drop in one activity and see every step.**

Stream every activity your robot runs straight to **Azure Application Insights** for real-time dashboards, alerting, and end-to-end observability. No per-activity wiring, no boilerplate.

<br clear="left" />

## Why Super Activities?

- **See everything.** Every activity emits a rich telemetry event — name, status, duration, and (optionally) argument values — so you can trace, measure, and alert on your automations like first-class software.
- **One activity, whole process.** Add **Super Initialize** at the top of `Main` and the entire run is instrumented, including the workflows it invokes. That's it.
- **Configured where it belongs.** Set your Application Insights key and toggle argument capture in **Project Settings** — no code, no hardcoded secrets in the workflow.
- **Built-in guardrails.** A Workflow Analyzer rule flags any process that forgot to add Super Initialize, so monitoring never silently goes dark.
- **Extensible by design.** A clean framework for adding cross-cutting behavior — telemetry, logging, auditing — across every activity, that other packages can build on.

## Packages

| Package | What it gives you |
| --- | --- |
| **M.Super.AppInsights.Activities** | Azure Application Insights monitoring for UiPath — automatic per-activity telemetry. |
| **M.Super.Extensions** | The extensibility framework: the **Super Initialize** activity, runtime dispatch, auto-discovery, and the analyzer rule. |
| **M.Super.Activities** | Handy building-block activities for everyday automation. |

## Quick start

1. Install **M.Super.AppInsights.Activities** (it brings **M.Super.Extensions** with it). Enable *Include Prerelease*.
2. Drop **Super Initialize** as the first activity in **Main**.
3. Open **Project Settings → App Insights** and paste your **Instrumentation Key** (a key or full connection string). Optionally enable **Capture arguments**.
4. Run. Open Application Insights and query your activity stream:

   ```kusto
   customEvents
   | where name == "Activity"
   | project timestamp,
             tostring(customDimensions.ActivityName),
             tostring(customDimensions.State)
   | order by timestamp desc
   ```

Each activity shows up with `ActivityName`, `State`, `WorkflowInstanceId`, and (when capture is on) `Arg.*` values.

## How it works

`Super Initialize` runs first and wires up the whole execution, so every activity after it — at any depth, including non-isolated invoked workflows — reports through the framework. It's a single, in-process step that works the same in attended, unattended, and cloud runs.

## Building from source

```powershell
.\pack.ps1   # builds, auto-versions (1.0.1-alpha.<n>), writes .nupkg to .\artifacts
dotnet nuget add source <repo>\artifacts -n super-local
```
