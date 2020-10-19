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
            var caller = GetCaller();
            var callee = GetCallee(connectionId);

            if (callee == null)
            {
                SendCallDeclined(caller, callee, UserAction.Leave);
                return;
            }

            if (GetUserCall(callee.ConnectionId) != null)
            {
                SendCallDeclined(caller, callee, UserAction.Busy);
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
            var caller = GetCaller();
            var callee = GetCallee(connectionId);

            if (caller == null)
            {
                return;
            }

            if (callee == null)
            {
                SendCallEnded(caller, callee, UserAction.Leave);
                return;
            }

            if (accept == false)
            {
                SendCallDeclined(callee, caller, UserAction.Decline);
                return;
            }

            var offerCount = CallOffers.RemoveAll(x => x.Callee == caller && x.Caller == callee);
            if (offerCount < 1)
            {
                SendCallEnded(caller, callee, UserAction.HangUp);
                return;
            }

            if (GetUserCall(callee.ConnectionId) != null)
            {
                SendCallDeclined(caller, callee, UserAction.Busy);
                return;
            }

            CallOffers.RemoveAll(c => c.Caller == callee);

            UserCalls.Add(new UserCall
            {
                Users = new List<User> { caller, callee }
            });

            Clients.Client(connectionId).CallAccepted(caller.ConnectionId);

            SendUsersListUpdate();
        }

        public void HangUp()
        {
            var caller = GetCaller();

            if (caller == null)
            {
                return;
            }

            var currentCall = GetUserCall(caller.ConnectionId);

            if (currentCall != null)
            {
                foreach (var user in currentCall.Users.Where(x => x != caller))
                {
                    SendCallEnded(user, caller, UserAction.HangUp);
                }

                currentCall.Users.RemoveAll(x => x == caller);

                if (currentCall.Users.Count < 2)
                {
                    UserCalls.Remove(currentCall);
                }
            }

            foreach (var offer in CallOffers.Where(x => x.Caller == caller).ToArray())
            {
                SendCallEnded(offer.Callee, caller, UserAction.Cancel);
                CallOffers.Remove(offer); 
            }

            SendUsersListUpdate();
        }

        public void SendSignal(string signal, string connectionId)
        {
            var caller = GetCaller();
            var callee = GetCallee(connectionId);

            if (caller == null || callee == null)
            {
                return;
            }

            var userCall = GetUserCall(caller.ConnectionId);

            if (userCall != null && userCall.Users.Exists(x => x == callee))
            {
                Clients.Client(connectionId).ReceiveSignal(caller.ConnectionId, signal);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var caller = GetCaller();

            if (caller == null)
            {
                return;
            }

            foreach (var call in UserCalls.Where(x => x.Users.Any(x => x == caller)).ToArray())
            {
                var otherUser = call.Users.SingleOrDefault(x => x != caller);
                _ = SendCallEnded(otherUser, caller, UserAction.Leave);

                UserCalls.Remove(call);
            }

            foreach (var offer in CallOffers.Where(x => x.Caller == caller || x.Callee == caller).ToArray())
            {
                var otherUser = offer.Caller == caller ? offer.Callee : offer.Caller;
                _ = SendCallEnded(otherUser, caller, UserAction.Leave);

                CallOffers.Remove(offer);
            }

            Users.Remove(caller);
            await SendUsersListUpdate();

            await base.OnDisconnectedAsync(exception);
        }


        private Task SendUsersListUpdate()
        {
            return Clients.All.UpdateUsersList(Users.ToJson());
        }

        private Task SendCallEnded(User to, User from, UserAction action)
        {
            return Clients.Client(to.ConnectionId).CallEnded(new ActionMessage { User = from, Action = action }.ToJson());
        }

        private Task SendCallDeclined(User to, User from, UserAction action)
        {
            return Clients.Client(to.ConnectionId).CallDeclined(new ActionMessage { User = from, Action = action }.ToJson());
        }

        private User GetCaller()
        {
            return Users.SingleOrDefault(x => x.ConnectionId == Context.ConnectionId);
        }

        private User GetCallee(string connectionId)
        {
            return Users.SingleOrDefault(x => x.ConnectionId == connectionId);
        }

        private UserCall GetUserCall(string connectionId)
        {
            return UserCalls.SingleOrDefault(x => x.Users.SingleOrDefault(u => u.ConnectionId == connectionId) != null);
        }
    }
}
