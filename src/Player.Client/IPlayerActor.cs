using System.Threading.Tasks;
using Dapr.Actors;

namespace Player
{
    public interface IPlayerActor : IActor
    {
        Task NotifyGameReadyAsync(GameInfo game);

        Task NotifyGameCompletedAsync(GameResult result);
    }
}