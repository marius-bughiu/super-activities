using System.Xaml;

namespace Super.Extensions.Xaml;

/// <summary>
/// Resolves a registered extension's <see cref="IActivityExtension.DeclaringType"/> to an
/// <see cref="ExtensionXamlType"/> so dynamic attachable members round-trip through XAML. Intercepts
/// both the by-type lookup (serialize) and the by-name lookup (deserialize).
/// </summary>
internal sealed class ExtensionXamlSchemaContext : XamlSchemaContext
{
    private readonly Dictionary<Type, ExtensionXamlType> _cache = new();
    private readonly object _gate = new();

    public override XamlType GetXamlType(Type type)
    {
        var ext = FindExtension(type);
        return ext is not null ? GetOrCreate(ext) : base.GetXamlType(type);
    }

    protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
    {
        var result = base.GetXamlType(xamlNamespace, name, typeArguments);
        if (result?.UnderlyingType is Type t)
        {
            var ext = FindExtension(t);
            if (ext is not null)
            {
                return GetOrCreate(ext);
            }
        }
        return result!;
    }

    private static IActivityExtension? FindExtension(Type type)
    {
        foreach (var ext in SuperExtensions.All)
        {
            if (ext.DeclaringType == type)
            {
                return ext;
            }
        }
        return null;
    }

    private ExtensionXamlType GetOrCreate(IActivityExtension extension)
    {
        lock (_gate)
        {
            if (!_cache.TryGetValue(extension.DeclaringType, out var xamlType))
            {
                xamlType = new ExtensionXamlType(extension, this);
                _cache[extension.DeclaringType] = xamlType;
            }
            return xamlType;
        }
    }
}
