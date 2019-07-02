using EastFive.Api.Azure;
using EastFive.Api.Tests.Mocks;
using EastFive.Azure.Auth;
using EastFive.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

namespace EastFive.Api.Tests
{
    public class InvokeTestApplication : InvokeApplicationDirect
    {

        public Session? SessionMaybe { get; private set; }

        public Guid? AccountIdMaybe { get; private set; }

        public override string[] ApiRoutes
        {
            get
            {
                var routesApiCSV = EastFive.Web.Configuration.Settings.GetString(
                        EastFive.Api.Tests.AppSettings.RoutesApi,
                    (routesApiFound) => routesApiFound,
                    (why) => "DefaultApi");

                var routesApi = routesApiCSV.Split(new[] { ',' }).ToArray();
                return routesApi;
            }
        }

        public override string[] MvcRoutes
        {
            get
            {
                var routesMvcCSV = EastFive.Web.Configuration.Settings.GetString(
                        EastFive.Api.Tests.AppSettings.RoutesMvc,
                    (routesApiFound) => routesApiFound,
                    (why) => "Default");

                return routesMvcCSV.Split(new[] { ',' });
            }
        }

        protected override IApplication Application => AzureApplication;
        public IApplication AzureApplication;

        protected InvokeTestApplication(IApplication application, Uri serverUrl)
            : base(serverUrl)
        {
            this.AzureApplication = application;
        }

        public static InvokeTestApplication Init()
        {
            var application = new HttpApplication();
            return Init(application);
        }

        public static InvokeTestApplication Init(HttpApplication application)
        {
            var hostingLocation = Web.Configuration.Settings.GetUri(
                    AppSettings.ServerUrl,
                (hostingLocationFound) => hostingLocationFound,
                (whyUnspecifiedOrInvalid) => new Uri("http://example.com"));
            return new InvokeTestApplication(application, hostingLocation);
        }

        public static async Task<InvokeTestApplication> InitUserAsync(
            Func<Task<Guid>> setupAccountAsync)
        {
            var invocation = InvokeTestApplication.Init();
            await invocation.LoginAsync(setupAccountAsync);
            return invocation;
        }

        public async Task<Session> SessionAsync()
        {
            this.SessionMaybe = await this
                .GetRequest<Session>()
                .PostAsync(
                        new Session
                        {
                            sessionId = Ref<Session>.NewRef(),
                        },
                    onCreatedBody: (sessionWithToken, contentType) => sessionWithToken);

            this.Headers.Add(this.SessionMaybe.Value.HeaderName, this.SessionMaybe.Value.token);
            return this.SessionMaybe.Value;
        }

        public async Task<Session> LoginAsync(
            Func<Task<Guid>> setupAccountAsync)
        {
            #region Get mock login method

            var accountId = Guid.Empty;

            (this.AzureApplication as AzureApplication)
                .AddOrUpdateInstantiation(typeof(ProvideLoginMock),
                    (app) =>
                    {
                        var loginMock = new ProvideLoginAccountMock();
                        loginMock.MapAccount =
                            async (externalKey, extraParameters, authenticationInner, authorization,
                                    baseUri, webApiApplication,
                                onCreatedMapping,
                                onAllowSelfServeAccounts,
                                onInterceptProcess,
                                onNoChange) =>
                            {
                                accountId = await setupAccountAsync();
                                return onCreatedMapping(accountId);
                            };
                        return loginMock.AsTask<object>();
                    });

            var mockAuthenticationMethod = await this
                .GetRequest<Method>()
                .GetAsync(
                    onContents:
                        authentications =>
                        {
                            var matchingAuthentications = authentications
                                .Where(auth => auth.name == ProvideLoginMock.IntegrationName);
                            Assert.IsTrue(matchingAuthentications.Any());
                            return matchingAuthentications.First();
                        });

            #endregion

            #region Perform direct link as fastest method for account construction

            var externalSystemUserId = Guid.NewGuid().ToString().Substring(0, 8);
            var responseResource = ProvideLoginMock.GetResponse(externalSystemUserId);
            var invocation = this;
            this.SessionMaybe = await await this
                .GetRequest<EastFive.Api.Tests.Redirection>()
                .Where(externalSystemUserId)
                .GetAsync(
                    onRedirect:
                        async (urlRedirect) =>
                        {
                            var authIdStr = urlRedirect.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier);
                            var authId = Guid.Parse(authIdStr);
                            var authIdRef = authId.AsRef<EastFive.Azure.Auth.Authorization>();

                            // TODO: New comms here?
                            return await await invocation
                                .GetRequest<EastFive.Azure.Auth.Authorization>()
                                .Where(auth => auth.authorizationRef == authIdRef)
                                .GetAsync(
                                    onContent:
                                        (authenticatedAuthorization) =>
                                        {
                                            var session = new Session
                                            {
                                                sessionId = Guid.NewGuid().AsRef<Session>(),
                                                authorization = authenticatedAuthorization.authorizationRef.Optional(),
                                            };
                                            return invocation.GetRequest<Session>()
                                                .PostAsync(session,
                                                    onCreatedBody:
                                                        (updated, contentType) =>
                                                        {
                                                            return updated;
                                                        });
                                        });
                        });

            //Assert.AreEqual(accountId, authorizationToAthenticateSession.account.Value);

            #endregion

            this.Headers.Add(this.SessionMaybe.Value.HeaderName, this.SessionMaybe.Value.token);
            this.AccountIdMaybe = this.SessionMaybe.Value.account;

            return this.SessionMaybe.Value;
        }

        protected override RequestMessage<TResource> BuildRequest<TResource>(
            IApplication application, HttpRequestMessage httpRequest)
        {
            if (this.AuthorizationHeader.HasBlackSpace())
                httpRequest.Headers.Authorization = new
                    AuthenticationHeaderValue(this.AuthorizationHeader);
            var request = base.BuildRequest<TResource>(application, httpRequest);
            return request;
        }
    }
}
