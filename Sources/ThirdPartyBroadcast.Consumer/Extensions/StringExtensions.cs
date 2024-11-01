using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Extensions;

public static class StringExtensions
{
    public static ResourcesModel ToResources(this string selectedResourced)
    {
        var serializer = new XmlSerializer(typeof(ResourcesModel));
        using var sr = new StringReader(selectedResourced);

        return (ResourcesModel)serializer.Deserialize(sr);
    }

    public static string ToProductName(this string code)
    {
        var field = typeof(ProductType).GetFields().ToList()
            .Find(f => string.Equals((string)f.GetValue(null), code));

        return field?.Name ?? string.Empty;
    }
}