namespace VideoChat.Shared.Models
{
    public class CallOffer
    {
        public User Caller { get; set; }
        public User Callee { get; set; }
    }
}
