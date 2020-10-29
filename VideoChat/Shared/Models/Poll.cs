using System;

namespace VideoChat.Shared.Models
{
    public class Poll
    {
        public Poll()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; }
        public User Poller { get; set; }
        public User Pollee { get; set; }
        public string StreamId { get; set; }
    }
}
