using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models.HappyFive;

[XmlRoot("Resources")]
public class Resources
{
    [XmlElement("Resource")]
    public List<ResourcesItem> ResourceItems { get; set; }
}