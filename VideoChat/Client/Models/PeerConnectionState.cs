namespace VideoChat.Client.Models
{
    public enum PeerConnectionState
    {
        New,
        Connecting,
        Connected,
        Disconnected,
        Failed,
        Closed
    }
}
