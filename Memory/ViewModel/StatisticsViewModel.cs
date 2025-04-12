using System.Collections.ObjectModel;

public class StatisticsViewModel
{
    public ObservableCollection<UserStats> Users { get; set; }

    public StatisticsViewModel()
    {
        var allStats = GameViewModel.LoadAllStats();
        Users = new ObservableCollection<UserStats>(allStats.Users);
    }
}
