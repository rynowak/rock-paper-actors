using System;

namespace GameMaster
{
    public class SessionResult
    {
        public PlayerResult Player1 { get; set; }
        public PlayerResult Player2 { get; set; }
        public DateTime SessionEnd { get; set; }
        public string Summary { get; set; }
        public bool IsTie
        {
            get
            {
                return !Player1.IsWinner && !Player2.IsWinner;
            }
        }
    }

    public class PlayerResult
    {
        public string PlayerId { get; set; }
        public Shape ShapeSelected { get; set; }
        public bool IsWinner { get; set; }
    }
}