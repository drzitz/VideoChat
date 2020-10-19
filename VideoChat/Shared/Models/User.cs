namespace VideoChat.Shared.Models
{
    public class User
    {
        public string Name;
        public string ConnectionId;
        public bool InCall;

        public static bool operator ==(User u1, User u2)
        {
            bool result;

            if (ReferenceEquals(u1, u2))
            {
                result = true;
            }
            else if ((u1 is null) || (u2 is null))
            {
                result = false;
            }
            else
            {
                result = u1.ConnectionId.CompareTo(u2.ConnectionId) == 0;
            }

            return result;
        }

        public static bool operator !=(User u1, User u2)
        {
            return !(u1 == u2);
        }

        public override bool Equals(object obj)
        {
            bool result = false;

            if (obj is User)
            {
                User u2 = obj as User;
                result = this == u2;
            }

            return result;
        }

        public override int GetHashCode()
        {
            return ConnectionId.GetHashCode();
        }
    }
}
