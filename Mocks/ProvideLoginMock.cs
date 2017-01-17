using BlackBarLabs.Extensions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using EastFive.Security.LoginProvider;

namespace EastFive.Api.Tests
{
    public class ProvideLoginMock : IProvideLogin
    {
        private Dictionary<string, string> credentials = new Dictionary<string, string>();
        private Dictionary<string, Guid> lookup = new Dictionary<string, Guid>();
        private Dictionary<string, Guid> tokens = new Dictionary<string, Guid>();

        public Task<TResult> CreateLoginAsync<TResult>(string displayName,
            string userId, bool isEmail, string secret, bool forceChange, 
            Func<Guid, TResult> onSuccess, 
            Func<string, TResult> onFail)
        {
            var loginId = Guid.NewGuid();
            if (credentials.ContainsKey(userId) ||
                tokens.Any(kvp => kvp.Value == loginId))
                return onFail("Already exists").ToTask();
            credentials.Add(userId, secret);
            lookup.Add(userId, loginId);

            return onSuccess(loginId).ToTask();
        }
        
        public string GetToken(string userId, string token)
        {
            var loginId = lookup[userId];
            var otherToken = Guid.NewGuid().ToString();
            tokens.Add(otherToken, loginId);
            return otherToken;
        }

        public Task<TResult> ValidateToken<TResult>(string idToken,
            Func<ClaimsPrincipal, TResult> onSuccess,
            Func<string, TResult> onFailed)
        {
            if (!tokens.ContainsKey(idToken))
                return onFailed("Token not found").ToTask();
            return onSuccess(null).ToTask();
        }
    }
}
