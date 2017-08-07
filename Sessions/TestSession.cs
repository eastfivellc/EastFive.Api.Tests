using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using BlackBarLabs.Web;
using EastFive.Security.LoginProvider;
using BlackBarLabs.Extensions;
using BlackBarLabs.Api.Tests.Mocks;
using EastFive.Api.Services;
using System.Configuration;
using BlackBarLabs.Collections.Generic;
using System.Net.Http.Headers;

namespace BlackBarLabs.Api.Tests
{
    public class TestSession : ITestSession
    {
        public static async Task StartAsync(Func<TestSession, Task> callback)
        {
            var session = new TestSession();
            var callbackTask = callback(session);
            await callbackTask;
        }

        public TestSession()
        {
            Id = Guid.NewGuid();
            Headers = new Dictionary<string, string>();
            Init();
        }

        public TestSession(string authorization)
        {
            Headers = new Dictionary<string, string>();
            Headers.Add("Authorization", authorization);
            Init();
        }

        public TestSession(Guid actorId)
        {
            Id = actorId;
            Headers = new Dictionary<string, string>();
            var token = BlackBarLabs.Api.Tests.TestSession.CreateToken(actorId);
            Headers.Add("Authorization", token);
            Init();
        }

        public TestSession(Guid actorId, IDictionary<string, string> claims)
        {
            Id = actorId;
            Headers = new Dictionary<string, string>();
            var token = BlackBarLabs.Api.Tests.TestSession.CreateToken(actorId, claims);
            Headers.Add("Authorization", token);
            Init();
        }

        private void Init()
        {
            this.UpdateRequestPropertyFetch(
                BlackBarLabs.Api.ServicePropertyDefinitions.IdentityService,
                ((IIdentityService)LoginService).ToTask());
            this.UpdateRequestPropertyFetch<ISendMessageService>(
                BlackBarLabs.Api.ServicePropertyDefinitions.MailService,
                MailService);
            this.UpdateRequestPropertyFetch<ITimeService>(
                BlackBarLabs.Api.ServicePropertyDefinitions.TimeService,
                TimeService);
        }

        public Guid Id { get; set; }

        private static EastFive.Api.Tests.ProvideLoginMock loginService =
            default(EastFive.Api.Tests.ProvideLoginMock);
        public EastFive.Api.Tests.ProvideLoginMock LoginService
        {
            get
            {
                if(default(EastFive.Api.Tests.ProvideLoginMock) == loginService)
                    loginService = new EastFive.Api.Tests.ProvideLoginMock();
                return loginService;
            }
            set
            {
                loginService = value;
            }
        }

        private static MockMailService mailService =
            default(MockMailService);
        public MockMailService MailService
        {
            get
            {
                if (default(MockMailService) == mailService)
                    mailService = new MockMailService();
                return mailService;
            }
        }

        private static MockTimeService timeService =
            default(MockTimeService);
        public MockTimeService TimeService
        {
            get
            {
                if (default(MockTimeService) == timeService)
                    timeService = new MockTimeService();
                return timeService;
            }
        }

        #region Methods

        public async Task CreateSessionAsync(string credentialToken)
        {
            var response = this.PostAsync<EastFive.Security.SessionServer.Api.Controllers.SessionController>(
                new EastFive.Security.SessionServer.Api.Resources.Session
                {
                    Id = this.Id,
                    CredentialToken = new EastFive.Security.SessionServer.Api.Resources.CredentialToken
                    {
                        Method = EastFive.Security.SessionServer.CredentialValidationMethodTypes.Password,
                        Token = credentialToken,
                    },
                });
            var sessionFromAuth = await response.GetContentAsync<EastFive.Security.SessionServer.Api.Resources.Session>(
                System.Net.HttpStatusCode.Created);
            Assert.AreEqual(this.Id, sessionFromAuth.AuthorizationId);
            this.Headers.Add(sessionFromAuth.SessionHeader.Name, sessionFromAuth.SessionHeader.Value);
        }

        public async Task<HttpResponseMessage> PostAsync<TController>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController
        {
            var controller = GetController<TController>();
            return await InvokeControllerAsync(controller, HttpMethod.Post,
                (request, user) =>
                {
                    return resource;
                });
        }

