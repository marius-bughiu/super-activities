using System.Xaml;
using System.Xaml.Schema;

namespace Super.Extensions.Xaml;

/// <summary>
/// A dynamic attachable XAML member representing one extension property. It has no underlying CLR
/// accessor methods; get/set go through <see cref="ExtensionXamlMemberInvoker"/>.
/// </summary>
internal sealed class ExtensionXamlMember : XamlMember
{
    private readonly IActivityExtension _extension;
    private readonly ExtensionPropertyDefinition _definition;
    private readonly XamlSchemaContext _context;

    public ExtensionXamlMember(
        IActivityExtension extension,
        ExtensionPropertyDefinition definition,
        XamlType declaringType,
        XamlSchemaContext context)
        : base(definition.Name, declaringType, isAttachable: true)
    {
        _extension = extension;
        _definition = definition;
        _context = context;
    }

    protected override XamlMemberInvoker LookupInvoker()
        => new ExtensionXamlMemberInvoker(this, _extension, _definition);

    protected override XamlType LookupType()
        => _context.GetXamlType(_definition.Type);

    protected override bool LookupIsReadOnly() => false;
    protected override bool LookupIsWriteOnly() => false;
    protected override bool LookupIsReadPublic() => true;
    protected override bool LookupIsWritePublic() => true;
    protected override bool LookupIsUnknown() => false;
}
