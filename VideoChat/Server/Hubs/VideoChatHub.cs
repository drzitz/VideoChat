using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoChat.Shared.Models;
using VideoChat.Shared.Extensions;
using System.Linq;
using System;

namespace VideoChat.Server.Hubs
{
    public class VideoChatHub : Hub<IVideoChatClient>
    {
        private static readonly List<User> Users = new List<User>();
        private static readonly List<UserCall> UserCalls = new List<UserCall>();
        private static readonly List<CallOffer> CallOffers = new List<CallOffer>();

        public async Task Join(string userName)
        {
            var existing = Users.FirstOrDefault(x => x.Name == userName);

            if (existing != null)
            {
                existing.Name = userName;
                existing.ConnectionId = Context.ConnectionId;
            }
            else
            {
                Users.Add(new User
                {
                    Name = userName,
                    ConnectionId = Context.ConnectionId
                });
            }

            await SendUsersListUpdate();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Users.RemoveAll(x => x.ConnectionId == Context.ConnectionId);
            await SendUsersListUpdate();

            await base.OnDisconnectedAsync(exception);
        }

        private Task SendUsersListUpdate()
        {
            return Clients.All.UpdateUsersList(Users.ToJson());
        }
    }
}
