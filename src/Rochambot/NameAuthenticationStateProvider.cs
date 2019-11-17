using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Rochambot
{
    // A custom AuthenticationStateProvider that lets the user just type in their name :)
    internal class NameAuthenticationStateProvider : AuthenticationStateProvider
    {
        private Task<AuthenticationState> _task;

        public NameAuthenticationStateProvider()
        {
            _task = Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
        }

        public void Login(string username)
        {
            if (username == null)
            {
                _task = Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
            }
            else
            {
                _task = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, username),
                }, "username"))));
            }

            NotifyAuthenticationStateChanged(_task);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return _task;
        }
    }
}