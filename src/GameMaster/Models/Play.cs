using Rochambot;

namespace GameMaster
{
    public class Play
    {
        public string PlayerId { get; set; }
        public Shape ShapeSelected { get; set; }
        public bool IsWinner { get; set; }
    }
}