using System.Activities;
using System.Activities.XamlIntegration;
using System.Text;
using System.Xaml;
using System.Xml;
using Super.Extensions.Xaml;

namespace Super.Extensions;

/// <summary>
/// Loads/saves workflows to XAML while preserving extension properties (stored as attached
/// properties) via a custom <see cref="XamlSchemaContext"/>. Registered extensions must be present
/// before calling so their declaring types resolve.
/// </summary>
public static class SuperXaml
{
    public static string Save(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var context = new ExtensionXamlSchemaContext();
        var builder = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };

        using (var xmlWriter = XmlWriter.Create(builder, settings))
        using (var xamlWriter = new XamlXmlWriter(xmlWriter, context))
        {
            // The object reader uses our context to resolve and emit the attached extension members.
            var objectReader = new XamlObjectReader(activity, context);
            XamlServices.Transform(objectReader, xamlWriter);
        }

        return builder.ToString();
    }

    public static Activity Load(string xaml)
    {
        ArgumentNullException.ThrowIfNull(xaml);

        var context = new ExtensionXamlSchemaContext();
        using var stringReader = new StringReader(xaml);
        using var xmlReader = XmlReader.Create(stringReader);
        using var innerReader = new XamlXmlReader(xmlReader, context);
        using var reader = ActivityXamlServices.CreateReader(innerReader, context);
        return ActivityXamlServices.Load(reader);
    }
}
