using System;

namespace Rochambot
{
    public class SessionResult
    {
        public PlayerResult Player1 { get; set; }
        public PlayerResult Player2 { get; set; }
        public DateTime SessionEnd { get; set; }
    }

    public class PlayerResult
    {
        public Guid PlayerId { get; set; }
        public Shape ShapeSelected { get; set; }
        public bool IsWinner { get; set; }
    }
}