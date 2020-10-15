using System.Threading.Tasks;

namespace VideoChat.Server.Hubs
{
    public interface IVideoChatClient
    {
        Task UpdateUsersList(string users);
        Task CallAccepted(string connectionId);
        Task CallDeclined(string connectionId, string message);
        Task CallEnded(string connectionId, string message);
        Task IncomingCall(string connectionId);
        Task ReceiveSignal(string connectionId, string data);
    }
}
