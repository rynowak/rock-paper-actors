using Dapr.Actors;

namespace Player
{
    public class PlayerInfo
    {
        public ActorReference Player { get; set; }

        public string Username { get; set; }

        public bool IsBot { get; set; }
    }
}