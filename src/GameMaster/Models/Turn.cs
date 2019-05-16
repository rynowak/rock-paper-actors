using System;

namespace GameMaster
{
    public class Turn
    {
        public Play Player1 { get; set; }
        public Play Player2 { get; set; }
        public DateTime TurnEnded { get; set; }
        public string Summary { get; set; }
        public bool IsTie => (Player1 != null && !Player1.IsWinner) && (Player2 != null && !Player2.IsWinner);
    }
}