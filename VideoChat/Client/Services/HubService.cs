using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoChat.Shared.Extensions;
using VideoChat.Shared.Models;

namespace VideoChat.Client.Services
{
    public class HubService
    {
        private readonly HubConnection _hubConnection;
        private readonly NavigationManager _navigationManager;

        public event Action<List<User>> OnOnlineUsersUpdated;
        public event Action<string> OnIncomingCall;
        public event Action<string> OnCallAccepted;
        public event Action<UserActionMessage> OnCallDeclined;
        public event Action<UserActionMessage> OnCallEnded;
        public event Action<string, string> OnSignalReceived;

        public event Action<ServerActionMessage> OnCallAborted;
        public event Action<List<User>> OnUsersUpdated;
        public event Action<List<UserCall>> OnCallsUpdated;

        public HubService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri("/videochathub"))
                .WithAutomaticReconnect()
                .Build();

            InitConnectionEvents();
            InitHubEvents();

            _hubConnection.StartAsync();
        }

        private void InitConnectionEvents()
        {
            _hubConnection.Reconnecting += error =>
            {
                Console.WriteLine($"Hub: Reconnecting... {error?.Message}");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                Console.WriteLine($"Hub: Reconnected, id: {connectionId}");
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                Console.WriteLine($"Hub: Connection closed. {error?.Message}");
                return Task.CompletedTask;
            };
        }

        private void InitHubEvents()
        {
            _hubConnection.On<string>("UpdateOnlineUsers", (message) =>
            {
                OnOnlineUsersUpdated?.Invoke(message.FromJson<List<User>>());
            });

            _hubConnection.On<string>("IncomingCall", (connectionId) =>
            {
                OnIncomingCall?.Invoke(connectionId);
            });

            _hubConnection.On<string>("CallAccepted", (connectionId) =>
            {
                OnCallAccepted?.Invoke(connectionId);
            });

            _hubConnection.On<string>("CallDeclined", (message) =>
            {
                OnCallDeclined?.Invoke(message.FromJson<UserActionMessage>());
            });

            _hubConnection.On<string>("CallEnded", (message) =>
            {
                OnCallEnded?.Invoke(message.FromJson<UserActionMessage>());
            });

            _hubConnection.On<string, string>("ReceiveSignal", (connectionId, data) =>
            {
                OnSignalReceived?.Invoke(connectionId, data);
            });


            _hubConnection.On<string>("UpdateUsers", (message) =>
            {
                OnUsersUpdated?.Invoke(message.FromJson<List<User>>());
            });

            _hubConnection.On<string>("UpdateCalls", (message) =>
            {
                OnCallsUpdated?.Invoke(message.FromJson<List<UserCall>>());
            });

            _hubConnection.On<string>("CallAborted", (message) =>
            {
                OnCallAborted?.Invoke(message.FromJson<ServerActionMessage>());
            });
        }

        public Task<User> Login(string userName, string password)
        {
            return _hubConnection.InvokeAsync<User>("Login", userName, password);
        }

        public Task<List<User>> GetUsers()
        {
            return _hubConnection.InvokeAsync<List<User>>("GetUsers");
        }

        public Task<List<User>> GetOnlineUsers()
        {
            return _hubConnection.InvokeAsync<List<User>>("GetOnlineUsers");
        }

        public Task<List<UserCall>> GetCalls()
        {
            return _hubConnection.InvokeAsync<List<UserCall>>("GetCalls");
        }

        public Task CallUser(string connectionId)
        {
            return _hubConnection.SendAsync("CallUser", connectionId);
        }

        public Task AnswerCall(bool accept, string connectionId)
        {
            return _hubConnection.SendAsync("AnswerCall", accept, connectionId);
        }

        public Task HangUp()
        {
            return _hubConnection.SendAsync("HangUp");
        }

        public Task Leave()
        {
            return _hubConnection.SendAsync("Leave");
        }

        public Task SendSignal(string signal, string connectionId)
        {
            return _hubConnection.SendAsync("SendSignal", signal, connectionId);
        }

        public Task AbortCall(string callId)
        {
            return _hubConnection.SendAsync("AbortCall", callId);
        }

        public Task AbortAllCalls()
        {
            return _hubConnection.SendAsync("AbortAllCalls");
        }

        public Task<bool> UpdateUser(int id, int balance, bool canChat)
        {
            return _hubConnection.InvokeAsync<bool>("UpdateUser", id, balance, canChat);
        }

        public Task Dispose()
        {
            return _hubConnection.DisposeAsync();
        }

        public string ConnectionId => _hubConnection?.ConnectionId;
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public string State => _hubConnection?.State.ToString() ?? HubConnectionState.Disconnected.ToString();
    }
}
