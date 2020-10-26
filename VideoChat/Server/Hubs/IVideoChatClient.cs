using System.Threading.Tasks;

namespace VideoChat.Server.Hubs
{
    public interface IVideoChatClient
    {
        Task UpdateOnlineUsers(string users);
        Task CallAccepted(string connectionId);
        Task CallDeclined(string message);
        Task CallDenied(string message);
        Task CallEnded(string message);
        Task IncomingCall(string connectionId);
        Task ReceiveSignal(string connectionId, string data);

        Task UpdateUsers(string users);
        Task UpdateCalls(string calls);
        Task CallAborted(string message);
    }
}
