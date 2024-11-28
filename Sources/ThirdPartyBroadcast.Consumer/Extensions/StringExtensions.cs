using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Extensions;

public static class StringExtensions
{
    private static XmlSerializerNamespaces NamespaceSettings
    {
        get
        {
            var setting = new XmlSerializerNamespaces();
            setting.Add(string.Empty, string.Empty);

            return setting;
        }
    }

    private static XmlWriterSettings Settings => new()
    {
        Encoding = new UnicodeEncoding(false, false),
        Indent = false,
        OmitXmlDeclaration = true
    };

    public static ResourcesModel ToResources(this string selectedResourced)
    {
        var serializer = new XmlSerializer(typeof(ResourcesModel));
        using var sr = new StringReader(selectedResourced);

        return (ResourcesModel)serializer.Deserialize(sr);
    }

    public static string ToStrings(this List<ResourceItemModel> resources)
    {
        var resourcesModel = new ResourcesModel
        {
            Items = resources
        };

        var serializer = new XmlSerializer(typeof(ResourcesModel));

        var stringBuilder = new StringBuilder();

        using var xmlWriter = XmlWriter.Create(stringBuilder, Settings);
        serializer.Serialize(xmlWriter, resourcesModel, NamespaceSettings);

        return stringBuilder.ToString();
    }

    public static string ToProductName(this string code)
    {
        var field = typeof(ProductType).GetFields().ToList()
            .Find(f => string.Equals((string)f.GetValue(null), code));

        return field?.Name ?? string.Empty;
    }
}