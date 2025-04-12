using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("AllSavedGames")]
public class AllSavedGames
{
    [XmlElement("SavedGame")]
    public List<SavedGame> Games { get; set; } = new List<SavedGame>();
}
