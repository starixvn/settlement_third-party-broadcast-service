using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models;

public class ResourceMarketItemModel
{
    [XmlAttribute("m")]
    public string MarketId { get; set; }
}