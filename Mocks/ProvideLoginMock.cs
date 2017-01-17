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
            var claimType = Microsoft.Azure.CloudConfigurationManager.GetSetting(
                        "BlackBarLabs.Security.CredentialProvider.AzureADB2C.ClaimType");
            var claimValue = tokens[idToken].ToString();
            var claim = new Claim(claimType, claimValue);
            var identity = new ClaimsIdentity(
                new[] { claim });
            var claims = new ClaimsPrincipal(identity);
            return onSuccess(claims).ToTask();
        }
    }
    
        public static class Extensions
        {
            public static void AddUpdateClaim(this System.Security.Principal.IPrincipal currentPrincipal, string key, string value)
            {
                var identity = currentPrincipal.Identity as ClaimsIdentity;
                if (identity == null)
                    return;

                // check for existing claim and remove it
                var existingClaim = identity.FindFirst(key);
                if (existingClaim != null)
                    identity.RemoveClaim(existingClaim);

                // add new claim
                identity.AddClaim(new Claim(key, value));
            }

            public static string GetClaimValue(this System.Security.Principal.IPrincipal currentPrincipal, string key)
            {
                var identity = currentPrincipal.Identity as ClaimsIdentity;
                if (identity == null)
                    return null;

                var claim = identity.Claims.First(c => c.Type == key);
                return claim.Value;
            }
        }

}
