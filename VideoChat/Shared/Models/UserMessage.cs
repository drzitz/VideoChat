namespace VideoChat.Shared.Models
{
    public class UserMessage
    {
        public User User { get; set; }
    }

    public class ActionMessage : UserMessage
    {
        public UserAction Action { get; set; }
    }

    public class SignalMessage : UserMessage
    {
        public string Data { get; set; }
    }
}
