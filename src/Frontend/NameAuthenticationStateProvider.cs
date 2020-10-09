using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend
{
    // A custom AuthenticationStateProvider that lets the user just type in their name :)
    internal class NameAuthenticationStateProvider : AuthenticationStateProvider
    {
        private Task<AuthenticationState> task;

        public NameAuthenticationStateProvider()
        {
            task = Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
        }

        public void Login(string username)
        {
            if (username == null)
            {
                task = Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
            }
            else
            {
                task = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, username),
                }, "username"))));
            }

            NotifyAuthenticationStateChanged(task);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return task;
        }
    }
}