using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models;

[XmlRoot("Resources")]
public class ResourcesMarketModel
{
    [XmlElement("rsc")]
    public List<ResourceMarketItemModel> Items { get; set; }
}