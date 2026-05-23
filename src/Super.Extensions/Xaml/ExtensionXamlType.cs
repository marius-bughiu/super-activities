using System.Xaml;

namespace Super.Extensions.Xaml;

/// <summary>
/// Represents an extension's <see cref="IActivityExtension.DeclaringType"/> as a XAML attach owner,
/// surfacing the extension's properties as dynamic attachable members.
/// </summary>
internal sealed class ExtensionXamlType : XamlType
{
    private readonly IActivityExtension _extension;
    private readonly XamlSchemaContext _context;

    public ExtensionXamlType(IActivityExtension extension, XamlSchemaContext context)
        : base(extension.DeclaringType, context)
    {
        _extension = extension;
        _context = context;
    }

    protected override XamlMember LookupAttachableMember(string name)
    {
        foreach (var def in _extension.Properties)
        {
            if (def.Name == name)
            {
                return new ExtensionXamlMember(_extension, def, this, _context);
            }
        }
        return base.LookupAttachableMember(name);
    }

    protected override IEnumerable<XamlMember> LookupAllAttachableMembers()
    {
        var members = new List<XamlMember>();
        var baseMembers = base.LookupAllAttachableMembers();
        if (baseMembers is not null)
        {
            members.AddRange(baseMembers);
        }
        foreach (var def in _extension.Properties)
        {
            members.Add(new ExtensionXamlMember(_extension, def, this, _context));
        }
        return members;
    }
}
