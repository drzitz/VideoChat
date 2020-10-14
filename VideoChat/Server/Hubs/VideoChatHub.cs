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

        public void CallUser(string connectionId)
        {
            var caller = Users.SingleOrDefault(u => u.ConnectionId == Context.ConnectionId);
            var callee = Users.SingleOrDefault(u => u.ConnectionId == connectionId);

            if (callee == null)
            {
                Clients.Caller.CallDeclined(connectionId, "The user you called has left");
                return;
            }

            if (GetUserCall(callee.ConnectionId) != null)
            {
                Clients.Caller.CallDeclined(connectionId, string.Format("{0} is already in a call", callee.Name));
                return;
            }

            Clients.Client(connectionId).IncomingCall(caller.ConnectionId);

            CallOffers.Add(new CallOffer
            {
                Caller = caller,
                Callee = callee
            });
        }
        
        public void AnswerCall(bool accept, string connectionId)
        {
            var caller = Users.SingleOrDefault(x => x.ConnectionId == Context.ConnectionId);
            var callee = Users.SingleOrDefault(x => x.ConnectionId == connectionId);

            if (caller == null)
            {
                return;
            }

            if (callee == null)
            {
                Clients.Caller.CallEnded(connectionId, "The other user in your call has left");
                return;
            }

            if (accept == false)
            {
                Clients.Client(connectionId).CallDeclined(caller.ConnectionId, string.Format("{0} did not accept your call", caller.Name));
                return;
            }

            var offerCount = CallOffers.RemoveAll(x => x.Callee.ConnectionId == caller.ConnectionId && x.Caller.ConnectionId == callee.ConnectionId);
            if (offerCount < 1)
            {
                Clients.Caller.CallEnded(connectionId, string.Format("{0} has already hung up.", callee.Name));
                return;
            }

            if (GetUserCall(callee.ConnectionId) != null)
            {
                Clients.Caller.CallDeclined(connectionId, string.Format("{0} currently in another call", callee.Name));
                return;
            }

            CallOffers.RemoveAll(c => c.Caller.ConnectionId == callee.ConnectionId);

            UserCalls.Add(new UserCall
            {
                Users = new List<User> { caller, callee }
            });

            Clients.Client(connectionId).CallAccepted(caller.ConnectionId);

            SendUsersListUpdate();
        }

        public void HangUp()
        {
            var caller = Users.SingleOrDefault(u => u.ConnectionId == Context.ConnectionId);

            if (caller == null)
            {
                return;
            }

            var currentCall = GetUserCall(caller.ConnectionId);

            if (currentCall != null)
            {
                foreach (var user in currentCall.Users.Where(x => x.ConnectionId != caller.ConnectionId))
                {
                    Clients.Client(user.ConnectionId).CallEnded(caller.ConnectionId, string.Format("{0} has hung up", caller.Name));
                }

                currentCall.Users.RemoveAll(u => u.ConnectionId == caller.ConnectionId);

                if (currentCall.Users.Count < 2)
                {
                    UserCalls.Remove(currentCall);
                }
            }

            CallOffers.RemoveAll(x => x.Caller.ConnectionId == caller.ConnectionId);

            SendUsersListUpdate();
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

        private UserCall GetUserCall(string connectionId)
        {
            return UserCalls.SingleOrDefault(x => x.Users.SingleOrDefault(u => u.ConnectionId == connectionId) != null);
        }
    }
}