        public async Task<HttpResponseMessage> PostMultipartAsync<TController>(Action<MultipartContent> multipartContentCallback)
            where TController : ApiController
        {
            var controller = GetController<TController>();
            var response = await InvokeControllerAsync(controller, HttpMethod.Post,
                (httpRequest, user) =>
                {
                    var multipartContent = new MultipartContent();
                    multipartContentCallback(multipartContent);
                    httpRequest.Content = multipartContent;
                });
            return response;
        }

        public async Task<HttpResponseMessage> PutAsync<TController>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController
        {
            var controller = GetController<TController>();
            return await InvokeControllerAsync(controller, HttpMethod.Put,
                (request, user) =>
                {
                    return resource;
                });
        }

        public async Task<HttpResponseMessage> PutMultipartAsync<TController>(Action<MultipartContent> multipartContentCallback)
            where TController : ApiController
        {
            var controller = GetController<TController>();
            var response = await InvokeControllerAsync(controller, HttpMethod.Put,
                (httpRequest, user) =>
                {
                    var multipartContent = new MultipartContent();
                    multipartContentCallback(multipartContent);
                    httpRequest.Content = multipartContent;
                });
            return response;
        }

        public static string CreateToken(Guid actorId)
        {
            var claims = new Dictionary<string, string>();
            var actorIdClaimType = ConfigurationManager.AppSettings[EastFive.Api.Configuration.SecurityDefinitions.ActorIdClaimType];
            claims.AddOrReplace(actorIdClaimType, actorId.ToString());

            var token = BlackBarLabs.Security.Tokens.JwtTools.CreateToken(Guid.NewGuid(), actorId,
                new Uri("http://test.example.com"), TimeSpan.FromHours(1.0), claims,
                (tokenNew) => tokenNew,
                (missingConfig) => { Assert.Fail(missingConfig); return string.Empty; },
                (configName, issue) => { Assert.Fail($"{configName} -- {issue}"); return string.Empty; });
            return token;
        }

        public static string CreateToken(Guid actorId, IDictionary<string, string> claims)
        {
            var actorIdClaimType = ConfigurationManager.AppSettings[EastFive.Api.Configuration.SecurityDefinitions.ActorIdClaimType];
            claims.AddOrReplace(actorIdClaimType, actorId.ToString());
            var token = BlackBarLabs.Security.Tokens.JwtTools.CreateToken(Guid.NewGuid(), actorId,
                new Uri("http://test.example.com"), TimeSpan.FromHours(1.0), claims,
                (tokenNew) => tokenNew,
                (missingConfig) => { Assert.Fail(missingConfig); return string.Empty; },
                (configName, issue) => { Assert.Fail($"{configName} -- {issue}"); return string.Empty; });
            return token;
        }

        public async Task<TResult> GetAsync<TController, TResult>(
                Func<HttpResponseMessage, TResult> callback)
            where TController : ApiController
        {
            var controller = GetController<TController>();
            var response = await InvokeControllerAsync(controller, HttpMethod.Get);
            return callback(response);
        }

        public async Task<HttpResponseMessage> GetAsync<TController>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController
        {
            var controller = GetController<TController>();
            return await InvokeControllerAsync(controller, HttpMethod.Get,
                (request, user) =>
                {
                    return resource;
                });
        }

        public async Task<TResult> GetAsync<TController, TResult>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController
        {
            var response = await this.GetAsync<TController>(resource, mutateRequest);
            response.Assert(System.Net.HttpStatusCode.OK);
            var results = response.GetContent<TResult>();
            return results;
        }

        public async Task<TResult> GetAsync<TController, TResult>(object resource,
                HttpActionDelegate<object, TResult> callback)
            where TController : ApiController
        {
            var response = await this.GetAsync<TController>(resource);
            var results = callback(response, resource);
            return results;
        }
        
        public async Task<HttpResponseMessage> DeleteAsync<TController>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController
        {
            var controller = GetController<TController>();
            return await InvokeControllerAsync(controller, HttpMethod.Delete,
                (request, user) =>
                {
                    return resource;
                });
        }

