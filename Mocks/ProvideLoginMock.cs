using BlackBarLabs.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackBarLabs.Linq;
using EastFive.Security.SessionServer;
using System.Net.Http;
using System.Security.Claims;
using EastFive.Api.Azure.Credentials;

namespace EastFive.Api.Tests
{
    public class ProvideLoginMock : IdentityServerConfiguration<Security.SessionServer.Tests.Controllers.ActorController>,
        IProvideLogin, IConfigureIdentityServer, IProvideLoginManagement, IProvideToken
    {
        private Dictionary<string, string> credentials = new Dictionary<string, string>();
        private static Dictionary<string, string> tokens = new Dictionary<string, string>();
        public static CredentialValidationMethodTypes method;
        
        public const string extraParamToken = "token";
        public const string extraParamState = "state";

        [Azure.Credentials.Attributes.IntegrationName("Mock")]
        public static Task<TResult> InitializeAsync<TResult>(
            Func<IProvideAuthorization, TResult> onProvideAuthorization,
            Func<TResult> onProvideNothing,
            Func<string, TResult> onFailure)
        {
            return onProvideAuthorization(new ProvideLoginMock()).ToTask();
        }

        public CredentialValidationMethodTypes Method => method;

        public Type CallbackController => typeof(ProvideLoginMock);
        
        public Task<TResult> RedeemTokenAsync<TResult>(
            IDictionary<string, string> tokensFromResponse,
            Func<string, Guid?, Guid?, IDictionary<string, string>, TResult> onSuccess,
            Func<Guid?, IDictionary<string, string>, TResult> onNotAuthenticated,
            Func<string, TResult> onInvalidToken,
            Func<string, TResult> onCouldNotConnect,
            Func<string, TResult> onUnspecifiedConfiguration,
            Func<string, TResult> onFailure)
        {
            var idToken = tokensFromResponse[ProvideLoginMock.extraParamToken];
            if (!tokens.ContainsKey(idToken))
                return onInvalidToken("Token not found").ToTask();
            var userId = tokens[idToken];

            var stateId = default(Guid?);
            if (tokensFromResponse.ContainsKey(ProvideLoginMock.extraParamState))
            {
                var stateIdString = tokensFromResponse[ProvideLoginMock.extraParamState];
                stateId = Guid.Parse(stateIdString);
            }

            return onSuccess(userId, stateId, default(Guid?), tokensFromResponse).ToTask();
        }

        public TResult ParseCredentailParameters<TResult>(IDictionary<string, string> tokensFromResponse, Func<string, Guid?, Guid?, TResult> onSuccess, Func<string, TResult> onFailure)
        {
            var idToken = tokensFromResponse[ProvideLoginMock.extraParamToken];
            if (!tokens.ContainsKey(idToken))
                return onFailure("Token not found");
            var userId = tokens[idToken];

            var stateId = default(Guid?);
            if (tokensFromResponse.ContainsKey(ProvideLoginMock.extraParamState))
            {
                var stateIdString = tokensFromResponse[ProvideLoginMock.extraParamState];
                stateId = Guid.Parse(stateIdString);
            }

            return onSuccess(userId, stateId, default(Guid?));
        }


        public static string GetToken(string userId)
        {
            var token = Guid.NewGuid().ToString();
            tokens.Add(token, userId);
            return token;
        }

        public IDictionary<string, string> CreateTokens(Guid actorId)
        {
            var token = GetToken(actorId.ToString("N"));
            return new Dictionary<string, string>()
            {
                {  ProvideLoginMock.extraParamToken, token }
            };
        }

        public Uri GetLoginUrl(Guid state, Uri responseControllerLocation, Func<Type, Uri> controllerToLocation)
        {
            return new Uri("http://provideloginmock.example.com/login")
                .AddQuery(ProvideLoginMock.extraParamState, state.ToString())
                .AddQuery("redirect", responseControllerLocation.AbsoluteUri);
        }

        public Uri GetLogoutUrl(Guid state, Uri responseControllerLocation, Func<Type, Uri> controllerToLocation)
        {
            return new Uri("http://provideloginmock.example.com/logout")
                .AddQuery(ProvideLoginMock.extraParamState, state.ToString())
                .AddQuery("redirect", responseControllerLocation.AbsoluteUri);
        }

        public Uri GetSignupUrl(Guid state, Uri responseControllerLocation, Func<Type, Uri> controllerToLocation)
        {
            return new Uri("http://provideloginmock.example.com/signup")
                .AddQuery(ProvideLoginMock.extraParamState, state.ToString())
                .AddQuery("redirect", responseControllerLocation.AbsoluteUri);
        }

        public Task<TResult> CreateAuthorizationAsync<TResult>(string displayName, string userId, bool isEmail, string secret, bool forceChange, 
            Func<Guid, TResult> onSuccess,
            Func<Guid, TResult> usernameAlreadyInUse,
            Func<TResult> onPasswordInsufficent, 
            Func<string, TResult> onServiceNotAvailable,
            Func<TResult> onServiceNotSupported,
            Func<string, TResult> onFailure)
        {
            return onSuccess(Guid.NewGuid()).ToTask();
        }

        public Task<TResult> GetAuthorizationAsync<TResult>(Guid loginId, Func<LoginInfo, TResult> onSuccess, Func<TResult> onNotFound, Func<string, TResult> onServiceNotAvailable, Func<TResult> onServiceNotSupported, Func<string, TResult> onFailure)
        {
            return onSuccess(
                new LoginInfo
                {
                    loginId = loginId,
                }).ToTask();
        }

        public Task<TResult> GetAllAuthorizationsAsync<TResult>(
            Func<LoginInfo[], TResult> onFound, Func<string, TResult> onServiceNotAvailable, Func<TResult> onServiceNotSupported, Func<string, TResult> onFailure)
        {
            return onFound(new LoginInfo[] { }).ToTask();
        }

        public Task<TResult> UpdateAuthorizationAsync<TResult>(Guid loginId, string password, bool forceChange, Func<TResult> onSuccess, Func<string, TResult> onServiceNotAvailable, Func<TResult> onServiceNotSupported, Func<string, TResult> onFailure)
        {
            throw new NotImplementedException();
        }

        public Task<TResult> UpdateEmailAsync<TResult>(Guid loginId, string email, Func<TResult> onSuccess, Func<string, TResult> onServiceNotAvailable, Func<TResult> onServiceNotSupported, Func<string, TResult> onFailure)
        {
            throw new NotImplementedException();
        }

        public Task<TResult> DeleteAuthorizationAsync<TResult>(Guid loginId, Func<TResult> onSuccess, Func<string, TResult> onServiceNotAvailable, Func<TResult> onServiceNotSupported, Func<string, TResult> onFailure)
        {
            throw new NotImplementedException();
        }

        public Task<TResult> CreateSessionAsync<TResult>(IDictionary<string, string> parameters, Func<HttpClient, IDictionary<string, string>, TResult> onCreatedSession, Func<string, TResult> onFailedToCreateSession)
        {
            return onCreatedSession(new HttpClient(), parameters).ToTask();
        }
        
        public Task<TResult> UserParametersAsync<TResult>(Guid actorId, System.Security.Claims.Claim[] claims, IDictionary<string, string> extraParams, Func<IDictionary<string, string>, IDictionary<string, Type>, IDictionary<string, string>, TResult> onSuccess)
        {
            return onSuccess(
                new Dictionary<string, string>() { { "push_pmp_file_to_ehr", "Push PMP file to EHR" } },
                new Dictionary<string, Type>() { { "push_pmp_file_to_ehr", typeof(bool) } },
                new Dictionary<string, string>() { { "push_pmp_file_to_ehr", "When true, the system will push PMP files into the provider's clinical documents in their EHR system." } }).ToTask();
        }
    }
}
