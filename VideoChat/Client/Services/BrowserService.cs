using Blazored.LocalStorage;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using VideoChat.Client.Models;

namespace VideoChat.Client.Services
{
    public class BrowserService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILocalStorageService _localStorage;

        private readonly DotNetObjectReference<BrowserService> objRef;

        public event Action<bool> OnLocalMediaAttached;
        public event Action<string, string> OnSendSignal;

        public BrowserService(IJSRuntime jsRuntime, ILocalStorageService localStorage)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));

            objRef = DotNetObjectReference.Create(this);
        }

        public ValueTask<UserInfo> GetUser()
        {
            return _localStorage.GetItemAsync<UserInfo>("user");
        }

        public ValueTask SetUser(string userName, string password)
        {
            return _localStorage.SetItemAsync("user", new UserInfo { Name = userName, Password = password });
        }

        public ValueTask RemoveUser()
        {
            return _localStorage.RemoveItemAsync("user");
        }

        public ValueTask Init()
        {
            return _jsRuntime.InvokeVoidAsync("VideoChat.App.init", objRef);
        }

        public ValueTask InitiateOffer(string connectionId)
        {
            return _jsRuntime.InvokeVoidAsync("VideoChat.App.initiateOffer", connectionId);
        }

        public ValueTask ProcessSignal(string connectionId, string data)
        {
            return _jsRuntime.InvokeVoidAsync("VideoChat.App.processSignal", connectionId, data);
        }

        public ValueTask CloseConnection(string connectionId)
        {
            return _jsRuntime.InvokeVoidAsync("VideoChat.App.closeConnection", connectionId);
        }

        public ValueTask Reset()
        {
            return _jsRuntime.InvokeVoidAsync("VideoChat.App.reset");
        }

        [JSInvokable]
        public void AttachLocalMediaCallback(bool success)
        {
            OnLocalMediaAttached?.Invoke(success);
        }

        [JSInvokable]
        public void SendSignalCallback(string data, string connectionId)
        {
            OnSendSignal?.Invoke(data, connectionId);
        }
    }
}
