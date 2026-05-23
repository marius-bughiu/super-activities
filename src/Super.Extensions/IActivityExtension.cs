using System.Activities;

namespace Super.Extensions;

/// <summary>
/// Implemented by a plugin author to extend existing activities without modifying their source.
/// Register an instance with <see cref="SuperExtensions.Register"/>; the engine then surfaces
/// <see cref="Properties"/> at design time, persists their values in XAML, and invokes the
/// before/after hooks at runtime.
/// </summary>
public interface IActivityExtension
{
    /// <summary>Unique, stable identifier for this extension.</summary>
    string Name { get; }

    /// <summary>
    /// The CLR type that owns this extension's attached properties. Each extension should use a
    /// distinct type so property names never collide and so XAML renders as
    /// <c>&lt;ns:DeclaringType.PropertyName&gt;</c>. Typically the extension's own type.
    /// </summary>
    Type DeclaringType { get; }

    /// <summary>
    /// XAML namespace mapping for <see cref="DeclaringType"/>, e.g.
    /// <c>clr-namespace:My.Plugins;assembly=My.Plugins</c>. Used when serializing/deserializing.
    /// </summary>
    string XmlNamespace { get; }

    /// <summary>Returns true for activities this extension should augment.</summary>
    bool AppliesTo(Activity activity);

    /// <summary>The extra properties this extension contributes.</summary>
    IReadOnlyList<ExtensionPropertyDefinition> Properties { get; }

    /// <summary>
    /// Return true if this extension needs each activity's argument values captured (extra overhead).
    /// When any registered extension opts in, <see cref="SuperInitialize"/> enables argument capture and
    /// the values arrive in <see cref="ExtensionExecutionContext.Arguments"/>.
    /// </summary>
    bool CapturesArguments => false;

    /// <summary>Invoked just before an applicable activity executes (state == Executing).</summary>
    void OnBeforeExecute(ExtensionExecutionContext context);

    /// <summary>Invoked after an applicable activity completes (Closed / Faulted / Canceled).</summary>
    void OnAfterExecute(ExtensionExecutionContext context);
}
