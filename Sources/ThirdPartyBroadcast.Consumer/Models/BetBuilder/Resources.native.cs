using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models.BetBuilder;

[XmlRoot("Resources")]
public class Resources
{
    [XmlElement("Resource")]
    public List<ResourceItem> ResourceItem { get; set; }
}