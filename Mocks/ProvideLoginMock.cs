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
using EastFive.Serialization;
using EastFive.Api.Azure;
using EastFive.Api.Controllers;
using System.Web.Http.Routing;
using BlackBarLabs.Api;
using Newtonsoft.Json;
using EastFive.Azure.Auth;
using EastFive.Extensions;
using EastFive.Reflection;

namespace EastFive.Api.Tests
{
    [Azure.Credentials.Attributes.IntegrationName(IntegrationName)]
    public class ProvideLoginMock : IdentityServerConfiguration<Security.SessionServer.Tests.Controllers.ActorController>,
        IProvideLogin, IConfigureIdentityServer, IProvideLoginManagement, IProvideToken, IProvideSession
    {
        public const string IntegrationName = "Mock";
        public string Method => IntegrationName;
        public Guid Id => System.Text.Encoding.UTF8.GetBytes(Method).MD5HashGuid();

        private Dictionary<string, string> credentials = new Dictionary<string, string>();
        private static Dictionary<string, string> tokens = new Dictionary<string, string>();
        public static CredentialValidationMethodTypes method;
        
        public const string extraParamToken = "token";
        public const string extraParamState = "state";

        [Azure.Credentials.Attributes.IntegrationName(IntegrationName)]
        public static Task<TResult> InitializeAsync<TResult>(
            Func<IProvideAuthorization, TResult> onProvideAuthorization,
            Func<TResult> onProvideNothing,
            Func<string, TResult> onFailure)
        {
            return onProvideAuthorization(new ProvideLoginMock()).ToTask();
        }

        public static IDictionary<string, string> GetParameters(string externalSystemUserId)
        {
            return new Dictionary<string, string>()
            {
                { extraParamToken, externalSystemUserId }
            };
        }

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

        public TResult ParseCredentailParameters<TResult>(IDictionary<string, string> tokensFromResponse,
            Func<string, Guid?, Guid?, TResult> onSuccess, 
            Func<string, TResult> onFailure)
        {
            if (!tokensFromResponse.ContainsKey(ProvideLoginMock.extraParamToken))
                return onFailure("Invalid dictionary.");

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

        public static Redirection GetResponse(string userKey, Guid stateId)
        {
            var token = ProvideLoginMock.GetToken(userKey);
            return new Redirection()
            {
                state = stateId,
                token = token,
            };
        }

        public static Redirection GetResponse(string userKey)
        {
            var token = ProvideLoginMock.GetToken(userKey);
            return new Redirection()
            {
                token = token,
            };
        }

        public Task<bool> SupportsSessionAsync(EastFive.Azure.Auth.Session session)
        {
            return true.AsTask();
        }
    }

    public class ProvideLoginAccountMock : ProvideLoginMock, IProvideAccountInformation
    {
        public async Task<TResult> CreateAccount<TResult>(string subject,
                IDictionary<string, string> extraParameters,
                Method authentication, Authorization authorization,
                Uri baseUri,
                AzureApplication webApiApplication, 
            Func<Guid, TResult> onCreatedMapping,
            Func<TResult> onAllowSelfServeAccounts,
            Func<Uri, TResult> onInterceptProcess,
            Func<TResult> onNoChange)
        {
            var resultObjTask = await MapAccount(subject,
                extraParameters, authentication, authorization,
                baseUri,
                webApiApplication,
                (accountId) =>
                {
                    async Task<TResult> Func()
                    {
                        await OnNewAccount(accountId, subject);
                        return onCreatedMapping(accountId);
                    }
                    return Func();
                },
                () =>
                {
                    TResult result = onAllowSelfServeAccounts();
                    return result.AsTask();
                },
                uri =>
                {
                    TResult result = onInterceptProcess(uri);
                    return result.AsTask();
                },
                () =>
                {
                    TResult result = onNoChange();
                    return result.AsTask();
                });
            var resultCasted = await resultObjTask.CastTask<TResult>();
            return resultCasted;
        }

        public delegate Task<object> CreateAccountCallback(string subject,
                IDictionary<string, string> extraParameters,
                Method authentication, Authorization authorization,
                Uri baseUri,
                AzureApplication webApiApplication,
            Func<Guid, object> onCreatedMapping,
            Func<object> onAllowSelfServeAccounts,
            Func<Uri, object> onInterceptProcess,
            Func<object> onNoChange);

        public CreateAccountCallback MapAccount { get; set; }

        protected virtual async Task OnNewAccount(Guid accountId, string subject)
        {
            await 1.AsTask();
        }
    }

    [FunctionViewController4(
        Route = "MockRedirection",
        Resource = typeof(Redirection),
        ContentType = "x-application/auth-redirection.mock",
        ContentTypeVersion = "0.1")]
    public class Redirection : EastFive.Azure.Auth.Redirection
    {
        [ApiProperty(PropertyName = ProvideLoginMock.extraParamState)]
        [JsonProperty(PropertyName = ProvideLoginMock.extraParamState)]
        public Guid? state;

        [ApiProperty(PropertyName = ProvideLoginMock.extraParamToken)]
        [JsonProperty(PropertyName = ProvideLoginMock.extraParamToken)]
        public string token;

        [HttpGet(MatchAllParameters = false)]
        public static async Task<HttpResponseMessage> Get(
                [QueryParameter(Name = ProvideLoginMock.extraParamState)]IRefOptional<Authorization> authorizationRef,
                [QueryParameter(Name = ProvideLoginMock.extraParamToken)]string token,
                AzureApplication application, UrlHelper urlHelper,
                HttpRequestMessage request,
            RedirectResponse redirectResponse,
            ServiceUnavailableResponse onNoServiceResponse,
            BadRequestResponse onBadCredentials,
            GeneralConflictResponse onFailure)
        {
            var authentication = await EastFive.Azure.Auth.Method.ByMethodName(
                ProvideLoginMock.IntegrationName, application);
            var parameters = new Dictionary<string, string>()
                    {
                        { ProvideLoginMock.extraParamToken, token },
                    };
            if(authorizationRef.HasValue)
                parameters.Add(ProvideLoginMock.extraParamState, authorizationRef.id.ToString());

            return await Redirection.ProcessRequestAsync(authentication, 
                    parameters,
                    application,
                    request, urlHelper,
                (redirect) =>
                {
                    var response = redirectResponse(redirect);
                    return response;
                },
                (why) => onBadCredentials().AddReason($"Bad credentials:{why}"),
                (why) => onNoServiceResponse().AddReason(why),
                (why) => onFailure(why));
        }
    }
}
