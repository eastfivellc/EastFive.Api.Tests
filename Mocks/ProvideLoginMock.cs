using BlackBarLabs.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackBarLabs.Api.Extensions;
using BlackBarLabs.Linq;
using EastFive.Security.SessionServer;

namespace EastFive.Api.Tests
{
    public class ProvideLoginMock : EastFive.Security.SessionServer.IdentityServerConfiguration<Security.SessionServer.Tests.Controllers.ActorController>,
        IProvideLogin, IConfigureIdentityServer
    {
        private Dictionary<string, string> credentials = new Dictionary<string, string>();
        private static Dictionary<string, string> tokens = new Dictionary<string, string>();
        
        public const string extraParamToken = "token";
        public const string extraParamState = "state";

        public static Task<TResult> InitializeAsync<TResult>(
            Func<IProvideAuthorization, TResult> onProvideAuthorization,
            Func<TResult> onProvideNothing,
            Func<string, TResult> onFailure)
        {
            return onProvideAuthorization(new ProvideLoginMock()).ToTask();
        }

        public CredentialValidationMethodTypes Method => CredentialValidationMethodTypes.Implicit;

        public Task<TResult> RedeemTokenAsync<TResult>(
            IDictionary<string, string> tokensFromResponse,
            Func<string, Guid?, Guid?, IDictionary<string, string>, TResult> onSuccess,
            Func<string, TResult> onInvalidCredentials,
            Func<string, TResult> onCouldNotConnect,
            Func<string, TResult> onUnspecifiedConfiguration,
            Func<string, TResult> onFailure)
        {
            var idToken = tokensFromResponse[ProvideLoginMock.extraParamToken];
            if (!tokens.ContainsKey(idToken))
                return onInvalidCredentials("Token not found").ToTask();
            var userId = tokens[idToken];

            var stateId = default(Guid?);
            if (tokensFromResponse.ContainsKey(ProvideLoginMock.extraParamState))
            {
                var stateIdString = tokensFromResponse[ProvideLoginMock.extraParamState];
                stateId = Guid.Parse(stateIdString);
            }

            return onSuccess(userId, stateId, default(Guid?), tokensFromResponse).ToTask();
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

        public Uri GetLogoutUrl(Guid state, Uri responseControllerLocation)
        {
            throw new NotImplementedException();
            //return (new Uri("http://example.com/logout"))
            //    .AddQuery("redirect", redirect_uri)
            //    .AddQuery("mode", ((int)mode).ToString())
            //    .AddQuery("data", Convert.ToBase64String(state))
            //    .AddQuery("state", GetState(redirect_uri, mode, state));
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
        
        public static string GetToken(string userId)
        {
            var token = Guid.NewGuid().ToString();
            tokens.Add(token, userId);
            return token;
        }

        public TResult ParseState<TResult>(string state, Func<Guid, TResult> onSuccess, Func<string, TResult> onInvalidState)
        {
            throw new NotImplementedException();
            //var bytes = Convert.FromBase64String(state);
            //var urlLength = BitConverter.ToInt16(bytes, 0);
            //if (bytes.Length < urlLength + 3)
            //    return onInvalidState("Encoded redirect length is invalid");
            //var addr = System.Text.Encoding.ASCII.GetString(bytes, 2, urlLength);
            //Uri url;
            //if (!Uri.TryCreate(addr, UriKind.RelativeOrAbsolute, out url))
            //    return onInvalidState($"Invalid value for redirect url:[{addr}]");
            //var mode = bytes.Skip(urlLength + 2).First();
            //var data = bytes.Skip(urlLength + 3).ToArray();
            //return onSuccess(url, mode, data);
        }

        public Task DeleteAuthorizationAsync(Guid loginId)
        {
            //throw new NotImplementedException();
            return 1.ToTask();
        }

        public TResult ParseState<TResult>(string state,
            Func<byte, byte[], IDictionary<string, string>, TResult> onSuccess, Func<string, TResult> invalidState)
        {
            return onSuccess(0, new byte[] { }, new Dictionary<string, string>());
        }

        public Uri GetLoginUrl(Guid state, Uri redirectUri)
        {
            return new Uri("http://provideloginmock.example.com")
                .AddQuery(ProvideLoginMock.extraParamState, state.ToString())
                .AddQuery("redirect", redirectUri.AbsoluteUri);
        }
    }
}
