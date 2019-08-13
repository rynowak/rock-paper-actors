namespace RobbyBot
{
    public class GameResult
    {
        public string GameId { get; set; }
        
        public UserInfo Player { get; set; }

        public UserInfo Opponent { get; set; }

        public Shape PlayerMove { get; set; }

        public Shape OpponentMove { get; set; }

        public bool IsVictory { get; set; }
    }
}