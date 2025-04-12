using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;

public class GameModel
{
    public int Rows { get; set; }
    public int Columns { get; set; }

    public ObservableCollection<ObservableCollection<Card>> Board { get; set; }

    public string Category { get; set; }

    public int MatchesFound { get; set; }
    public int TotalMatches => (Rows * Columns) / 2;

    public DispatcherTimer Timer { get; set; }
    public TimeSpan TimeElapsed { get; set; }

}
