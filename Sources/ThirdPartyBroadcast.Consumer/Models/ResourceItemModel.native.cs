using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models;

public class ResourceItemModel
{
    [XmlAttribute("Id")]
    public string Id { get; set; }

    [XmlAttribute("Home")]
    public string Home { get; set; }

    [XmlAttribute("Away")]
    public string Away { get; set; }
}