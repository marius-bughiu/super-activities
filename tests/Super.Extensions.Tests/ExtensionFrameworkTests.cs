using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using Super.Activities;
using Xunit;

namespace Super.Extensions.Tests;

public sealed class ExtensionFrameworkTests : IDisposable
{
    private readonly RecordingExtension _ext = new();

    public ExtensionFrameworkTests()
    {
        SuperExtensions.Clear();
        SuperExtensions.Register(_ext);
    }

    public void Dispose() => SuperExtensions.Clear();

    private static (Activity root, WriteMessage target) BuildWorkflow()
    {
        var target = new WriteMessage { DisplayName = "Greet", Message = new InArgument<string>("hi") };
        var root = new Sequence { Activities = { target } };
        return (root, target);
    }

    [Fact]
    public void SuperInitialize_InjectsDispatcher_WithoutHostWireUp()
    {
        // Simulates the UiPath Robot: the host runs the workflow but never calls UseSuperExtensions.
        // The Super Initialize activity reflects into the live executor and attaches the dispatcher.
        var greet = new WriteMessage { DisplayName = "Greet", Message = new InArgument<string>("hi") };
        var root = new Sequence { Activities = { new SuperInitialize(), greet } };

        new WorkflowInvoker(root).Invoke(); // NOTE: no UseSuperExtensions(root)

        Assert.Contains(_ext.Events, e => e.Name == "Greet" && e.State == "Executing");
        Assert.Contains(_ext.Events, e => e.Name == "Greet" && e.State == "Closed");
    }

    [Fact]
    public void SuperInitialize_AutoDiscovers_Extensions()
    {
        // No explicit registration: discovery must find RecordingExtension (parameterless ctor) and the
        // discovered instance must receive events.
        SuperExtensions.Clear();

        var greet = new WriteMessage { DisplayName = "Greet", Message = new InArgument<string>("hi") };
        var root = new Sequence { Activities = { new SuperInitialize(), greet } };

        new WorkflowInvoker(root).Invoke();

        var discovered = SuperExtensions.All.OfType<RecordingExtension>().FirstOrDefault();
        Assert.NotNull(discovered);
        Assert.Contains(discovered!.Events, e => e.Name == "Greet" && e.State == "Closed");
    }

    [Fact]
    public void Dispatcher_Captures_DeeplyNestedActivities_FromSingleRootAttach()
    {
        // One attach at the root sees activities at any depth (same executor) -> mirrors how a single
        // Super Initialize in Main covers non-isolated Invoke Workflow File children.
        var deep = new WriteMessage { DisplayName = "Deep", Message = new InArgument<string>("x") };
        var root = new Sequence
        {
            Activities =
            {
                new SuperInitialize(),
                new Sequence { Activities = { new Sequence { Activities = { deep } } } },
            }
        };

        new WorkflowInvoker(root).Invoke(); // no UseSuperExtensions

        Assert.Contains(_ext.Events, e => e.Name == "Deep" && e.State == "Closed");
    }

    [Fact]
    public void AnalyzerRule_IsSuperInitialize_MatchesActivityType()
    {
        Assert.True(SuperInitializeAnalyzerRule.IsSuperInitialize("Super.Extensions.SuperInitialize"));
        Assert.False(SuperInitializeAnalyzerRule.IsSuperInitialize("System.Activities.Statements.Sequence"));
        Assert.False(SuperInitializeAnalyzerRule.IsSuperInitialize(null));
    }

    [Fact]
    public void RuntimeHooks_FireBeforeAndAfter_WithConfiguredValue()
    {
        var (root, target) = BuildWorkflow();
        ActivityExtensionData.Set(target, _ext, "MaxRetries", 2);

        var invoker = new WorkflowInvoker(root);
        invoker.UseSuperExtensions(root);
        invoker.Invoke();

        Assert.Contains(_ext.Events, e => e is { Name: "Greet", State: "Executing", MaxRetries: 2 });
        Assert.Contains(_ext.Events, e => e is { Name: "Greet", State: "Closed", MaxRetries: 2 });
    }

    [Fact]
    public void Storage_SetGetList_Works()
    {
        var (_, target) = BuildWorkflow();
        ActivityExtensionData.Set(target, _ext, "MaxRetries", 7);

        Assert.True(ActivityExtensionData.TryGet(target, _ext, "MaxRetries", out var value));
        Assert.Equal(7, value);

        var listed = ActivityExtensionData.List(target);
        Assert.Contains(listed, p => p.MemberName == "MaxRetries" && Equals(p.Value, 7));
    }

    [Fact]
    public void DesignTime_SurfacesProperty_ViaTypeDescriptor()
    {
        SuperExtensions.EnableDesignTime();
        var (_, target) = BuildWorkflow();

        var pd = TypeDescriptor.GetProperties(target)["MaxRetries"];
        Assert.NotNull(pd);
        Assert.Equal(typeof(int), pd!.PropertyType);
        Assert.Equal("Reliability", pd.Category);
        Assert.Equal("Retry count.", pd.Description);

        pd.SetValue(target, 9);
        Assert.Equal(9, pd.GetValue(target));
    }

    [Fact]
    public void Xaml_RoundTrips_ExtensionProperty()
    {
        var (root, target) = BuildWorkflow();
        ActivityExtensionData.Set(target, _ext, "MaxRetries", 5);

        var xaml = SuperXaml.Save(root);
        Assert.Contains("MaxRetries=\"5\"", xaml);

        var loaded = SuperXaml.Load(xaml);
        var loadedTarget = FindWriteMessage(loaded);
        Assert.NotNull(loadedTarget);
        Assert.True(ActivityExtensionData.TryGet(loadedTarget!, _ext, "MaxRetries", out var reloaded));
        Assert.Equal(5, reloaded);
    }

    private static WriteMessage? FindWriteMessage(Activity root)
    {
        foreach (var child in WorkflowInspectionServices.GetActivities(root))
        {
            if (child is WriteMessage wm)
            {
                return wm;
            }
            var nested = FindWriteMessage(child);
            if (nested is not null)
            {
                return nested;
            }
        }
        return root as WriteMessage;
    }
}
