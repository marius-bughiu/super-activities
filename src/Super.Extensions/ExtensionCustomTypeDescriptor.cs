using System.Activities;
using System.ComponentModel;

namespace Super.Extensions;

/// <summary>
/// Wraps the activity's normal type descriptor and appends one property per applicable
/// extension property.
/// </summary>
internal sealed class ExtensionCustomTypeDescriptor : CustomTypeDescriptor
{
    private readonly Activity? _activity;

    public ExtensionCustomTypeDescriptor(ICustomTypeDescriptor? parent, Activity? activity)
        : base(parent)
    {
        _activity = activity;
    }

    public override PropertyDescriptorCollection GetProperties() => GetProperties(null);

    public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        var baseProperties = base.GetProperties(attributes);
        if (_activity is null)
        {
            return baseProperties;
        }

        var list = new List<PropertyDescriptor>(baseProperties.Count);
        foreach (PropertyDescriptor property in baseProperties)
        {
            list.Add(property);
        }

        foreach (var ext in SuperExtensions.ForActivity(_activity))
        {
            foreach (var def in ext.Properties)
            {
                list.Add(new ExtensionPropertyDescriptor(ext, def));
            }
        }

        return new PropertyDescriptorCollection(list.ToArray());
    }
}
