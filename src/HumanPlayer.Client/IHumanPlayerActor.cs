using System.Threading.Tasks;
using Dapr.Actors;
using Player;

namespace HumanPlayer
{
    public interface IHumanPlayerActor : IActor
    {
        Task RequestGameAsync();

        Task PlayMoveAsync(Shape move);
    }
}