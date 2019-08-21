using System.Threading.Tasks;

namespace MatchMaker
{
    public class QueueEntry
    {
        public QueueEntry(UserInfo user)
        {
            User = user;
            Completion = new TaskCompletionSource<GameInfo>();
        }

        public UserInfo User { get; }

        public TaskCompletionSource<GameInfo> Completion { get; }
    }
}