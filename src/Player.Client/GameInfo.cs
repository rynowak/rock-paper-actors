using System.Text.Json.Serialization;
using Dapr.Actors;

namespace Player
{
    public class GameInfo
    {
        public ActorReference Game { get; set; }

        [JsonIgnore]
        public string GameId => Game.ActorId.GetId();

        public PlayerInfo Player { get; set; }

        public PlayerInfo Opponent { get; set; }
    }
}