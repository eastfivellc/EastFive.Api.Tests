using BlackBarLabs.Extensions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Web.Http.Routing;
using BlackBarLabs.Api.Extensions;
using BlackBarLabs.Linq;
using EastFive.Api.Services;
using EastFive.Security.SessionServer;
using BlackBarLabs.Api.Resources;

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
        
        public static string GetToken(string userId)
        {
            var token = Guid.NewGuid().ToString();
            tokens.Add(token, userId);
            return token;
        }
        

        public Uri GetLoginUrl(Guid state, Uri responseControllerLocation)
        {
            return new Uri("http://provideloginmock.example.com/login")
                .AddQuery(ProvideLoginMock.extraParamState, state.ToString())
                .AddQuery("redirect", responseControllerLocation.AbsoluteUri);
        }

        public Uri GetLogoutUrl(Guid state, Uri responseControllerLocation)
        {
            return new Uri("http://provideloginmock.example.com/logout")
                .AddQuery(ProvideLoginMock.extraParamState, state.ToString())
                .AddQuery("redirect", responseControllerLocation.AbsoluteUri);
        }

        public Uri GetSignupUrl(Guid state, Uri responseControllerLocation)
        {
            return new Uri("http://provideloginmock.example.com/signup")
                .AddQuery(ProvideLoginMock.extraParamState, state.ToString())
                .AddQuery("redirect", responseControllerLocation.AbsoluteUri);
        }
    }

}
