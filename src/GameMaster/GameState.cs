using System.Collections.Generic;
using System.Linq;
using Dapr.Actors;
using Player;

namespace GameMaster
{
    public class GameState
    {
        public string GameId { get; set; }

        public PlayerInfo[] Players { get; set; }

        public Dictionary<string, Shape?> Moves { get; set; }

        public string Winner { get; set; }

        public IEnumerable<(PlayerInfo player, GameResult result)> GetViews()
        {
            foreach (var (player, opponent) in new[]{ (Players[0], Players[1]), (Players[1], Players[0])})
            {
                var outcome = (GameOutcome?)GameOutcome.Draw;
                if (Moves.Any(kvp => kvp.Value == null))
                {
                    outcome = null;
                }
                else
                {
                    outcome = player.Username == Winner ? GameOutcome.Win : GameOutcome.Loss;
                }

                yield return (player, new GameResult()
                {
                    Info = new GameInfo()
                    {
                        Game = new ActorReference()
                        {
                            ActorId = new ActorId(GameId),
                            ActorType = "GameActor",
                        },
                        Player = player,
                        Opponent = opponent,
                    },
                    Moves = Moves,
                    Outcome = outcome,
                });
            }
        }
    }
}