        public async Task<TResult> OptionsAsync<TController, TResult>(
                Func<HttpResponseMessage, HttpMethod[], TResult> callback)
            where TController : ApiController
        {
            var controller = GetController<TController>();
            var response = await InvokeControllerAsync(controller, HttpMethod.Options,
                (request, user) =>
                {
                    return null;
                });
            var options = response.GetOptions();
            var results = callback(response, options.ToArray());
            return results;
        }

        public async Task<TResult> OptionsAsync<TController, TResult>(object resource,
                Func<HttpResponseMessage, HttpMethod[], TResult> callback)
            where TController : ApiController
        {
            var controller = GetController<TController>();
            var response = await InvokeControllerAsync(controller, HttpMethod.Options,
                (request, user) =>
                {
                    return resource;
                });
            var options = response.GetOptions();
            var results = callback(response, options.ToArray());
            return results;
        }

        #endregion
        
        private Dictionary<string, object> requestPropertyObjects = new Dictionary<string, object>();
        private Dictionary<string, object> requestPropertyFetches = new Dictionary<string, object>();
        
        public void UpdateRequestPropertyFetch<T>(string propertyKey, T propertyValue, out T currentValue)
        {
            if (requestPropertyObjects.ContainsKey(propertyKey))
            {
                currentValue = (T)requestPropertyObjects[propertyKey];
                requestPropertyObjects[propertyKey] = propertyValue;
                return;
            }
            currentValue = default(T);
            requestPropertyObjects.Add(propertyKey, propertyValue);

            Func<T> fetchPropertyValue = () => (T)requestPropertyObjects[propertyKey];
            requestPropertyFetches.Add(propertyKey, fetchPropertyValue);
        }
        public void UpdateRequestPropertyFetch<T>(string propertyKey, T propertyValue)
        {
            T discard;
            UpdateRequestPropertyFetch(propertyKey, propertyValue, out discard);
        }

        public T GetRequestPropertyFetch<T>(string propertyKey)
        {
            if (requestPropertyObjects.ContainsKey(propertyKey))
            {
                var currentValue = (T)requestPropertyObjects[propertyKey];
                return currentValue;
            }
            return default(T);
        }
        
        private HttpRequestMessage GetRequest<TController>(TController controller, HttpMethod method)
            where TController : ApiController
        {
            var hostingLocation = Microsoft.Azure.CloudConfigurationManager.GetSetting("BlackBarLabs.Api.Tests.ServerUrl");
            if (String.IsNullOrWhiteSpace(hostingLocation))
                hostingLocation = "http://example.com";
            var httpRequest = new HttpRequestMessage(method, hostingLocation);
            var config = new HttpConfiguration();

            var routesApi = Microsoft.Azure.CloudConfigurationManager.GetSetting("BlackBarLabs.Api.Tests.RoutesApi");
            if (String.IsNullOrWhiteSpace(routesApi))
                routesApi = "DefaultApi";
            var routesMvc = Microsoft.Azure.CloudConfigurationManager.GetSetting("BlackBarLabs.Api.Tests.RoutesMvc");
            if (String.IsNullOrWhiteSpace(routesMvc))
                routesMvc = "Default";

            routesApi.Split(new[] { ',' }).Select(
                routeName =>
                {
                    var route = config.Routes.MapHttpRoute(
                        name: routeName,
                        routeTemplate: routeName + "/{controller}/{id}",
                        defaults: new { id = RouteParameter.Optional }
                    );
                    httpRequest.SetRouteData(new System.Web.Http.Routing.HttpRouteData(route));
                    return route;
                }).ToArray();
            routesMvc.Split(new[] { ',' }).Select(
                routeName =>
                {
                    var route = config.Routes.MapHttpRoute(
                        name: "Default",
                        routeTemplate: "{controller}/{id}",
                        defaults: new { id = RouteParameter.Optional }
                        );
                    httpRequest.SetRouteData(new System.Web.Http.Routing.HttpRouteData(route));
                    return route;
                }).ToArray();
            
            httpRequest.SetConfiguration(config);

            foreach(var requestPropertyKvp in requestPropertyFetches)
            {
                httpRequest.Properties.Add(
                    requestPropertyKvp.Key, requestPropertyKvp.Value);
            }

            controller.Request = httpRequest;
            foreach (var headerKVP in Headers)
            {
                httpRequest.Headers.Add(headerKVP.Key, headerKVP.Value);
            }
            
            return httpRequest;
        }

