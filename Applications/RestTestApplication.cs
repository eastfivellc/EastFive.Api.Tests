using EastFive.Analytics;
using EastFive.Api.Serialization;
using EastFive.Collections.Generic;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public class RestTestApplication : ITestApplication, IApplication
    {
        private string authenticationToken;

        public RestTestApplication()
        {

        }

        public RestTestApplication(string authenticationToken)
        {
            this.authenticationToken = authenticationToken;
        }

        public IDictionary<string, string> Headers
        {
            get
            {
                if (this.authenticationToken.IsNullOrWhiteSpace())
                    return new Dictionary<string, string>();
                return new Dictionary<string, string>()
                {
                    { "Authentication", this.authenticationToken },
                    { "Authorization",  this.authenticationToken }
                };
            }
        }

        public Uri ServerLocation => EastFive.Web.Configuration.Settings.GetUri(
                        EastFive.Api.Tests.AppSettings.ServerUrl,
                    (routesApiFound) => routesApiFound,
                    (why) => throw new Exception(why));

        public string ApiRouteName => EastFive.Web.Configuration.Settings.GetString(
                        EastFive.Api.Tests.AppSettings.RoutesApi,
                    (routesApiFound) => routesApiFound,
                    (why) => "DefaultApi");

        public ILogger Logger => throw new NotImplementedException();

        public object CastResourceProperty(object value, Type propertyType)
        {
            throw new NotImplementedException();
        }

        private Dictionary<HttpStatusCode, InstigatorDelegate> instigators =
            new Dictionary<HttpStatusCode, InstigatorDelegate>();

        public void SetInstigator(Type type, InstigatorDelegate instigator, bool clear = false)
        {
            if(type.ContainsCustomAttribute<HttpActionDelegateAttribute>())
            {
                var actionDelAttr = type.GetCustomAttribute<HttpActionDelegateAttribute>();
                var code = actionDelAttr.StatusCode;

                if (!clear)
                {
                    instigators.AddOrReplace(code, instigator);
                    return;
                }
                if (instigators.ContainsKey(code))
                    instigators.Remove(code);

            }
        }

        private Dictionary<HttpStatusCode, InstigatorDelegateGeneric> instigatorsGeneric =
            new Dictionary<HttpStatusCode, InstigatorDelegateGeneric>();

        public void SetInstigatorGeneric(Type type, InstigatorDelegateGeneric instigator,
            bool clear = false)
        {
            if (type.ContainsCustomAttribute<HttpActionDelegateAttribute>())
            {
                var actionDelAttr = type.GetCustomAttribute<HttpActionDelegateAttribute>();
                var code = actionDelAttr.StatusCode;
                if (!clear)
                {
                    instigatorsGeneric.AddOrReplace(code, instigator);
                    return;
                }
                if (instigatorsGeneric.ContainsKey(code))
                    instigatorsGeneric.Remove(code);
            }
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request);

                if (instigators.ContainsKey(response.StatusCode))
                {
                    var instigator = instigators[response.StatusCode];
                    var resultAttempt = await instigator(null, request, null,
                        async (data) =>
                        {
                            var dataType = data.GetType();
                            var invokeMethod = dataType.GetMethod("Invoke");
                            var invokeParameters = invokeMethod.GetParameters();
                            if (!invokeParameters.Any())
                            {
                                var invokeResponseObj = (data as Delegate).DynamicInvoke(new object[] { });
                                var invokeResponse = invokeResponseObj as HttpResponseMessage;
                                return invokeResponse;
                            }
                            return await response.AsTask();
                        });
                    if (resultAttempt is ApplicationRequestExtensions.IReturnResult)
                    {
                        return resultAttempt;
                    }
                }

                if (instigatorsGeneric.ContainsKey(response.StatusCode))
                {
                    var instigatorGeneric = instigatorsGeneric[response.StatusCode];
                    return await instigatorGeneric(default(Type), null, request, null,
                        async (data) =>
                        {
                            var dataType = data.GetType();
                            if (dataType.IsSubClassOfGeneric(typeof(Controllers.CreatedBodyResponse<>)))
                            {
                                var jsonString = await response.Content.ReadAsStringAsync();
                                var resourceType = dataType.GenericTypeArguments.First();
                                var converter = new Serialization.Converter();
                                var instance = Newtonsoft.Json.JsonConvert.DeserializeObject(
                                    jsonString, resourceType, converter);
                                var responseDelegate = ((Delegate)data).DynamicInvoke(
                                    instance, response.Content.Headers.ContentType.MediaType);
                                return (HttpResponseMessage)responseDelegate;
                            }
                            if (dataType.IsSubClassOfGeneric(typeof(Controllers.ExecuteBackgroundResponseAsync)))
                            {
                                //var jsonString = await response.Headers.();
                                //var resourceType = dataType.GenericTypeArguments.First();
                                //var converter = new RefConverter();
                                //var instance = Newtonsoft.Json.JsonConvert.DeserializeObject(
                                //    jsonString, resourceType, converter);
                                var responseDelegate = ((Delegate)data).DynamicInvoke(
                                    new ExecuteContext());
                                return (HttpResponseMessage)responseDelegate;
                            }
                            return response;
                        });
                }

                return response;
            }
        }

        public TResult GetControllerMethods<TResult>(string routeName, Func<IDictionary<HttpMethod, MethodInfo[]>, TResult> onMethodsIdentified, Func<TResult> onKeyNotFound)
        {
            throw new NotImplementedException();
        }

        public TResult Bind<TResult>(Type type, IParseToken content, Func<object, TResult> onParsed, Func<string, TResult> onDidNotBind)
        {
            throw new NotImplementedException();
        }

        public Task<TResult> ParseContentValuesAsync<TParseResult, TResult>(HttpContent content, Func<ParseContentDelegate<TParseResult>, string[], Task<TResult>> onParsedContentValues)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> Instigate(HttpRequestMessage request, ParameterInfo methodParameter, Func<object, Task<HttpResponseMessage>> onInstigated)
        {
            throw new NotImplementedException();
        }

        public RequestMessage<TResource> GetRequest<TResource>()
        {
            throw new NotImplementedException();
        }
    }

    public class ExecuteContext : Controllers.IExecuteAsync
    {
        public bool ForceBackground => false;

        public Task<HttpResponseMessage> InvokeAsync(Action<double> updateCallback)
        {
            throw new NotImplementedException();
        }
    }

}
