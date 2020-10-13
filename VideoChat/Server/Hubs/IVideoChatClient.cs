using System.Threading.Tasks;

namespace VideoChat.Server.Hubs
{
    public interface IVideoChatClient
    {
        Task UpdateUsersList(string users);
    }
}
