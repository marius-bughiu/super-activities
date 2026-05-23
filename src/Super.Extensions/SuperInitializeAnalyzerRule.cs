using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Super.Extensions;

/// <summary>
/// Workflow Analyzer registration discovered by UiPath Studio. Because this rule ships in the core
/// extensions package, it only exists when that package is installed. It is a WORKFLOW-level rule so the
/// result is associated with the offending file: it inspects the project's entry point(s) and errors if
/// the <see cref="SuperInitialize"/> activity is missing (otherwise the extensions never run).
/// </summary>
public sealed class SuperInitializeAnalyzerRule : IRegisterAnalyzerConfiguration
{
    private const string RuleId = "SUPER-001";
    private const string RuleName = "Missing Super Initialize";

    public void Initialize(IAnalyzerConfigurationService service)
    {
        var rule = new Rule<IWorkflowModel>(RuleName, RuleId, Inspect)
        {
            DefaultErrorLevel = TraceLevel.Error,
            RecommendationMessage = $"Add the '{SuperInitialize.DefaultDisplayName}' activity at the start of the project's main entry point.",
        };
        service.AddRule(rule);
    }

    internal static InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
    {
        // Only the entry-point workflow(s) must contain the activity.
        if (!IsEntryPoint(workflow))
        {
            return new InspectionResult { HasErrors = false };
        }

        var root = workflow.Root;
        if (root is not null && ContainsSuperInitialize(root))
        {
            return new InspectionResult { HasErrors = false };
        }

        // The result is scoped to this workflow, so Studio shows the file in the File column — keep the
        // message itself file-agnostic.
        var result = new InspectionResult
        {
            HasErrors = true,
            ErrorLevel = rule.ErrorLevel,
            RecommendationMessage =
                $"Add the '{SuperInitialize.DefaultDisplayName}' activity at the start of the main entry point so the Super extensions run. " +
                "Without it, the extensions (e.g. App Insights telemetry) are never wired up.",
        };
        result.Messages?.Add($"The '{SuperInitialize.DefaultDisplayName}' activity is missing.");
        return result;
    }

    internal static bool IsEntryPoint(IWorkflowModel workflow)
    {
        var project = workflow.Project;
        if (project is null)
        {
            return false;
        }

        bool Matches(string? candidate)
            => !string.IsNullOrEmpty(candidate)
               && (string.Equals(candidate, workflow.RelativePath, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(FileNameOf(candidate), FileNameOf(workflow.RelativePath), StringComparison.OrdinalIgnoreCase));

        if (Matches(project.EntryPointName))
        {
            return true;
        }

        if (project.EntryPoints is not null)
        {
            foreach (var entry in project.EntryPoints)
            {
                if (Matches(entry))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool ContainsSuperInitialize(IActivityModel activity)
    {
        if (IsSuperInitialize(activity.Type))
        {
            return true;
        }

        foreach (var child in activity.Children)
        {
            if (ContainsSuperInitialize(child))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool IsSuperInitialize(string? activityType)
        => activityType is not null && activityType.Contains(nameof(SuperInitialize), StringComparison.Ordinal);

    private static string? FileNameOf(string? path)
        => string.IsNullOrEmpty(path) ? null : Path.GetFileName(path);
}
