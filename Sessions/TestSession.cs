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
using System.Configuration;
using System.Net.Http.Headers;

using EastFive.Web.Services;
using EastFive.Collections.Generic;
using EastFive.Extensions;
using EastFive.Linq;
using EastFive.Api.Azure.Credentials;
using EastFive.Api.Azure.Credentials.Resources;
using EastFive.Web;
using EastFive.Api;

namespace BlackBarLabs.Api.Tests
{
    public class TestSession : ITestSession
    {
        public static TResult Start<TResult>(Func<TestSession, TResult> callback)
        {
            var session = new TestSession();
            var callbackTask = callback(session);
            return callbackTask;
        }

        public static TResult Start<TResult>(Guid actorId, Func<TestSession, TResult> callback)
        {
            var session = new TestSession(actorId);
            var callbackTask = callback(session);
            return callbackTask;
        }

        // [Obsolete("Use Start")]
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
        }

        public TestSession(string authorization)
        {
            Headers = new Dictionary<string, string>();
            Headers.Add("Authorization", authorization);
        }

        public TestSession(Guid actorId)
        {
            Id = actorId;
            Headers = new Dictionary<string, string>();
            var token = BlackBarLabs.Api.Tests.TestSession.CreateToken(actorId);
            Headers.Add("Authorization", token);
        }

        public TestSession(Guid actorId, IDictionary<string, string> claims)
        {
            Id = actorId;
            Headers = new Dictionary<string, string>();
            var token = BlackBarLabs.Api.Tests.TestSession.CreateToken(actorId, claims);
            Headers.Add("Authorization", token);
        }

