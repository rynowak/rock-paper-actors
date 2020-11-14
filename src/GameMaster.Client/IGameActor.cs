using System.Threading.Tasks;
using Dapr.Actors;
using Player;

namespace GameMaster
{
    public interface IGameActor : IActor
    {
        Task InitializeAsync(PlayerInfo[] players);

        Task PlayMoveAsync(PlayerInfo player, Shape move);
    }
}