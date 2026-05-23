using System.Activities;
using System.ComponentModel;

namespace Super.Extensions;

/// <summary>
/// A synthetic <see cref="PropertyDescriptor"/> that exposes one extension property to any
/// <see cref="TypeDescriptor"/>-driven surface (e.g. a property grid). Get/Set are backed by the
/// same attached-property store the runtime reads, so design-time edits and runtime reads agree.
/// </summary>
internal sealed class ExtensionPropertyDescriptor : PropertyDescriptor
{
    private readonly IActivityExtension _extension;
    private readonly ExtensionPropertyDefinition _definition;

    public ExtensionPropertyDescriptor(IActivityExtension extension, ExtensionPropertyDefinition definition)
        : base(definition.Name, BuildAttributes(definition))
    {
        _extension = extension;
        _definition = definition;
    }

    private static Attribute[] BuildAttributes(ExtensionPropertyDefinition def)
    {
        var attributes = new List<Attribute>();
        if (!string.IsNullOrEmpty(def.Category))
        {
            attributes.Add(new CategoryAttribute(def.Category));
        }
        if (!string.IsNullOrEmpty(def.Description))
        {
            attributes.Add(new DescriptionAttribute(def.Description));
        }
        attributes.Add(new BrowsableAttribute(def.Browsable));
        return attributes.ToArray();
    }

    public override Type ComponentType => typeof(Activity);

    public override Type PropertyType => _definition.Type;

    public override bool IsReadOnly => false;

    public override bool CanResetValue(object component) => GetValue(component) is not null;

    public override void ResetValue(object component)
    {
        if (component is Activity activity)
        {
            ActivityExtensionData.Remove(activity, _extension, _definition.Name);
            OnValueChanged(component, EventArgs.Empty);
        }
    }

    public override bool ShouldSerializeValue(object component)
        => component is Activity activity && ActivityExtensionData.TryGet(activity, _extension, _definition.Name, out _);

    public override object? GetValue(object? component)
    {
        if (component is Activity activity && ActivityExtensionData.TryGet(activity, _extension, _definition.Name, out var value))
        {
            return value;
        }
        return _definition.DefaultValue;
    }

    public override void SetValue(object? component, object? value)
    {
        if (component is Activity activity)
        {
            ActivityExtensionData.Set(activity, _extension, _definition.Name, value);
            OnValueChanged(component, EventArgs.Empty);
        }
    }
}
