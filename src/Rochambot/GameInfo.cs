namespace Rochambot
{
    public class GameInfo
    {
        public string GameId { get; set; }

        public UserInfo Player { get; set; }
        
        public UserInfo Opponent { get; set; }
    }
}