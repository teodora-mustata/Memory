using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using Memory.ViewModel;
using System.IO;
using System.Windows.Documents;
using System.Windows.Threading;
using Memory;
using System.Windows.Controls.Primitives;
using Memory.Model;
using System.Xml.Serialization;
using Memory.View;

public class GameViewModel : INotifyPropertyChanged
{
    public GameModel gameModel { get; private set; }
    private List<Card> flippedCards = new List<Card>();
    private string selectedUser;
    private UserStats selectedUserStats;
    private AllUserStats allUserStats;
    private static readonly string statsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_stats.xml");
    private readonly string savedGamesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "saved_games.xml");
    public ObservableCollection<Card> FlattenedBoard
    {
        get
        {
            if (gameModel?.Board == null)
            {
                return new ObservableCollection<Card>();
            }
            return new ObservableCollection<Card>(gameModel.Board.SelectMany(row => row));
        }
    }

    public string MatchesDisplay => gameModel != null
        ? $"{gameModel.MatchesFound}/{gameModel.TotalMatches} matches found"
        : "Start a new game? :)";

    public string TimeElapsedDisplay => gameModel != null
    ? gameModel.TimeElapsed.ToString(@"mm\:ss")
    : "00:00";


    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string TimeDisplay => gameModel?.TimeElapsed.ToString(@"mm\:ss") ?? "00:00";

    private string _selectedCategory = "Animals";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory != value)
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
            }
        }
    }

    private string _selectedDimension = "Standard (4x4)";

    public string SelectedDimension
    {
        get => _selectedDimension;
        set
        {
            if (_selectedDimension != value)
            {
                _selectedDimension = value;
                OnPropertyChanged(nameof(SelectedDimension));
            }
        }
    }

    private string _selectedTime = "Easy (5 minutes)";
    public string SelectedTime
    {
        get => _selectedTime;
        set
        {
            if (_selectedTime != value)
            {
                _selectedTime = value;
                OnPropertyChanged(nameof(SelectedTime));
            }
        }
    }

    public GameViewModel(string user)
    {
        FlipCardCommand = new RelayCommand<Card>(FlipCard);
        NewGameCommand = new RelayCommand(NewGame);
        OpenStatsCommand = new RelayCommand(OpenStatisticsWindow);
        selectedUser = user;
        allUserStats = LoadAllStats();
        selectedUserStats = allUserStats.Users.FirstOrDefault(u => u.Username == user);
        if (selectedUserStats == null)
        {
            selectedUserStats = new UserStats(user);
            allUserStats.Users.Add(selectedUserStats);
        }
    }
    public ICommand NewGameCommand { get; }
    public void NewGame()
    {
        if (gameModel == null)
        {
            gameModel = new GameModel();
        }

        int size = DimensionToSize(SelectedDimension);
        TimeSpan time = TimeStringToSpan(SelectedTime);

        GenerateBoard(SelectedCategory, size, size);
        StartTimer(time);

        OnPropertyChanged(nameof(gameModel.Rows));
        OnPropertyChanged(nameof(gameModel.Columns));
        OnPropertyChanged(nameof(FlattenedBoard));
        OnPropertyChanged(nameof(MatchesDisplay));
        OnPropertyChanged(nameof(TimeElapsedDisplay));

    }

    public ICommand FlipCardCommand { get; private set; }

    private void FlipCard(Card card)
    {
        if (card == null || card.IsMatched || flippedCards.Count >= 2 || flippedCards.Contains(card))
            return; 

        card.IsFlipped = true;
        flippedCards.Add(card);

        OnPropertyChanged(nameof(FlattenedBoard));

        if (flippedCards.Count == 2)
        {
            CheckForMatch();
            OnPropertyChanged(nameof(MatchesDisplay));
        }
    }

    public ICommand OpenStatsCommand { get; }
    private void OpenStatisticsWindow()
    {
        var statsWindow = new StatisticsWindow();
        statsWindow.Show();
    }

    private async void CheckForMatch()
    {
        if (flippedCards.Count != 2) return;

        if (flippedCards[0].ImagePath == flippedCards[1].ImagePath)
        {
            flippedCards[0].IsMatched = true;
            flippedCards[1].IsMatched = true;

            gameModel.MatchesFound++;
            flippedCards.Clear();

            CheckWinCondition();
        }
        else
        {
            await Task.Delay(1000);
            foreach (var card in flippedCards)
            {
                card.IsFlipped = false;
            }
            flippedCards.Clear();
            OnPropertyChanged(nameof(FlattenedBoard));
        }
    }

    private void CheckWinCondition()
    {
        if (FlattenedBoard.All(card => card.IsMatched))
        {
            gameModel.Timer.Stop();
            selectedUserStats.GamesWon++;
            selectedUserStats.GamesPlayed++;
            SaveAllStats();
            MessageBox.Show("You won!", "Good job!", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private int DimensionToSize(string dim)
    {
        return dim switch
        {
            "Small (2x2)" => 2,
            "Medium-Small (3x4)" => 3,
            "Standard (4x4)" => 4,
            "Medium-Large (5x6)" => 5,
            "Large (6x6)" => 6,
            _ => 4
        };
    }

    private TimeSpan TimeStringToSpan(string time)
    {
        return time switch
        {
            "Easy (5 minutes)" => TimeSpan.FromMinutes(5),
            "Medium (3:30 minutes)" => TimeSpan.FromMinutes(3.5),
            "Hard (2 minutes)" => TimeSpan.FromMinutes(2),
            _ => TimeSpan.FromMinutes(5)
        };
    }
    public void GenerateBoard(string category, int rows, int columns)
    {
        if ((rows * columns) % 2 != 0)
        {
        columns++;
        }

        gameModel.MatchesFound = 0;

        var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Images", category);

        var jpgFiles = Directory.GetFiles(folderPath, "*.jpg");
        var pngFiles = Directory.GetFiles(folderPath, "*.png");
        var webpFiles = Directory.GetFiles(folderPath, "*.webp");
        var jpegFiles = Directory.GetFiles(folderPath, "*.jpeg");

        var imageFiles = jpgFiles.Concat(pngFiles).Concat(webpFiles).Concat(jpegFiles).ToArray();

        gameModel.Rows = rows; gameModel.Columns = columns;
        gameModel.Category = category;

        int totalCards = rows * columns;
        int totalPairs = totalCards / 2;

        var selectedImages = imageFiles.OrderBy(x => Guid.NewGuid())
                                       .Take(totalPairs)
                                       .ToList();

        var allCards = selectedImages.SelectMany(img => new[]
        {
        new Card { ImagePath = img },
            new Card { ImagePath = img }
    }).OrderBy(x => Guid.NewGuid()).ToList();

        gameModel.Board = new ObservableCollection<ObservableCollection<Card>>();

        int index = 0;

        for (int r = 0; r < rows; r++)
        {
            var row = new ObservableCollection<Card>();
            for (int c = 0; c < columns; c++)
            {
                var card = allCards[index++];
                card.Row = r;
                card.Column = c;
                row.Add(card);
            }
            gameModel.Board.Add(row);
        }
    }

    public void RestoreBoard(List<Card> savedCards, int rows, int columns)
    {
        gameModel.Board = new ObservableCollection<ObservableCollection<Card>>();

        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            var row = new ObservableCollection<Card>();
            for (int c = 0; c < columns; c++)
            {
                var card = savedCards[index++];
                card.Row = r;
                card.Column = c;
                row.Add(card);
            }
            gameModel.Board.Add(row);
        }
    }

    public void StartTimer(TimeSpan startingTime)
    {
        if (gameModel.Timer != null)
        {
            gameModel.Timer.Stop();
            gameModel.Timer = null; 
        }


        gameModel.TimeElapsed = startingTime;

        gameModel.Timer = new DispatcherTimer();
        gameModel.Timer.Interval = TimeSpan.FromSeconds(1);
        gameModel.Timer.Tick += (s, e) =>
        {
            gameModel.TimeElapsed = gameModel.TimeElapsed.Subtract(TimeSpan.FromSeconds(1));
            OnPropertyChanged(nameof(TimeElapsedDisplay));

            if (gameModel.TimeElapsed <= TimeSpan.Zero)
            {
                gameModel.Timer.Stop();
                selectedUserStats.GamesPlayed++;
                SaveAllStats();
                MessageBox.Show("You lost! :(", "Try again?", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        };
        gameModel.Timer.Start();
    }

    public static AllUserStats LoadAllStats()
    {
        if (!File.Exists(statsFile))
            return new AllUserStats();

        XmlSerializer serializer = new XmlSerializer(typeof(AllUserStats));
        using (FileStream fs = new FileStream(statsFile, FileMode.Open))
        {
            return (AllUserStats)serializer.Deserialize(fs);
        }
    }

    private void SaveAllStats()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(AllUserStats));
        using (FileStream fs = new FileStream(statsFile, FileMode.Create))
        {
            serializer.Serialize(fs, allUserStats);
        }
    }

    public ICommand SaveGameCommand => new RelayCommand(SaveGame);
    public ICommand LoadGameCommand => new RelayCommand(LoadSavedGame);
    public void SaveGame()
    {
        if (gameModel == null)
        {
            MessageBox.Show("No game to save! Please create one.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        AllSavedGames all;
        if (File.Exists(savedGamesFile))
        {
            using (FileStream fs = new FileStream(savedGamesFile, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AllSavedGames));
                all = (AllSavedGames)serializer.Deserialize(fs);
            }
        }
        else
        {
            all = new AllSavedGames();
        }

        all.Games.RemoveAll(g => g.Username == selectedUserStats.Username);

        var savedGame = new SavedGame(
            selectedUserStats.Username,
            SelectedCategory,
            gameModel.Rows,
            gameModel.Columns,
            gameModel.TimeElapsed,
            FlattenedBoard.ToList()
        );

        all.Games.Add(savedGame);

        using (FileStream fs = new FileStream(savedGamesFile, FileMode.Create))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AllSavedGames));
            serializer.Serialize(fs, all);
        }

        MessageBox.Show("Game saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void LoadSavedGame()
    {
        if (!File.Exists(savedGamesFile))
        {
            MessageBox.Show("No saved games found.", "Oops", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AllSavedGames all;
        using (FileStream fs = new FileStream(savedGamesFile, FileMode.Open))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AllSavedGames));
            all = (AllSavedGames)serializer.Deserialize(fs);
        }

        var saved = all.Games.FirstOrDefault(g => g.Username == selectedUserStats.Username);
        if (saved == null)
        {
            MessageBox.Show("You have no saved game!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (gameModel == null)
            gameModel = new GameModel();

        gameModel.Rows = saved.Rows;
        gameModel.Columns = saved.Columns;
        gameModel.Category = saved.Category;
        gameModel.MatchesFound = saved.MatchesFound;

        RestoreBoard(saved.FlattenedBoard, saved.Rows, saved.Columns);
        StartTimer(saved.TimeLeft);

        MessageBox.Show("Game loaded!", "Welcome back", MessageBoxButton.OK, MessageBoxImage.Information);
        OnPropertyChanged(nameof(gameModel.Rows));
        OnPropertyChanged(nameof(gameModel.Columns));
        OnPropertyChanged(nameof(FlattenedBoard));
        OnPropertyChanged(nameof(MatchesDisplay));
        OnPropertyChanged(nameof(TimeElapsedDisplay));
    }

}
