using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Models.FM;

[XmlRoot("BetTypes")]
public class BetTypeXML
{
    [XmlElement("BetType")]
    public List<BetTypeElement> BetType { get; set; }
}


public class BetTypeElement
{
    [XmlAttribute]
    public int Id { get; set; }

    [XmlElement("Item")]
    public List<ItemElement> Items { get; set; }
}

public class ItemElement
{
    [XmlAttribute]
    public int Position { get; set; }

    [XmlAttribute]
    public string Result { get; set; }
}