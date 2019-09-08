using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Scoreboard
{
    public class GameResult
    {
        public string GameId { get; set; }

        public UserInfo[] Players { get; set; }

        public List<PlayerMove> Moves { get; set; }

        public UserInfo Winner { get; set; }

        [JsonIgnore]
        public bool IsComplete => Moves?.Count == 2;

        [JsonIgnore]
        public bool? IsDraw => IsComplete ? Winner == null : (bool?)null;

        public bool? IsVictory(UserInfo player)
        {
            if (!IsComplete)
            {
                return null;
            }

            return Winner.Username == player.Username;
        }
        public PlayerMove GetPlayerMove(UserInfo player)
        {
            if (Moves == null)
            {
                return null;
            }

            for (var i = 0; i < Moves.Count; i++)
            {
                if (Moves[i].Player.Username == player.Username)
                {
                    return Moves[i];
                }
            }

            return null;
        }

        public PlayerMove GetOpponentMove(UserInfo player)
        {
            if (Moves == null)
            {
                return null;
            }

            for (var i = 0; i < Moves.Count; i++)
            {
                if (Moves[i].Player.Username != player.Username)
                {
                    return Moves[i];
                }
            }

            return null;
        }
    }
}