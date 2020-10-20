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

        public event Action<List<User>> OnUsersUpdated;
        public event Action<string> OnIncomingCall;
        public event Action<string> OnCallAccepted;
        public event Action<ActionMessage> OnCallDeclined;
        public event Action<ActionMessage> OnCallEnded;
        public event Action<string, string> OnSignalReceived;

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
            _hubConnection.On<string>("UpdateUsersList", (message) =>
            {
                OnUsersUpdated?.Invoke(message.FromJson<List<User>>());
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
                OnCallDeclined?.Invoke(message.FromJson<ActionMessage>());
            });

            _hubConnection.On<string>("CallEnded", (message) =>
            {
                OnCallEnded?.Invoke(message.FromJson<ActionMessage>());
            });

            _hubConnection.On<string, string>("ReceiveSignal", (connectionId, data) =>
            {
                OnSignalReceived?.Invoke(connectionId, data);
            });
        }

        public Task<bool> Login(string userName, string password)
        {
            return _hubConnection.InvokeAsync<bool>("Login", userName, password);
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

        public Task Dispose()
        {
            return _hubConnection.DisposeAsync();
        }

        public string ConnectionId => _hubConnection?.ConnectionId;
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public string State => _hubConnection?.State.ToString() ?? HubConnectionState.Disconnected.ToString();
    }
}
