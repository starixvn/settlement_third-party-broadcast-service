using Fanex.Settlement.MessageBridge.ResultAmendment;
using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models.FM;
[XmlRoot("Resources")]
public class Resources
{
    [XmlElement("Resource")]
    public List<ResourceItem> ResourceItems { get; set; }
}