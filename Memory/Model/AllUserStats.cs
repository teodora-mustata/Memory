using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("AllUserStats")]
public class AllUserStats
{
    [XmlElement("UserStats")]
    public List<UserStats> Users { get; set; } = new List<UserStats>();
}
