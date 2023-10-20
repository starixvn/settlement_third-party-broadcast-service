using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models;

[XmlRoot("Messages")]
public class MessageAnnounence
{
    [XmlElement("Item")]
    public List<MessageAnnounenceItem> Items { get; set; }
}

public class MessageAnnounenceItem
{
    [XmlAttribute]
    public int MessageId { get; set; }

    [XmlAttribute]
    public int TargetId { get; set; }

    [XmlAttribute]
    public string TargetName { get; set; }

    [XmlAttribute]
    public string BetTransId { get; set; }
}