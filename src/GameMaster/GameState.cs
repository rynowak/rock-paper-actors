using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameMaster
{
    public class GameState
    {
        public string GameId { get; set; }

        public UserInfo[] Players { get; set; }

        public List<PlayerMove> Moves { get; set; }

        public UserInfo Winner { get; set; }

        [JsonIgnore]
        public bool IsComplete => Moves?.Count == 2;

        [JsonIgnore]
        public bool? IsDraw => IsComplete ? Winner == null : (bool?)null;
    }
}