using System;
using System.Collections.Generic;
using System.Xml.Serialization;

public class SavedGame
{
    public string Username { get; set; }
    public string Category { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public TimeSpan TimeLeft { get; set; }
    public int MatchesFound { get; set; }

    [XmlArray("Cards")]
    [XmlArrayItem("Card")]
    public List<Card> FlattenedBoard { get; set; }

    public SavedGame() { }

    public SavedGame(string username, string category, int rows, int columns, TimeSpan timeLeft, List<Card> cards)
    {
        Username = username;
        Category = category;
        Rows = rows;
        Columns = columns;
        TimeLeft = timeLeft;
        FlattenedBoard = cards;
        MatchesFound = FlattenedBoard.Count(c => c.IsMatched) / 2;
    }
}
