using System.Threading.Tasks;

namespace VideoChat.Server.Hubs
{
    public interface IVideoChatClient
    {
        Task UpdateOnlineUsers(string users);
        Task CallAccepted(string connectionId);
        Task CallDeclined(string message);
        Task CallEnded(string message);
        Task IncomingCall(string connectionId);
        Task ReceiveSignal(string connectionId, string data);
    }
}
