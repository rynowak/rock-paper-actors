using System;

namespace Rochambot
{
    public class SessionResult
    {
        public PlayerResult Player1 { get; set; }
        public PlayerResult Player2 { get; set; }
        public DateTime SessionEnd { get; set; }
        public string Summary { get; set; }
        public bool IsTie => !Player1.IsWinner && !Player2.IsWinner;
    }
}