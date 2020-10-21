﻿using JsonFlatFileDataStore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoChat.Shared.Extensions;
using VideoChat.Shared.Models;

namespace VideoChat.Server.Hubs
{
    public class VideoChatHub : Hub<IVideoChatClient>
    {
        private static readonly IDocumentCollection<User> Users = new DataStore("data.json").GetCollection<User>();

        private static readonly List<UserCall> UserCalls = new List<UserCall>();
        private static readonly List<CallOffer> CallOffers = new List<CallOffer>();
        
        public async Task<User> Login(string userName, string password)
        {
            var user = Users.AsQueryable().FirstOrDefault(x => string.Equals(x.Name, userName, StringComparison.InvariantCultureIgnoreCase) && x.Password == password);

            if (user != null && !user.IsAdmin)
            {
                user.ConnectionId = Context.ConnectionId;
                await Users.UpdateOneAsync(user.Id, user);
                await SendOnlineUsers();
            }

            return user;
        }

        public List<User> GetOnlineUsers()
        {
            return Users.AsQueryable().Where(x => !x.IsAdmin && x.IsOnline).ToList();
        }

        public void CallUser(string connectionId)
        {
            var caller = GetUser();
            var callee = GetUser(connectionId);

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
            var caller = GetUser(connectionId);
            var callee = GetUser();
            
            if (callee == null)
            {
                return;
            }

            if (caller == null)
            {
                SendCallEnded(callee, caller, UserAction.Leave);
                return;
            }

            if (accept == false)
            {
                SendCallDeclined(caller, callee, UserAction.Decline);
                return;
            }

            var offerCount = CallOffers.RemoveAll(x => x.Callee == callee && x.Caller == caller);
            if (offerCount < 1)
            {
                SendCallEnded(callee, caller, UserAction.HangUp);
                return;
            }

            if (GetUserCall(caller.ConnectionId) != null)
            {
                SendCallDeclined(callee, caller, UserAction.Busy);
                return;
            }

            CallOffers.RemoveAll(c => c.Caller == caller);

            UserCalls.Add(new UserCall
            {
                Users = new List<User> { callee, caller }
            });

            Clients.Client(connectionId).CallAccepted(callee.ConnectionId);

            SendOnlineUsers();
        }

        public void HangUp()
        {
            var user = GetUser();

            if (user == null)
            {
                return;
            }

            var currentCall = GetUserCall(user.ConnectionId);

            if (currentCall != null)
            {
                foreach (var other in currentCall.Users.Where(x => x != user))
                {
                    SendCallEnded(other, user, UserAction.HangUp);
                }

                currentCall.Users.RemoveAll(x => x == user);

                if (currentCall.Users.Count < 2)
                {
                    UserCalls.Remove(currentCall);
                }
            }

            foreach (var offer in CallOffers.Where(x => x.Caller == user).ToArray())
            {
                SendCallEnded(offer.Callee, user, UserAction.Cancel);
                CallOffers.Remove(offer); 
            }

            SendOnlineUsers();
        }

        public async Task Leave()
        {
            var user = GetUser();

            if (user == null || user.IsAdmin)
            {
                return;
            }

            foreach (var call in UserCalls.Where(x => x.Users.Any(x => x == user)).ToArray())
            {
                var otherUser = call.Users.SingleOrDefault(x => x != user);
                _ = SendCallEnded(otherUser, user, UserAction.Leave);

                UserCalls.Remove(call);
            }

            foreach (var offer in CallOffers.Where(x => x.Caller == user || x.Callee == user).ToArray())
            {
                var otherUser = offer.Caller == user ? offer.Callee : offer.Caller;
                _ = SendCallEnded(otherUser, user, UserAction.Leave);

                CallOffers.Remove(offer);
            }

            user.ConnectionId = null;
            await Users.UpdateOneAsync(user.Id, user);
            await SendOnlineUsers();
        }

        public void SendSignal(string signal, string connectionId)
        {
            var caller = GetUser();
            var callee = GetUser(connectionId);

            if (caller == null || callee == null)
            {
                return;
            }

            var userCall = GetUserCall(caller.ConnectionId);

            if (userCall != null && userCall.Users.Exists(x => x == callee))
            {
                Clients.Client(callee.ConnectionId).ReceiveSignal(caller.ConnectionId, signal);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Leave();
            await base.OnDisconnectedAsync(exception);
        }


        private Task SendOnlineUsers()
        {
            return Clients.All.UpdateOnlineUsers(Users.AsQueryable().Where(x => !x.IsAdmin && x.IsOnline).ToJson());
        }

        private Task SendCallEnded(User to, User from, UserAction action)
        {
            return Clients.Client(to.ConnectionId).CallEnded(new ActionMessage { User = from, Action = action }.ToJson());
        }

        private Task SendCallDeclined(User to, User from, UserAction action)
        {
            return Clients.Client(to.ConnectionId).CallDeclined(new ActionMessage { User = from, Action = action }.ToJson());
        }

        private User GetUser(string connectionId = null)
        {
            connectionId ??= Context.ConnectionId;
            return Users.AsQueryable().SingleOrDefault(x => x.ConnectionId == connectionId);
        }

        private UserCall GetUserCall(string connectionId)
        {
            return UserCalls.SingleOrDefault(x => x.Users.SingleOrDefault(u => u.ConnectionId == connectionId) != null);
        }
    }
}
