using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models.BetBuilder;

public class ResourceItem
{
    [XmlAttribute]
    public int Id { get; set; }

    [XmlAttribute]
    public string Result { get; set; }
}