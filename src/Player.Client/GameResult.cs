using System.Collections.Generic;

namespace Player
{
    public class GameResult
    {
        public GameInfo Info { get; set; }

        public Dictionary<string, Shape?> Moves { get; set; }

        public GameOutcome? Outcome { get; set; }

        public Shape? GetPlayerMove()
        {
            Moves.TryGetValue(Info.Player.Username, out var move);
            return move;
        }

        public Shape? GetOpponentMove()
        {
            Moves.TryGetValue(Info.Opponent.Username, out var move);
            return move;
        }
    }
}