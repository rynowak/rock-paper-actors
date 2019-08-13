using System;

namespace Rochambot
{
    public class GameService
    {
        public event EventHandler<GameInfo> GameChanged;

        private GameInfo gameInfo;

        public GameInfo CurrentGame
        {
            get => gameInfo;
            set
            {
                gameInfo = value;
                GameChanged?.Invoke(this, value);
            }
        }
    }
}