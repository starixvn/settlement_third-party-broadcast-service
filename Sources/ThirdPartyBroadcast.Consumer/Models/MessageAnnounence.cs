using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models;

public class Message
{
    [XmlElement("Item")]
    public List<MessageAnnounence> Items { get; set; }
}

public class MessageAnnounence
{
    [XmlAttribute]
    public int MessageId { get; set; }

    [XmlAttribute]
    public int TargetId { get; set; }

    [XmlAttribute]
    public int TargetName { get; set; }

    [XmlAttribute]
    public string BetTransId { get; set; }
}