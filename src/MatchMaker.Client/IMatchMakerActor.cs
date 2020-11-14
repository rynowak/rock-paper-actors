using System.Threading.Tasks;
using Dapr.Actors;
using Player;

namespace MatchMaker
{
    public interface IMatchMakerActor : IActor
    {
        Task RequestGameAsync(PlayerInfo player);
    }
}