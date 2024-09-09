using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models;

[XmlRoot("Resources")]
public class ResourcesModel
{
    [XmlElement("ResourceItem")]
    public List<ResourceItemModel> Items { get; set; }
}