        public Dictionary<string, string> Headers { get; set; }

        private TController GetController<TController>()
            where TController : ApiController
        {
            var controller = Activator.CreateInstance<TController>();
            return controller;
        }

        private delegate T InvokeControllerDelegate<T>(HttpRequestMessage request, MockPrincipal user);

        private async Task<HttpResponseMessage> InvokeControllerAsync<TController>(
                TController controller,
                HttpMethod method,
                InvokeControllerDelegate<object> callback)
            where TController : ApiController
        {
            return await InvokeControllerAsync<TController>(
                controller, method,
                (httpRequest, methodInfo) =>
                {
                    var resource = callback(httpRequest, controller.User as MockPrincipal);
                    return methodInfo.GetParameters()
                        .Select(
                            param =>
                            {
                                if (param.ParameterType.IsAssignableFrom(resource.GetType()))
                                    return resource;

                                if (param.ParameterType.IsAssignableFrom(typeof(Resources.WebIdQuery)))
                                {
                                    var idProperty = resource.GetType().GetProperty("Id");
                                    var id = idProperty.GetValue(resource);
                                    return id;
                                }

                                if (param.ParameterType.IsAssignableFrom(typeof(Guid)))
                                {
                                    var idProperty = resource.GetType().GetProperty("Id");
                                    var idQuery = (Resources.WebId)idProperty.GetValue(resource);
                                    return idQuery.UUID;
                                }

                                return new object();
                            })
                        .ToArray();
                });
        }

        private async Task<HttpResponseMessage> InvokeControllerAsync<TController>(
                TController controller,
                HttpMethod method)
            where TController : ApiController
        {
            return await InvokeControllerAsync<TController>(
                controller, method,
                (request, methodInfo) =>
                {
                    if (methodInfo.GetParameters().Length != 0)
                        throw new Exception("Must specify parameters to call " + methodInfo.ToString());
                    return new object[] { };
                });
        }
        private async Task<HttpResponseMessage> InvokeControllerAsync<TController>(
                TController controller,
                HttpMethod method,
                Func<HttpRequestMessage, System.Reflection.MethodInfo, object []> getParameters)
            where TController : ApiController
        {
            var httpRequest = GetRequest(controller, method);

            var methodName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(method.ToString().ToLower());
            var methodInfo = typeof(TController).GetMethod(methodName);
            if (null == methodInfo)
                Assert.Fail("Method {0} not supported on {1}", methodName, controller.GetType().Name);

            IHttpActionResult resourceFromController;
            var parameters = getParameters(httpRequest, methodInfo);
            if (methodInfo.ReturnType.GUID == typeof(Task<IHttpActionResult>).GUID)
            {
                var resourceFromControllerTask = (Task<IHttpActionResult>)methodInfo.Invoke(controller, parameters);
                resourceFromController = await resourceFromControllerTask;
            }
            else if (methodInfo.ReturnType.GUID == typeof(HttpResponseMessage).GUID)
            {
                var responseMessage = (HttpResponseMessage)methodInfo.Invoke(controller, parameters);
                resourceFromController = responseMessage.ToActionResult();
            }
            else
            {
                resourceFromController = (IHttpActionResult)methodInfo.Invoke(controller, parameters);
            }
            var response = await resourceFromController.ExecuteAsync(CancellationToken.None);
            foreach (var header in response.Headers)
            {
                if (String.Compare(header.Key, "Set-Cookie", true) == 0)
                {
                    // TODO: Store these for next request
                }
            }
            return response;
        }
        
        private async Task<HttpResponseMessage> InvokeControllerAsync<TController>(
                TController controller,
                HttpMethod method,
                Action<HttpRequestMessage, System.Reflection.MethodInfo> callback)
            where TController : ApiController
        {
            return await InvokeControllerAsync<TController>(controller, method,
                (request, methodInfo) =>
                {
                    callback(request, methodInfo);
                    return (new object[] { });
                });
        }
    }
}
