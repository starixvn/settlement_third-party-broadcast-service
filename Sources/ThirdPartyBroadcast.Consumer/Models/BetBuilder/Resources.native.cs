using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models.BetBuilder;

[XmlRoot("Resources")]
public class Resources
{
    [XmlElement("ResourceItem")]
    public List<ResourceItem> ResourceItem { get; set; }
}