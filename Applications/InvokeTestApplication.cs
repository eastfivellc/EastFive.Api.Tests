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

        protected override RequestMessage<TResource> BuildRequest<TResource>(
            IApplication application, HttpRequestMessage httpRequest)
        {
            httpRequest.Headers.Authorization = new
                AuthenticationHeaderValue(this.AuthorizationHeader);
            var request = base.BuildRequest<TResource>(application, httpRequest);
            return request;
        }

        public static async Task<InvokeTestApplication> InitUserAsync(
            Func<Task<Guid>> setupAccountAsync)
        {
            #region setup sessions

            // var sessionFactory = new RestApplicationFactory();
            var invocation = InvokeTestApplication.Init();
            invocation.AuthorizationHeader = await invocation.GetRequest<Session>()
                .PostAsync(
                        new Session
                        {
                            sessionId = Ref<Session>.NewRef(),
                        },
                    onCreatedBody: (sessionWithToken, contentType) => sessionWithToken.token);

            #endregion

            #region Get mock login method

            var accountId = Guid.Empty;

            (invocation.AzureApplication as AzureApplication)
                .AddOrUpdateInstantiation(typeof(ProvideLoginMock),
                    async (app) =>
                    {
                        await 1.AsTask();
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
                        return loginMock;
                    });

            var mockAuthenticationMethod = await invocation.GetRequest<Method>().GetAsync(
                onContents:
                    authentications =>
                    {
                        var matchingAuthentications = authentications
                            .Where(auth => auth.name == ProvideLoginMock.IntegrationName);
                        Assert.IsTrue(matchingAuthentications.Any());
                        return matchingAuthentications.First();
                    });

            #endregion

            #region perform direct link as fastest method for account construction

            var externalSystemUserId = Guid.NewGuid().ToString().Substring(0, 8);
            var responseResource = ProvideLoginMock.GetResponse(externalSystemUserId);
            var authorizationToAthenticateSession = await await invocation
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

            Assert.AreEqual(accountId, authorizationToAthenticateSession.account.Value);

            #endregion

            return invocation;
        }
    }
}
