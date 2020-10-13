using Blazored.LocalStorage;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace VideoChat.Client.Services
{
    public class BrowserService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILocalStorageService _localStorage;

        private readonly DotNetObjectReference<BrowserService> objRef;

        public event Action<bool> OnLocalMediaAttached;

        public BrowserService(IJSRuntime jsRuntime, ILocalStorageService localStorage)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));

            objRef = DotNetObjectReference.Create(this);
        }

        public ValueTask<string> GetUserName()
        {
            return _localStorage.GetItemAsync<string>("userName");
        }

        public ValueTask SetUserName(string userName)
        {
            return _localStorage.SetItemAsync("userName", userName);
        }

        public ValueTask AttachLocalMedia()
        {
            return _jsRuntime.InvokeVoidAsync("VideoChat.App.attachLocalMedia", "my-video", objRef);
        }

        [JSInvokable]
        public void AttachLocalMediaCallback(bool success)
        {
            OnLocalMediaAttached?.Invoke(success);
        }
    }
}
