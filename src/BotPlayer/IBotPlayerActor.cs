using System.Threading.Tasks;
using Dapr.Actors;

namespace BotPlayer
{
    public interface IBotPlayerActor : IActor
    {
        Task InitializeAsync();
    }
}