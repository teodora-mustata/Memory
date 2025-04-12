public class UserStats
{
    public string Username { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }

    public UserStats() { }
    public UserStats(string username)
    {
        Username = username;
        GamesPlayed = 0;
        GamesWon = 0;
    }
}
