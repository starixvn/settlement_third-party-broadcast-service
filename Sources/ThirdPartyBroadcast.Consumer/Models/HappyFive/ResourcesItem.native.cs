using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models.HappyFive;

public class ResourcesItem
{
    [XmlAttribute("Id")]
    public byte Seq { get; set; }

    [XmlAttribute("Result")]
    public byte NumberBall { get; set; }
}