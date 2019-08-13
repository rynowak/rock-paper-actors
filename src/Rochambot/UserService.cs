using System;

namespace Rochambot
{
    public class UserService
    {
        public event EventHandler<UserInfo> UserChanged;

        private UserInfo currentUser;

        public UserInfo CurrentUser
        {
            get => currentUser;
            set
            {
                currentUser = value;
                UserChanged?.Invoke(this, value);
            }
        }
    }
}