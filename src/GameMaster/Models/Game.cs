using Newtonsoft.Json;

namespace GameMaster
{
    public class Game
    {
        [JsonProperty(PropertyName = "id")]
        public string GameId { get; set; }
        public int NumberOfTurnsNeededToWin { get; set; } = 3; // default is best-to-3
        public Turn[] Turns { get; set; }
        public string WinnerPlayerId { get; set; }
    }
}