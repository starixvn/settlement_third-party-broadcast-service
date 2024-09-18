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
}