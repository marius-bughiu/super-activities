using System.Activities;
using System.ComponentModel;

namespace Super.Extensions;

/// <summary>
/// A <see cref="TypeDescriptionProvider"/> registered against <see cref="Activity"/> so that every
/// activity (the base type registration flows to derived types) exposes its applicable extension
/// properties through <see cref="TypeDescriptor"/>.
/// </summary>
internal sealed class ExtensionTypeDescriptionProvider : TypeDescriptionProvider
{
    public ExtensionTypeDescriptionProvider(TypeDescriptionProvider parent)
        : base(parent)
    {
    }

    public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
    {
        var parentDescriptor = base.GetTypeDescriptor(objectType, instance);
        return new ExtensionCustomTypeDescriptor(parentDescriptor, instance as Activity);
    }
}
