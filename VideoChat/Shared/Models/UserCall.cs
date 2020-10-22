using System;
using System.Collections.Generic;

namespace VideoChat.Shared.Models
{
    public class UserCall
    {
        public UserCall()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; set; }

        public List<User> Users { get; set; }

        public User Caller { get; set; }

        public User Callee { get; set; }

        public DateTime Started { get; set; }

        public DateTime Confirmed { get; set; }
    }
}
