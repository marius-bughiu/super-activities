namespace Super.Extensions;

/// <summary>
/// Describes one extra property an <see cref="IActivityExtension"/> contributes to an activity.
/// The value is stored as an attached property on the activity instance and surfaced both at
/// design time (property grid) and runtime (the before/after hook).
/// </summary>
public sealed class ExtensionPropertyDefinition
{
    public ExtensionPropertyDefinition(string name, Type type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>Plain property name. Must not contain '.' (it becomes a XAML attachable member name).</summary>
    public string Name { get; }

    public Type Type { get; }

    public object? DefaultValue { get; init; }

    /// <summary>Maps to <see cref="System.ComponentModel.CategoryAttribute"/> in a property grid.</summary>
    public string? Category { get; init; }

    /// <summary>Maps to <see cref="System.ComponentModel.DescriptionAttribute"/> in a property grid.</summary>
    public string? Description { get; init; }

    public bool Browsable { get; init; } = true;
}
