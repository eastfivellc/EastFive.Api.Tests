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
using System.Web.Http.Routing;
using BlackBarLabs.Api.Extensions;
using BlackBarLabs.Linq;
using EastFive.Api.Services;

namespace EastFive.Api.Tests
{
    public class ProvideLoginMock : IIdentityService
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

        public Uri GetLoginUrl(string redirect_uri, byte mode, byte[] state, Uri responseControllerLocation)
        {
            return (new Uri("http://example.com/login"))
                .AddQuery("redirect", redirect_uri)
                .AddQuery("mode", ((int)mode).ToString())
                .AddQuery("data", Convert.ToBase64String(state))
                .AddQuery("state", GetState(redirect_uri, mode, state));
        }

        public Uri GetSignupUrl(string redirect_uri, byte mode, byte[] state, Uri responseControllerLocation)
        {
            return (new Uri("http://example.com/signup"))
                .AddQuery("redirect", redirect_uri)
                .AddQuery("mode", ((int)mode).ToString())
                .AddQuery("data", Convert.ToBase64String(state))
                .AddQuery("state", GetState(redirect_uri, mode, state));
        }

        public Uri GetLogoutUrl(string redirect_uri, byte mode, byte[] state, Uri responseControllerLocation)
        {
            return (new Uri("http://example.com/logout"))
                .AddQuery("redirect", redirect_uri)
                .AddQuery("mode", ((int)mode).ToString())
                .AddQuery("data", Convert.ToBase64String(state))
                .AddQuery("state", GetState(redirect_uri, mode, state));
        }

        private string GetState(string redirect_uri, byte mode, byte[] state)
        {
            var redirBytes = System.Text.Encoding.ASCII.GetBytes(redirect_uri);
            var stateBytes = (new byte[][]
            {
                BitConverter.GetBytes(((short)redirBytes.Length)),
                redirBytes,
                new byte [] {mode},
                state,
            }).SelectMany().ToArray();
            var base64 = Convert.ToBase64String(stateBytes);
            return base64;
        }

        public string GetToken(string userId, string token)
        {
            var loginId = lookup[userId];
            var otherToken = Guid.NewGuid().ToString();
            tokens.Add(otherToken, loginId);
            return otherToken;
        }

        public string CreateUser(string userId, Guid loginId)
        {
            lookup.Add(userId, loginId);
            var otherToken = Guid.NewGuid().ToString();
            tokens.Add(otherToken, loginId);
            return otherToken;
        }

        public TResult ParseState<TResult>(string state,
            Func<Uri, byte, byte[], TResult> onSuccess,
            Func<string, TResult> invalidState)
        {
            var bytes = Convert.FromBase64String(state);
            var urlLength = BitConverter.ToInt16(bytes, 0);
            if (bytes.Length < urlLength + 3)
                return invalidState("Encoded redirect length is invalid");
            var addr = System.Text.Encoding.ASCII.GetString(bytes, 2, urlLength);
            Uri url;
            if (!Uri.TryCreate(addr, UriKind.RelativeOrAbsolute, out url))
                return invalidState($"Invalid value for redirect url:[{addr}]");
            var mode = bytes.Skip(urlLength + 2).First();
            var data = bytes.Skip(urlLength + 3).ToArray();
            return onSuccess(url, mode, data);
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

        public Task DeleteLoginAsync(Guid loginId)
        {
            throw new NotImplementedException();
        }
    }

}
