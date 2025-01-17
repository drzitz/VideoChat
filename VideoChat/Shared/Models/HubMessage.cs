﻿namespace VideoChat.Shared.Models
{
    public class HubMessage
    {
        public User User { get; set; }
    }

    public class UserActionMessage : HubMessage
    {
        public UserAction Action { get; set; }
    }

    public class ServerActionMessage : HubMessage
    {
        public ServerAction Action { get; set; }
        public User ActionTarget { get; set; }
    }

    public class SignalMessage : HubMessage
    {
        public string Data { get; set; }
    }

    public class PollMessage: HubMessage
    {
        public Poll Poll { get; set; }
    }
}