        public void LoadToken(string jwtToken)
        {
            if(Headers.ContainsKey("Authorization"))
            {
                Headers["Authorization"] = jwtToken;
                return;
            }
            Headers.Add("Authorization", jwtToken);
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
                    mailService = new MockMailService(null);
                return mailService;
            }
        }

        #region Methods


        public async Task<IHttpResponse> PostAsync<TController>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>))
        {
            var controller = GetController<TController>();
            return await InvokeControllerAsync(controller, HttpMethod.Post,
                (request, user) =>
                {
                    request.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(resource));
                    return resource;
                });
        }

        public async Task<IHttpResponse> PostMultipartAsync<TController>(Action<MultipartContent> multipartContentCallback)
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

        public async Task<IHttpResponse> PutAsync<TController>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>))
        {
            var controller = GetController<TController>();
            return await InvokeControllerAsync(controller, HttpMethod.Put,
                (request, user) =>
                {
                    return resource;
                });
        }

        public async Task<IHttpResponse> PutMultipartAsync<TController>(Action<MultipartContent> multipartContentCallback)
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
            return EastFive.Web.Configuration.Settings.GetString(
                EastFive.Api.AppSettings.ActorIdClaimType,
                (actorIdClaimType) =>
                {
                    claims.AddOrReplace(actorIdClaimType, actorId.ToString());
                    var token = EastFive.Api.Auth.JwtTools.CreateToken(Guid.NewGuid(), actorId,
                        new Uri("http://test.example.com"), TimeSpan.FromHours(1.0), claims,
                        (tokenNew) => tokenNew,
                        (missingConfig) => { Assert.Fail(missingConfig); return string.Empty; },
                        (configName, issue) => { Assert.Fail($"{configName} -- {issue}"); return string.Empty; });
                    return token;;
                },
                (why) =>
                {
                    Assert.Fail(why);
                    throw new Exception();
                });
        }

        public static string CreateToken(Guid actorId, IDictionary<string, string> claims)
        {
            var actorIdClaimType = ConfigurationManager.AppSettings[EastFive.Api.AppSettings.ActorIdClaimType];
            claims.AddOrReplace(actorIdClaimType, actorId.ToString());
            var token = EastFive.Api.Auth.JwtTools.CreateToken(Guid.NewGuid(), actorId,
                new Uri("http://test.example.com"), TimeSpan.FromHours(1.0), claims,
                (tokenNew) => tokenNew,
                (missingConfig) => { Assert.Fail(missingConfig); return string.Empty; },
                (configName, issue) => { Assert.Fail($"{configName} -- {issue}"); return string.Empty; });
            return token;
        }

        public async Task<TResult> GetAsync<TController, TResult>(
                Func<IHttpResponse, TResult> callback)
        {
            var controller = GetController<TController>();
            var response = await InvokeControllerAsync(controller, HttpMethod.Get);
            return callback(response);
        }

        public async Task<IHttpResponse> GetAsync<TController>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>))
        {
            var controller = GetController<TController>();
            return await InvokeControllerAsync(controller, HttpMethod.Get,
                (request, user) =>
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (!mutateRequest.IsDefault())
                        mutateRequest(request);
                    return resource;
                });
        }

        public async Task<TResult> GetAsync<TController, TResult>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>))
        {
            var response = await this.GetAsync<TController>(resource, mutateRequest);
            response.Assert(System.Net.HttpStatusCode.OK);
            var results = response.GetContent<TResult>();
            return results;
        }

        public async Task<TResult> GetAsync<TController, TResult>(object resource,
                HttpActionDelegate<object, TResult> callback)
        {
            var response = await this.GetAsync<TController>(resource);
            var results = callback(response, resource);
            return results;
        }
        
        public async Task<IHttpResponse> DeleteAsync<TController>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>))
        {
            var controller = GetController<TController>();
            return await InvokeControllerAsync(controller, HttpMethod.Delete,
                (request, user) =>
                {
                    return resource;
                });
        }

        public async Task<TResult> OptionsAsync<TController, TResult>(
                Func<IHttpResponse, HttpMethod[], TResult> callback)
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
                Func<IHttpResponse, HttpMethod[], TResult> callback)
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
        
        public TResult MockMailService<TResult>(MockMailService.SendEmailMessageDelegate callback,
            Func<TResult> onMocked)
        {
            var currentMailFetch = EastFive.Web.Services.ServiceConfiguration.SendMessageService;
            var mockMailService = new MockMailService(callback);
            EastFive.Web.Services.ServiceConfiguration.SendMessageService =
                () => mockMailService;
            
            var result = onMocked();
            EastFive.Web.Services.ServiceConfiguration.SendMessageService = currentMailFetch;
            return result;
        }

        private TResult SendgridMailService<TResult>(ITestSession session,
            Func<TResult> callback)
        {
            var currentMailFetch = EastFive.Web.Services.ServiceConfiguration.SendMessageService;
            //var sendgridMailer = EastFive.SendGrid.SendGridMailer.Load(
            //    mailer => mailer,
            //    (why)=> throw new Exception(why));
            //EastFive.Web.Services.ServiceConfiguration.SendMessageService =
            //    () => sendgridMailer;

            var result = callback();
            EastFive.Web.Services.ServiceConfiguration.SendMessageService = currentMailFetch;
            return result;
        }

        private IHttpRequest GetRequest<TController>(TController controller, HttpMethod method)
        {
            var hostingLocation = Microsoft.Azure.CloudConfigurationManager.GetSetting(EastFive.Api.Tests.AppSettings.ServerUrl);
            if (String.IsNullOrWhiteSpace(hostingLocation))
                hostingLocation = "http://example.com";
            var httpRequest = new IHttpRequest(method, hostingLocation);
            var config = new HttpConfiguration();

            var routesApi = Microsoft.Azure.CloudConfigurationManager.GetSetting(EastFive.Api.Tests.AppSettings.RoutesApi);
            if (String.IsNullOrWhiteSpace(routesApi))
                routesApi = "DefaultApi";
            var routesMvc = Microsoft.Azure.CloudConfigurationManager.GetSetting(EastFive.Api.Tests.AppSettings.RoutesMvc);
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
            
            controller.Request = httpRequest;
            foreach (var headerKVP in Headers)
            {
                httpRequest.Headers.Add(headerKVP.Key, headerKVP.Value);
            }
            
            return httpRequest;
        }

        public Dictionary<string, string> Headers { get; set; }

        private TController GetController<TController>()
        {
            var controller = Activator.CreateInstance<TController>();
            return controller;
        }

        private delegate T InvokeControllerDelegate<T>(IHttpRequest request, MockPrincipal user);

        private async Task<IHttpResponse> InvokeControllerAsync<TController>(
                TController controller,
                HttpMethod method,
                InvokeControllerDelegate<object> callback)
        {
            return await InvokeControllerAsync<TController>(
                controller, method,
                (IHttpRequest httpRequest, System.Reflection.MethodInfo methodInfo) =>
                {
                    var resource = callback(httpRequest, controller.User as MockPrincipal);
                    return methodInfo.GetParameters()
                        .ReduceItems<bool, System.Reflection.ParameterInfo, object>(
                            false,
                            (aggr1, param, next, skip) =>
                            {
                                if (param.ParameterType.IsAssignableFrom(resource.GetType()))
                                {
                                    return next(resource, true);
                                }

                                if (param.ParameterType.IsAssignableFrom(typeof(Resources.WebIdQuery)))
                                {
                                    var idProperty = resource.GetType().GetProperty("Id");
                                    var id = idProperty.GetValue(resource);
                                    return next(id, aggr1);
                                }

                                if (param.ParameterType.IsAssignableFrom(typeof(Guid)))
                                {
                                    var idProperty = resource.GetType().GetProperty("Id");
                                    var idQuery = (Resources.WebId)idProperty.GetValue(resource);
                                    return next(idQuery.UUID, aggr1);
                                }

                                return next(new object(), aggr1);
                            }).ToArray();
                });
        }

        private async Task<IHttpResponse> InvokeControllerAsync<TController>(
                TController controller,
                HttpMethod method)
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

        private async Task<IHttpResponse> InvokeControllerAsync<TController>(
                TController controller,
                HttpMethod method,
                Func<IHttpRequest, System.Reflection.MethodInfo, object []> getParameters)
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
            else if (methodInfo.ReturnType.GUID == typeof(IHttpResponse).GUID)
            {
                var responseMessage = (IHttpResponse)methodInfo.Invoke(controller, parameters);
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
        
        private async Task<IHttpResponse> InvokeControllerAsync<TController>(
                TController controller,
                HttpMethod method,
                Action<IHttpRequest, System.Reflection.MethodInfo> callback)
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
