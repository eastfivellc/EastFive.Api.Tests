using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using EastFive.Linq;
using EastFive.Collections.Generic;
using EastFive;
using EastFive.Reflection;
using EastFive.Extensions;
using EastFive.Api.Controllers;
using EastFive.Linq.Expressions;
using BlackBarLabs.Extensions;
using BlackBarLabs.Api;
using EastFive.Linq.Async;

namespace EastFive.Api.Tests
{
    public static class ApplicationRequestExtensions
    {

        private static HttpRequestMessage GetRequest(this ITestApplication application,
            HttpMethod method, FunctionViewControllerAttribute functionViewControllerAttribute)
        {
            var hostingLocation = Web.Configuration.Settings.GetUri(
                    AppSettings.ServerUrl,
                (hostingLocationFound) => hostingLocationFound,
                (whyUnspecifiedOrInvalid) => new Uri("http://example.com"));

            var routesApiCSV = EastFive.Web.Configuration.Settings.GetString(
                    EastFive.Api.Tests.AppSettings.RoutesApi,
                (routesApiFound) => routesApiFound,
                (why) => "DefaultApi");

            var routesMvcCSV = EastFive.Web.Configuration.Settings.GetString(
                    EastFive.Api.Tests.AppSettings.RoutesMvc,
                (routesApiFound) => routesApiFound,
                (why) => "Default");

            var routesApi = routesApiCSV.Split(new[] { ',' }).ToArray();


            var httpRequest = new HttpRequestMessage(); // $"{hostingLocation}/{routesApi[0]}/{functionViewControllerAttribute.Route}");
            httpRequest.Method = method;
            var config = new HttpConfiguration();

            var firstApiRoute = routesApi.Select(
                routeName =>
                {
                    var route = config.Routes.MapHttpRoute(
                        name: routeName,
                        routeTemplate: routeName + "/{controller}/{id}",
                        defaults: new { id = RouteParameter.Optional }
                    );
                    httpRequest.SetRouteData(new System.Web.Http.Routing.HttpRouteData(route));
                    return route;
                }).First();

            IHttpRoute[] mvcRoutes = routesMvcCSV.Split(new[] { ',' }).Select(
                routeName =>
                {
                    var route = config.Routes.MapHttpRoute(
                        name: routeName,
                        routeTemplate: "{controller}/{action}/{id}",
                        defaults: new { controller = "Default", action = "Index", id = "" }
                        );
                    httpRequest.SetRouteData(new System.Web.Http.Routing.HttpRouteData(route));
                    return route;
                }).ToArray();

            var urlTemplate = $"{hostingLocation}/{firstApiRoute.RouteTemplate}";
            //var routeValues = firstApiRoute.Defaults.SelectValues().Append(functionViewControllerAttribute.Route).Reverse().ToArray();
            var requestUriString = // String.Format( urlTemplate, routeValues);
                urlTemplate
                    .Replace("{controller}", functionViewControllerAttribute.Route)
                    .Replace("/{id}", string.Empty);

            httpRequest.RequestUri = new Uri(requestUriString);

            httpRequest.SetConfiguration(config);

            foreach (var headerKVP in application.Headers)
                httpRequest.Headers.Add(headerKVP.Key, headerKVP.Value);

            return httpRequest;
        }

        private static HttpRequestMessage GetRequest(this ITestApplication application, HttpMethod method, Uri location)
        {
            var httpRequest = new HttpRequestMessage(); // $"{hostingLocation}/{routesApi[0]}/{functionViewControllerAttribute.Route}");
            httpRequest.Method = method;
            var config = new HttpConfiguration();

            httpRequest.RequestUri = location;

            httpRequest.SetConfiguration(config);

            foreach (var headerKVP in application.Headers)
                httpRequest.Headers.Add(headerKVP.Key, headerKVP.Value);

            return httpRequest;
        }

        public static Task<TResult> ActionAsync<TResource, TResult>(this ITestApplication application,
                string action,
                Expression<Action<TResource>> param1,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>),
            Func<TResult> onCreated = default(Func<TResult>),
            Func<TResource, string, TResult> onCreatedBody = default(Func<TResource, string, TResult>),
            Func<TResult> onUpdated = default(Func<TResult>),

            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),

            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onUnauthorized = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<string, TResult> onFailure = default(Func<string, TResult>),

            Func<TResult> onNotImplemented = default(Func<TResult>))
        {
            return application.ActionAsync<TResource, TResult>(action,
                    new Expression<Action<TResource>>[] { param1 },
                onContent: onContent,
                onContents: onContents,
                onHtml: onHtml,
                onCreated: onCreated,
                onCreatedBody: onCreatedBody,
                onUpdated: onUpdated,
                onRedirect: onRedirect,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onFailure: onFailure,
                onNotImplemented: onNotImplemented);
        }

        public static Task<TResult> ActionAsync<TResource, TResult>(this ITestApplication application,
                string action,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>),
            Func<TResult> onCreated = default(Func<TResult>),
            Func<TResource, string, TResult> onCreatedBody = default(Func<TResource, string, TResult>),
            Func<TResult> onUpdated = default(Func<TResult>),

            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),

            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onUnauthorized = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<string, TResult> onFailure = default(Func<string, TResult>),

            Func<TResult> onNotImplemented = default(Func<TResult>))
        {
            return application.ActionAsync<TResource, TResult>(action,
                    new Expression<Action<TResource>>[] { },
                onContent: onContent,
                onContents: onContents,
                onHtml: onHtml,
                onCreated: onCreated,
                onCreatedBody: onCreatedBody,
                onUpdated: onUpdated,
                onRedirect: onRedirect,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onFailure: onFailure,
                onNotImplemented: onNotImplemented);
        }

        public static Task<TResult> ActionAsync<TResource, TResult>(this ITestApplication application,
                string action,
                Expression<Action<TResource>> [] paramSet,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>),
            Func<TResult> onCreated = default(Func<TResult>),
            Func<TResource, string, TResult> onCreatedBody = default(Func<TResource, string, TResult>),
            Func<TResult> onUpdated = default(Func<TResult>),

            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),

            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onUnauthorized = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<string, TResult> onFailure = default(Func<string, TResult>),

            Func<TResult> onNotImplemented = default(Func<TResult>))
        {
            return application.MethodAsync<TResource, TResult, TResult>(HttpMethod.Get,
                (request) =>
                {
                    var actionUrl = new Uri(request.RequestUri + $"/{action}");
                    request.RequestUri = actionUrl.AssignQueryExpressions(application, paramSet);
                    return request;
                },
                (TResult result) =>
                {
                    return result;
                },
                onContent: onContent,
                onContents: onContents,
                onHtml: onHtml,
                onCreated: onCreated,
                onCreatedBody: onCreatedBody,
                onUpdated: onUpdated,
                onRedirect: onRedirect,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onFailure: onFailure,
                onNotImplemented: onNotImplemented);
        }

        public static Task<TResult> MethodAsync<TResource, TResultInner, TResult>(this ITestApplication application,
                HttpMethod method,
                Func<HttpRequestMessage, HttpRequestMessage> requestMutation,
            Func<TResultInner, TResult> onExecuted,

            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>),
            Func<TResult> onCreated = default(Func<TResult>),
            Func<TResource, string, TResult> onCreatedBody = default(Func<TResource, string, TResult>),
            Func<TResult> onUpdated = default(Func<TResult>),

            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),

            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onUnauthorized = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<string, TResult> onFailure = default(Func<string, TResult>),

            Func<TResult> onNotImplemented = default(Func<TResult>),
            Func<IExecuteAsync, Task<TResult>> onExecuteBackground = default(Func<IExecuteAsync, Task<TResult>>))
        {
            application.CreatedResponse<TResource, TResult>(onCreated);
            application.CreatedBodyResponse<TResource, TResult>(onCreatedBody);
            application.BadRequestResponse<TResource, TResult>(onBadRequest);
            application.AlreadyExistsResponse<TResource, TResult>(onExists);
            application.RefNotFoundTypeResponse(onRefDoesNotExistsType);
            application.RedirectResponse<TResource, TResult>(onRedirect);
            application.NotImplementedResponse<TResource, TResult>(onNotImplemented);

            application.ContentResponse(onContent);
            application.ContentTypeResponse<TResource, TResult>((body, contentType) => onContent(body));
            application.MultipartContentResponse(onContents);
            if(!onContentObjects.IsDefaultOrNull())
                application.MultipartContentObjectResponse<TResource, TResult>(onContentObjects);
            application.NotFoundResponse<TResource, TResult>(onNotFound);
            application.HtmlResponse<TResource, TResult>(onHtml);

            application.NoContentResponse<TResource, TResult>(onUpdated);
            application.UnauthorizedResponse<TResource, TResult>(onUnauthorized);
            application.GeneralConflictResponse<TResource, TResult>(onFailure);
            application.ExecuteBackgroundResponse<TResource, TResult>(onExecuteBackground);

            return application.MethodAsync<TResource, TResultInner, TResult>(method,
                requestMutation,
                onExecuted);
        }

        public static Task<TResult> MethodAsync<TResource, TResultInner, TResult>(this ITestApplication application,
                HttpMethod method,
                Func<HttpRequestMessage, HttpRequestMessage> requestMutation,
            Func<TResultInner, TResult> onExecuted)
        {
            return typeof(TResource).GetCustomAttribute<FunctionViewControllerAttribute, Task<TResult>>(
                async fvcAttr =>
                {
                    var requestGeneric = application.GetRequest(method, fvcAttr);
                    var request = requestMutation(requestGeneric);

                    var response = await application.SendAsync(request);
                    //var response = await EastFive.Api.Modules.ControllerHandler.DirectSendAsync(application as EastFive.Api.HttpApplication, request, default(CancellationToken),
                    //    (requestBack, token) =>
                    //    {
                    //        Assert.Fail($"Failed to invoke {fvcAttr.Route}");
                    //        throw new Exception();
                    //    });

                    if(response is IDidNotOverride)
                    {
                        (response as IDidNotOverride).OnFailure();
                    }

                    if (!(response is IReturnResult))
                        Assert.Fail($"Failed to override response with status code `{response.StatusCode}` for {typeof(TResource).FullName}\nResponse:{response.ReasonPhrase}");

                    var attachedResponse = response as IReturnResult;
                    var result = attachedResponse.GetResultCasted<TResultInner>();
                    return onExecuted(result);
                },
                () =>
                {
                    Assert.Fail($"Type {typeof(TResource).FullName} does not have FunctionViewControllerAttribute");
                    throw new Exception();
                });
        }

        public static async Task<TResult> UrlAsync<TResource, TResultInner, TResult>(this ITestApplication application,
                HttpMethod method, Uri location,
            Func<TResultInner, TResult> onExecuted,

            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>),
            Func<TResult> onCreated = default(Func<TResult>),
            Func<TResource, string, TResult> onCreatedBody = default(Func<TResource, string, TResult>),
            Func<TResult> onUpdated = default(Func<TResult>),

            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),

            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onUnauthorized = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<string, TResult> onFailure = default(Func<string, TResult>),

            Func<TResult> onNotImplemented = default(Func<TResult>),
            Func<IExecuteAsync, Task<TResult>> onExecuteBackground = default(Func<IExecuteAsync, Task<TResult>>))
        {
            application.CreatedResponse<TResource, TResult>(onCreated);
            application.CreatedBodyResponse<TResource, TResult>(onCreatedBody);
            application.BadRequestResponse<TResource, TResult>(onBadRequest);
            application.AlreadyExistsResponse<TResource, TResult>(onExists);
            application.RefNotFoundTypeResponse(onRefDoesNotExistsType);
            application.RedirectResponse<TResource, TResult>(onRedirect);
            application.NotImplementedResponse<TResource, TResult>(onNotImplemented);

            application.ContentResponse(onContent);
            application.ContentTypeResponse<TResource, TResult>((body, contentType) => onContent(body));
            application.MultipartContentResponse(onContents);
            if (!onContentObjects.IsDefaultOrNull())
                application.MultipartContentObjectResponse<TResource, TResult>(onContentObjects);
            application.NotFoundResponse<TResource, TResult>(onNotFound);
            application.HtmlResponse<TResource, TResult>(onHtml);

            application.NoContentResponse<TResource, TResult>(onUpdated);
            application.UnauthorizedResponse<TResource, TResult>(onUnauthorized);
            application.GeneralConflictResponse<TResource, TResult>(onFailure);
            application.ExecuteBackgroundResponse<TResource, TResult>(onExecuteBackground);

            var request = application.GetRequest(method, location);
            var response = await application.SendAsync(request);

            if (response is IDidNotOverride)
            {
                (response as IDidNotOverride).OnFailure();
            }

            if (!(response is IReturnResult))
                Assert.Fail($"Failed to override response with status code `{response.StatusCode}` for {typeof(TResource).FullName}\nResponse:{response.ReasonPhrase}");

            var attachedResponse = response as IReturnResult;
            var result = attachedResponse.GetResultCasted<TResultInner>();
            return onExecuted(result);
        }

        private static Uri AssignQueryExpressions<TResource>(this Uri baseUri, ITestApplication application, Expression<Action<TResource>>[] parameters)
        {
            var queryParams = parameters
                .Select(
                    param =>
                    {
                        return param.GetUrlAssignment(
                            (propName, value) =>
                            {
                                var propertyValue = (string)application.CastResourceProperty(value, typeof(String));
                                return propName.PairWithValue(propertyValue);
                            });
                    })
                .Concat(baseUri.ParseQuery())
                .ToDictionary();

            var updatedUri = baseUri.SetQuery(queryParams);
            return updatedUri;
        }

        private static Uri AssignResourceToQuery<TResource>(this Uri baseUri, ITestApplication application, TResource resource)
        {
            var queryParams = typeof(TResource)
                .GetMembers()
                .Where(member => member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                .Select(
                    memberInfo =>
                    {
                        var propName = memberInfo.GetCustomAttribute<JsonPropertyAttribute, string>(
                            jsonAttr => jsonAttr.PropertyName,
                            () => memberInfo.Name);
                        var value = memberInfo.GetValue(resource);
                        var propertyValue = (string)application.CastResourceProperty(value, typeof(String));
                        return propName.PairWithValue(propertyValue);
                    })
                .Concat(baseUri.ParseQuery())
                .ToDictionary();

            var updatedUri = baseUri.SetQuery(queryParams);
            return updatedUri;
        }

        public static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
                TResource resourceForQuery,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>))
        {
            return application.MethodAsync<TResource, TResult, TResult>(HttpMethod.Get,
                (request) =>
                {
                    request.RequestUri = request.RequestUri.AssignResourceToQuery(application, resourceForQuery);
                    return request;
                },
                (TResult result) =>
                {
                    return result;
                },
                onContent: onContent,
                onContents: onContents,
                onContentObjects: onContentObjects,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect: onRedirect,
                onHtml: onHtml);

        }

        private static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
                Expression<Action<TResource>>[] parameters,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>))
        {
            return application.MethodAsync<TResource, TResult, TResult>(HttpMethod.Get,
                (request) =>
                {
                    request.RequestUri = AssignQueryExpressions(request.RequestUri, application, parameters);
                    return request;
                },
                (TResult result) =>
                {
                    return result;
                },
                onContent: onContent,
                onContents: onContents,
                onContentObjects: onContentObjects,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect: onRedirect,
                onHtml: onHtml);

        }

        public static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>),
            Func<IExecuteAsync, Task<TResult>> onExecuteBackground = default(Func<IExecuteAsync, Task<TResult>>))
        {
            return application.GetAsync(new Expression<Action<TResource>>[] {  },
                onContent: onContent,
                onContents: onContents,
                onContentObjects: onContentObjects,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect:onRedirect,
                onHtml: onHtml);
        }


        public static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
                Expression<Action<TResource>> param1,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>))
        {
            return application.GetAsync(new[] { param1 },
                onContent: onContent,
                onContents: onContents,
                onContentObjects: onContentObjects,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect: onRedirect,
                onHtml: onHtml);
        }

        public static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
                Expression<Action<TResource>> param1,
                Expression<Action<TResource>> param2,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>))
        {
            return application.GetAsync(
                    new Expression<Action<TResource>>[] { param1, param2 },
                onContent: onContent,
                onContents: onContents,
                onContentObjects: onContentObjects,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect: onRedirect,
                onHtml: onHtml);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="application"></param>
        /// <param name="resource"></param>
        /// <param name="onCreated"></param>
        /// <param name="onBadRequest"></param>
        /// <param name="onExists"></param>
        /// <param name="onRefDoesNotExistsType"></param>
        /// <param name="onNotImplemented"></param>
        /// <returns></returns>
        /// <remarks>Response hooks are only called if the method is actually invoked. Responses from the framework are not trapped.</remarks>
        public static Task<TResult> PostAsync<TResource, TResult>(this ITestApplication application,
                TResource resource,
            Func<TResult> onCreated = default(Func<TResult>),
            Func<TResource, string, TResult> onCreatedBody = default(Func<TResource, string, TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<TResult> onNotImplemented = default(Func<TResult>),
            Func<IExecuteAsync, Task<TResult>> onExecuteBackground = default(Func<IExecuteAsync, Task<TResult>>))
        {
            return application.MethodAsync<TResource, TResult, TResult>(HttpMethod.Post,
                (request) =>
                {
                    var contentJsonString = JsonConvert.SerializeObject(resource, new RefConverter());
                    request.Content = new StreamContent(contentJsonString.ToStream());
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    return request;
                },
                (TResult result) =>
                {
                    return result;
                },
                onCreated: onCreated,
                onCreatedBody: onCreatedBody,
                onBadRequest: onBadRequest,
                onExists: onExists,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect: onRedirect,
                onNotImplemented: onNotImplemented,
                onExecuteBackground: onExecuteBackground);
        }

        private static Task<TResult> DeleteAsync<TResource, TResult>(this ITestApplication application,
                Expression<Action<TResource>>[] parameters,
            Func<TResult> onNoContent = default(Func<TResult>),
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>))
        {
            return application.MethodAsync<TResource, TResult, TResult>(HttpMethod.Delete,
                (request) =>
                {
                    request.RequestUri = AssignQueryExpressions(request.RequestUri, application, parameters);
                    return request;
                },
                (TResult result) =>
                {
                    return result;
                },
                onUpdated: onNoContent,
                onContent: onContent,
                onContents: onContents,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect: onRedirect,
                onHtml: onHtml);
        }

        public static Task<TResult> DeleteAsync<TResource, TResult>(this ITestApplication application,
                Expression<Action<TResource>> param1,
            Func<TResult> onNoContent = default(Func<TResult>),
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>))
        {
            return application.DeleteAsync(new[] { param1 },
                onNoContent: onNoContent,
                onContent: onContent,
                onContents: onContents,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect: onRedirect,
                onHtml: onHtml);
        }

        public static Task<TResult> DeleteAsync<TResource, TResult>(this ITestApplication application,
            Func<TResult> onNoContent = default(Func<TResult>),
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, string, TResult> onRedirect = default(Func<Uri, string, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>))
        {
            return application.DeleteAsync(new Expression<Action<TResource>>[] { },
                onNoContent: onNoContent,
                onContent: onContent,
                onContents: onContents,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefDoesNotExistsType: onRefDoesNotExistsType,
                onRedirect: onRedirect,
                onHtml: onHtml);
        }

        public static Task<TResult> PatchAsync<TResource, TResult>(this ITestApplication application,
                TResource resource,
            Func<TResult> onUpdated = default(Func<TResult>),
            Func<TResource, TResult> onUpdatedBody = default(Func<TResource, TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<TResult> onUnauthorized = default(Func<TResult>),
            Func<string, TResult> onFailure = default(Func<string, TResult>))
        {
            return application.MethodAsync<TResource, TResult, TResult>(new HttpMethod("patch"),
                (request) =>
                {
                    var contentJsonString = JsonConvert.SerializeObject(resource, new RefConverter());
                    request.Content = new StreamContent(contentJsonString.ToStream());
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    return request;
                },
                (TResult result) =>
                {
                    return result;
                },
                onUpdated: onUpdated,
                onContent:onUpdatedBody,
                onNotFound: onNotFound,
                onUnauthorized: onUnauthorized,
                onFailure: onFailure);
        }
        
        #region Response types

        public interface IReturnResult
        {
            TResult GetResultCasted<TResult>();
        }

        private class AttachedHttpResponseMessage<TResult> : HttpResponseMessage, IReturnResult
        {
            public TResult Result { get; }

            public AttachedHttpResponseMessage(TResult result)
            {
                this.Result = result;
            }

            public HttpResponseMessage Inner { get; }
            public AttachedHttpResponseMessage(TResult result, HttpResponseMessage inner)
            {
                this.Result = result;
                this.Inner = inner;
            }

            public TResult1 GetResultCasted<TResult1>()
            {
                return (TResult1)(this.Result as object);
            }
        }

        private interface IDidNotOverride
        {
            void OnFailure();
        }

        private class NoOverrideHttpResponseMessage<TResource> : HttpResponseMessage, IDidNotOverride
        {
            private Type typeOfResponse;
            private HttpRequestMessage request;
            public NoOverrideHttpResponseMessage(Type typeOfResponse, HttpRequestMessage request)
            {
                this.typeOfResponse = typeOfResponse;
                this.request = request;
            }

            public void OnFailure()
            {
                Assert.Fail($"Failed to override response for: [{request.Method.Method}] `{typeof(TResource).FullName}`.`{typeOfResponse.Name}`");
            }
        }

        private static void ContentResponse<TResource, TResult>(this ITestApplication application,
            Func<TResource, TResult> onContent)
        {
            application.SetInstigator(
                typeof(EastFive.Api.Controllers.ContentResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.ContentResponse created =
                        (content, mimeType) =>
                        {
                            if (onContent.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(EastFive.Api.Controllers.ContentResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            if (!(content is TResource))
                                Assert.Fail($"Could not cast {content.GetType().FullName} to {typeof(TResource).FullName}.");
                            var resource = (TResource)content;
                            var result = onContent(resource);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                },
                onContent.IsDefaultOrNull());
        }

        private static void HtmlResponse<TResource, TResult>(this ITestApplication application,
            Func<string, TResult> onHtml)
        {
            application.SetInstigator(
                typeof(EastFive.Api.Controllers.HtmlResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.HtmlResponse created =
                        (content) =>
                        {
                            if (onHtml.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(EastFive.Api.Controllers.HtmlResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onHtml(content);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }

        private static void BadRequestResponse<TResource, TResult>(this ITestApplication application,
            Func<TResult> onBadRequest)
        {
            application.SetInstigator(
                typeof(EastFive.Api.Controllers.BadRequestResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.BadRequestResponse badRequest =
                        () =>
                        {
                            if (onBadRequest.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(EastFive.Api.Controllers.BadRequestResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onBadRequest();
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(badRequest);
                });
        }
        
        private static void RefNotFoundTypeResponse<TResult>(this ITestApplication application,
            Func<Type, TResult> referencedDocDoesNotExists)
        {
            application.SetInstigatorGeneric(
                typeof(ReferencedDocumentDoesNotExistsResponse<>),
                (type, thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    var scope = new CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>(referencedDocDoesNotExists,
                        thisAgain, requestAgain, paramInfo, onSuccess);
                    var multipartResponseMethodInfoGeneric = typeof(CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>)
                        .GetMethod("RefNotFoundTypeResponseGeneric", BindingFlags.Public | BindingFlags.Instance);
                    var multipartResponseMethodInfoBound = multipartResponseMethodInfoGeneric
                        .MakeGenericMethod(type.GenericTypeArguments);
                    var dele = Delegate.CreateDelegate(type, scope, multipartResponseMethodInfoBound);
                    return onSuccess((object)dele);
                },
                referencedDocDoesNotExists.IsDefaultOrNull());
        }

        public class CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>
        {
            private Func<Type, TResult> referencedDocDoesNotExists;
            private HttpApplication thisAgain;
            private HttpRequestMessage requestAgain;
            private ParameterInfo paramInfo;
            private Func<object, Task<HttpResponseMessage>> onSuccess;
            
            public CallbackWrapperReferencedDocumentDoesNotExistsResponse(Func<Type, TResult> referencedDocDoesNotExists,
                HttpApplication thisAgain, HttpRequestMessage requestAgain, ParameterInfo paramInfo, Func<object, Task<HttpResponseMessage>> onSuccess)
            {
                this.referencedDocDoesNotExists = referencedDocDoesNotExists;
                this.thisAgain = thisAgain;
                this.requestAgain = requestAgain;
                this.paramInfo = paramInfo;
                this.onSuccess = onSuccess;
            }

            public HttpResponseMessage RefNotFoundTypeResponseGeneric<TResource>()
            {
                if (referencedDocDoesNotExists.IsDefaultOrNull())
                    return FailureToOverride<TResource>(typeof(ReferencedDocumentDoesNotExistsResponse<>), thisAgain, requestAgain, paramInfo, onSuccess);

                var result = referencedDocDoesNotExists(typeof(TResource));
                return new AttachedHttpResponseMessage<TResult>(result);
            }
        }

        private static void RefNotFoundTypeResponseGeneric<T>()
        {

        }

        private class InstigatorGenericWrapper1<TCallback, TResult, TResource>
        {
            private Type type;
            private HttpApplication httpApp;
            private HttpRequestMessage request;
            private ParameterInfo paramInfo;
            private TCallback callback;
            private Func<object, Task<HttpResponseMessage>> onSuccess;

            public InstigatorGenericWrapper1(Type type,
                HttpApplication httpApp, HttpRequestMessage request, ParameterInfo paramInfo,
                TCallback callback, Func<object, Task<HttpResponseMessage>> onSuccess)
            {
                this.type = type;
                this.httpApp = httpApp;
                this.request = request;
                this.paramInfo = paramInfo;
                this.callback = callback;
                this.onSuccess = onSuccess;
            }

            HttpResponseMessage ContentTypeResponse(object content, string mediaType = default(string))
            {
                if (callback.IsDefault())
                    return FailureToOverride<TResource>(
                        type, this.httpApp, this.request, this.paramInfo, onSuccess);
                var contentObj = (object)content;
                var contentType = (TResource)contentObj;
                var callbackObj = (object)callback;
                var callbackDelegate = (Delegate)callbackObj;
                var resultObj = callbackDelegate.DynamicInvoke(contentType, mediaType);
                var result = (TResult)resultObj;
                return new AttachedHttpResponseMessage<TResult>(result);
            }

            HttpResponseMessage CreatedBodyResponse(object content, string mediaType = default(string))
            {
                if (callback.IsDefault())
                    return FailureToOverride<TResource>(
                        type, this.httpApp, this.request, this.paramInfo, onSuccess);
                var contentObj = (object)content;
                var contentType = (TResource)contentObj;
                var callbackObj = (object)callback;
                var callbackDelegate = (Delegate)callbackObj;
                var resultObj = callbackDelegate.DynamicInvoke(contentType, mediaType);
                var result = (TResult)resultObj;
                return new AttachedHttpResponseMessage<TResult>(result);
            }
        }

        private static void CreatedBodyResponse<TResource, TResult>(this ITestApplication application,
            Func<TResource, string, TResult> onCreated)
        {
            application.SetInstigatorGeneric(
                typeof(EastFive.Api.Controllers.CreatedBodyResponse<>),
                (type, httpApp, request, paramInfo, onSuccess) =>
                {
                    type = typeof(CreatedBodyResponse<>).MakeGenericType(typeof(TResource));
                    var wrapperConcreteType = typeof(InstigatorGenericWrapper1<,,>).MakeGenericType(
                        //type.GenericTypeArguments
                        //    .Append(typeof(Func<TResource, string, TResult>))
                        typeof(Func<TResource, string, TResult>)
                            .AsArray()
                            .Append(typeof(TResult))
                            .Append(typeof(TResource))
                            .ToArray());
                    var wrapperInstance = Activator.CreateInstance(wrapperConcreteType,
                        new object[] { type, httpApp, request, paramInfo, onCreated, onSuccess });
                    var dele = Delegate.CreateDelegate(type, wrapperInstance, "CreatedBodyResponse", false);
                    return onSuccess(dele);
                },
                onCreated.IsDefaultOrNull());
        }

        private static void ContentTypeResponse<TResource, TResult>(this ITestApplication application,
            Func<TResource, string, TResult> onCreated)
        {
            application.SetInstigatorGeneric(
                typeof(EastFive.Api.Controllers.ContentTypeResponse<>),
                (type, httpApp, request, paramInfo, onSuccess) =>
                {
                    type = typeof(ContentTypeResponse<>).MakeGenericType(typeof(TResource));
                    var wrapperConcreteType = typeof(InstigatorGenericWrapper1<,,>).MakeGenericType(
                        typeof(Func<TResource, string, TResult>)
                            .AsArray()
                            .Append(typeof(TResult))
                            .Append(typeof(TResource))
                            .ToArray());
                    var wrapperInstance = Activator.CreateInstance(wrapperConcreteType,
                        new object[] { type, httpApp, request, paramInfo, onCreated, onSuccess });
                    var dele = Delegate.CreateDelegate(type, wrapperInstance, "ContentTypeResponse", false);
                    return onSuccess(dele);
                },
                onCreated.IsDefaultOrNull());
        }

        private static void CreatedResponse<TResource, TResult>(this ITestApplication application,
            Func<TResult> onCreated)
        {
            application.SetInstigator(
                typeof(EastFive.Api.Controllers.CreatedResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.CreatedResponse created =
                        () =>
                        {
                            if (onCreated.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(EastFive.Api.Controllers.CreatedResponse),
                                    thisAgain, requestAgain, paramInfo, onSuccess);
                            return new AttachedHttpResponseMessage<TResult>(onCreated());
                        };
                    return onSuccess(created);
                },
                onCreated.IsDefaultOrNull());
        }

        private static void RedirectResponse<TResource, TResult>(this ITestApplication application,
            Func<Uri, string, TResult> onRedirect)
        {
            application.SetInstigator(
                typeof(EastFive.Api.Controllers.RedirectResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.RedirectResponse redirect =
                        (where, why) =>
                        {
                            if (onRedirect.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(EastFive.Api.Controllers.RedirectResponse),
                                    thisAgain, requestAgain, paramInfo, onSuccess);
                            return new AttachedHttpResponseMessage<TResult>(onRedirect(where, why));
                        };
                    return onSuccess(redirect);
                });
        }

        private static void AlreadyExistsResponse<TResource, TResult>(this ITestApplication application,
            Func<TResult> onAlreadyExists)
        {
            if (!onAlreadyExists.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(EastFive.Api.Controllers.AlreadyExistsResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        EastFive.Api.Controllers.AlreadyExistsResponse exists =
                            () =>
                            {
                                if (onAlreadyExists.IsDefaultOrNull())
                                    return FailureToOverride<TResource>(
                                        typeof(EastFive.Api.Controllers.AlreadyExistsResponse),
                                        thisAgain, requestAgain, paramInfo, onSuccess);
                                return new AttachedHttpResponseMessage<TResult>(onAlreadyExists());
                            };
                        return onSuccess(exists);
                    });
        }


        private static void NotImplementedResponse<TResource, TResult>(this ITestApplication application,
            Func<TResult> onNotImplemented)
        {
            application.SetInstigator(
                typeof(EastFive.Api.Controllers.NotImplementedResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.NotImplementedResponse notImplemented =
                        () =>
                        {
                            if (onNotImplemented.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(EastFive.Api.Controllers.NotImplementedResponse),
                                    thisAgain, requestAgain, paramInfo, onSuccess);
                            return new AttachedHttpResponseMessage<TResult>(onNotImplemented());
                        };
                    return onSuccess(notImplemented);
                });
        }

        private static void MultipartContentResponse<TResource, TResult>(this ITestApplication application,
            Func<TResource[], TResult> onContents)
        {
            application.SetInstigator(
                typeof(EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync created =
                        (contents) =>
                        {
                            var resources = contents.Cast<TResource>().ToArray();
                            // TODO: try catch
                            //if (!(content is TResource))
                            //    Assert.Fail($"Could not cast {content.GetType().FullName} to {typeof(TResource).FullName}.");

                            if (onContents.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync), 
                                    thisAgain, requestAgain, paramInfo, onSuccess).AsTask();
                            var result = onContents(resources);
                            return new AttachedHttpResponseMessage<TResult>(result).ToTask<HttpResponseMessage>();
                        };
                    return onSuccess(created);
                });

            application.SetInstigatorGeneric(
                typeof(EastFive.Api.Controllers.MultipartResponseAsync<>),
                (type, thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    var scope = new CallbackWrapper<TResource, TResult>(onContents, null, thisAgain, requestAgain, paramInfo, onSuccess);
                    var multipartResponseMethodInfoGeneric = typeof(CallbackWrapper<TResource, TResult>).GetMethod("MultipartResponseAsyncGeneric", BindingFlags.Public | BindingFlags.Instance);
                    var multipartResponseMethodInfoBound = multipartResponseMethodInfoGeneric; // multipartResponseMethodInfoGeneric.MakeGenericMethod(type.GenericTypeArguments);
                    var dele = Delegate.CreateDelegate(type, scope, multipartResponseMethodInfoBound);
                    return onSuccess((object)dele);
                });
        }

        private static void MultipartContentObjectResponse<TResource, TResult>(this ITestApplication application,
            Func<object[], TResult> onContents)
        {
            application.SetInstigator(
                typeof(EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync created =
                        (contents) =>
                        {
                            var resources = contents.ToArray();
                            // TODO: try catch
                            //if (!(content is TResource))
                            //    Assert.Fail($"Could not cast {content.GetType().FullName} to {typeof(TResource).FullName}.");

                            if (onContents.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync),
                                    thisAgain, requestAgain, paramInfo, onSuccess).AsTask();
                            var result = onContents(resources);
                            return new AttachedHttpResponseMessage<TResult>(result).ToTask<HttpResponseMessage>();
                        };
                    return onSuccess(created);
                });

            application.SetInstigatorGeneric(
                typeof(EastFive.Api.Controllers.MultipartResponseAsync<>),
                (type, thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    var callbackWrapperInstance = typeof(CallbackWrapper<,>).MakeGenericType(
                        new Type[] { type.GenericTypeArguments.First(), typeof(TResult) });
                    //var scope = new CallbackWrapper<TResource, TResult>(null, onContents, thisAgain, requestAgain, paramInfo, onSuccess);
                    var scope = Activator.CreateInstance(callbackWrapperInstance, 
                        new object[] { null, onContents, thisAgain, requestAgain, paramInfo, onSuccess });
                    var multipartResponseMethodInfoGeneric = callbackWrapperInstance.GetMethod("MultipartResponseAsyncGeneric", BindingFlags.Public | BindingFlags.Instance);
                    var multipartResponseMethodInfoBound = multipartResponseMethodInfoGeneric; // multipartResponseMethodInfoGeneric.MakeGenericMethod(type.GenericTypeArguments);
                    var dele = Delegate.CreateDelegate(type, scope, multipartResponseMethodInfoBound);
                    return onSuccess((object)dele);
                });
        }

        public class CallbackWrapper<TResource, TResult>
        {
            private Func<TResource[], TResult> callback;
            private Func<object[], TResult> callbackObjs;
            private HttpApplication thisAgain;
            private HttpRequestMessage requestAgain;
            private ParameterInfo paramInfo;
            private Func<object, Task<HttpResponseMessage>> onSuccess;
            
            public CallbackWrapper(Func<TResource[], TResult> onContents, Func<object[], TResult> callbackObjs,
                HttpApplication thisAgain, HttpRequestMessage requestAgain, ParameterInfo paramInfo,
                Func<object, Task<HttpResponseMessage>> onSuccess)
            {
                this.callback = onContents;
                this.callbackObjs = callbackObjs;
                this.thisAgain = thisAgain;
                this.requestAgain = requestAgain;
                this.paramInfo = paramInfo;
                this.onSuccess = onSuccess;
            }

            public async Task<HttpResponseMessage> MultipartResponseAsyncGeneric(IEnumerableAsync<TResource> resources)
            {
                if (!callback.IsDefaultOrNull())
                {
                    var resourcesArray = await resources.ToArrayAsync();
                    var result = callback(resourcesArray);
                    return new AttachedHttpResponseMessage<TResult>(result);
                }
                if (!callbackObjs.IsDefaultOrNull())
                {
                    var resourcesArray = await resources.ToArrayAsync();
                    var result = callbackObjs(resourcesArray.Cast<object>().ToArray());
                    return new AttachedHttpResponseMessage<TResult>(result);
                }
                return FailureToOverride<TResource>(typeof(EastFive.Api.Controllers.MultipartResponseAsync<>), 
                    thisAgain, requestAgain, paramInfo, onSuccess);
            }
            
        }


        private static void NoContentResponse<TResource, TResult>(this ITestApplication application,
            Func<TResult> onNoContent)
        {
            application.SetInstigator(
                typeof(NoContentResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    NoContentResponse created =
                        () =>
                        {
                            if (onNoContent.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(NoContentResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onNoContent();
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }

        private static void NotFoundResponse<TResource, TResult>(this ITestApplication application,
            Func<TResult> onNotFound)
        {
            application.SetInstigator(
                typeof(NotFoundResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    NotFoundResponse notFound =
                        () =>
                        {
                            if (onNotFound.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(NotFoundResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onNotFound();
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(notFound);
                });
        }

        private static void UnauthorizedResponse<TResource, TResult>(this ITestApplication application,
            Func<TResult> onUnauthorized)
        {
            application.SetInstigator(
                typeof(UnauthorizedResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    UnauthorizedResponse created =
                        () =>
                        {
                            if (onUnauthorized.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(UnauthorizedResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onUnauthorized();
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }

        private static void GeneralConflictResponse<TResource, TResult>(this ITestApplication application,
            Func<string, TResult> onGeneralConflictResponse)
        {
            application.SetInstigator(
                typeof(GeneralConflictResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    GeneralConflictResponse created =
                        (reason) =>
                        {
                            if (onGeneralConflictResponse.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(GeneralConflictResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onGeneralConflictResponse(reason);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
               });
        }

        private static void ExecuteBackgroundResponse<TResource, TResult>(this ITestApplication application,
            Func<IExecuteAsync, Task<TResult>> onExecuteBackgroundResponse)
        {
            application.SetInstigator(
                typeof(ExecuteBackgroundResponseAsync),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    ExecuteBackgroundResponseAsync created =
                        async (executionContent) =>
                        {
                            if (onExecuteBackgroundResponse.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(ExecuteBackgroundResponseAsync), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = await onExecuteBackgroundResponse(executionContent);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }


        private static HttpResponseMessage FailureToOverride<TResource>(Type typeOfResponse,
            HttpApplication application,
            HttpRequestMessage request, ParameterInfo paramInfo,
            Func<object, Task<HttpResponseMessage>> onSuccess)
        {
            return new NoOverrideHttpResponseMessage<TResource>(paramInfo.ParameterType, request);
        }

        #region Depricated

        private static EastFive.Api.Controllers.ContentResponse ContentResponse<TResource, TResult>(this Func<HttpResponseMessage, TResource, TResult> onContent, HttpRequestMessage request)
            where TResource : class
        {
            return (content, contentType) =>
            {
                var response = request.CreateResponse(System.Net.HttpStatusCode.OK);
                var attached = new AttachedHttpResponseMessage<TResult>(
                    onContent(response, content as TResource),
                    response);
                return attached;
            };
        }

        private static EastFive.Api.Controllers.NotFoundResponse NotFoundResponse<TResult>(this Func<HttpResponseMessage, TResult> onNotFound, HttpRequestMessage request)
        {
            return () =>
            {
                var response = request.CreateResponse(System.Net.HttpStatusCode.NotFound);
                var attached = new AttachedHttpResponseMessage<TResult>(
                    onNotFound(response),
                    response);
                return attached;
            };
        }

        private static EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync MultipartAcceptArrayResponseAsync<TResource, TResult>(
            this Func<HttpResponseMessage, TResource[], TResult> onResponse, HttpRequestMessage request)
        {
            return (IEnumerable<object> objects) =>
            {
                var response = request.CreateResponse(System.Net.HttpStatusCode.OK);
                var result = onResponse(response,
                    objects.Cast<TResource>().ToArray());
                var responseInnerAttached = new AttachedHttpResponseMessage<TResult>(
                    result, response);
                var responseTask = responseInnerAttached.ToTask<HttpResponseMessage>();
                return responseTask;
            };
        }

        private static EastFive.Api.Controllers.ReferencedDocumentNotFoundResponse MultipartReferencedNotFoundResponse<TResult>(
            this Func<HttpResponseMessage, TResult> onResponse, HttpRequestMessage request)
        {
            return () =>
            {
                var response = request.CreateResponse(System.Net.HttpStatusCode.OK);
                var result = onResponse(response);
                return new AttachedHttpResponseMessage<TResult>(
                    result, response);
            };
        }

        private static Controllers.CreatedResponse CreatedResponse<TResult>(this Func<TResult> onCreated, HttpRequestMessage request)
        {
            return () =>
            {
                var attached = new AttachedHttpResponseMessage<TResult>(
                onCreated(),
                request.CreateResponse(System.Net.HttpStatusCode.Created));
                return attached;
            };
        }

        #endregion

        #endregion

        #region Depricated invocation via context object

        private static HttpRequestMessage GetRequest(this ITestApplication application, HttpMethod method)
        {
            var hostingLocation = Web.Configuration.Settings.GetUri(
                    AppSettings.ServerUrl,
                (hostingLocationFound) => hostingLocationFound,
                (whyUnspecifiedOrInvalid) => new Uri("http://example.com"));
            var httpRequest = new HttpRequestMessage(method, hostingLocation);

            var config = new HttpConfiguration();

            var routesApi = EastFive.Web.Configuration.Settings.GetString(
                    EastFive.Api.Tests.AppSettings.RoutesApi,
                (routesApiFound) => routesApiFound,
                (why) => "DefaultApi");

            var routesMvc = EastFive.Web.Configuration.Settings.GetString(
                    EastFive.Api.Tests.AppSettings.RoutesMvc,
                (routesApiFound) => routesApiFound,
                (why) => "Default");

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

            foreach (var headerKVP in application.Headers)
                httpRequest.Headers.Add(headerKVP.Key, headerKVP.Value);

            return httpRequest;
        }

        [Obsolete]
        public struct RequestContext
        {
            public HttpRequestMessage request;
            public UrlHelper urlHelper;
            public Controllers.Security security;
            public Controllers.AlreadyExistsResponse onAlreadyExists;
            public Controllers.ReferencedDocumentNotFoundResponse onReferencedDocumentNotFound;
            public Controllers.AlreadyExistsReferencedResponse onRelationshipAlreadyExists;
            public Controllers.UnauthorizedResponse onUnauthorized;
            public Controllers.GeneralConflictResponse onGeneralConflict;

        }

        [Obsolete]
        public static Task<TResult> GetRequestContext<TResult>(this ITestApplication application, HttpMethod method,
            Func<HttpRequestMessage, RequestContext, Task<HttpResponseMessage>> callback)
        {
            var request = application.GetRequest(method);
            return Web.Configuration.Settings.GetString(
                Api.AppSettings.ActorIdClaimType,
                async (actorIdClaimType) =>
                {
                    var requestContext = new RequestContext
                    {
                        request = request,
                        urlHelper = new UrlHelper(request),
                        security = new Controllers.Security()
                        {
                            performingAsActorId = application.ActorId,
                            claims = new System.Security.Claims.Claim[]
                            {
                                new System.Security.Claims.Claim(actorIdClaimType, application.ActorId.ToString()),
                            }
                        },
                    };

                    var response = await callback(request, requestContext);
                    var attachedResponse = response as IReturnResult;
                    var result = attachedResponse.GetResultCasted<TResult>();
                    return result;
                },
                (why) =>
                {
                    Assert.Fail(why);
                    throw new Exception(why);
                });
        }

        [Obsolete]
        public static Task<TResult> PostAsync<TResult>(this ITestApplication application,
                Func<RequestContext,
                    Controllers.CreatedResponse,
                    Task<HttpResponseMessage>> operation,
                Func<TResult> onCreated)
        {
            return application.GetRequestContext<TResult>(HttpMethod.Post,
                (request, context) => operation(
                        context,
                        onCreated.CreatedResponse(request)));
        }


        [Obsolete]
        public static Task<TResult> GetByRelatedAsync<TResource, TResult>(this ITestApplication application,
                Func<RequestContext,
                    Controllers.MultipartAcceptArrayResponseAsync,
                    Controllers.ReferencedDocumentNotFoundResponse,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse,
                Func<HttpResponseMessage, TResult> onReferencedNotFound)
        {
            return application.GetRequestContext<TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.MultipartAcceptArrayResponseAsync(request),
                    onReferencedNotFound.MultipartReferencedNotFoundResponse(request)));
        }


        [Obsolete]
        public static Task<TResult> GetAllAsync<TResource, TResult>(this ITestApplication application,
                Func<RequestContext,
                    Controllers.MultipartAcceptArrayResponseAsync,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse)
        {
            return application.GetRequestContext<TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.MultipartAcceptArrayResponseAsync(request)));
        }
        
        #endregion

        #region Depricated complex direct invocation

        //public static Task<TResult> PostAsync<TApplication, T1, T2, T3, TResource, TRefDoc, TResult>(
        //    this ITestApplication application,
        //        Func<
        //            T1, T2, T3,
        //            TApplication,
        //            System.Web.Http.Routing.UrlHelper,
        //            EastFive.Api.Controllers.CreatedResponse,
        //            EastFive.Api.Controllers.BadRequestResponse,
        //            EastFive.Api.Controllers.AlreadyExistsResponse,
        //            EastFive.Api.Controllers.ReferencedDocumentDoesNotExistsResponse<TRefDoc>,
        //            Task<HttpResponseMessage>> operation,
        //        T1 value1, T2 value2, T3 value3,
        //    Func<TResource, TResult> onCreated = default(Func<TResource, TResult>),
        //    Func<TResult> onBadRequest = default(Func<TResult>),
        //    Func<TResult> onExists = default(Func<TResult>),
        //    Func<TRefDoc, TResult> onRefDoesNotExists = default(Func<TRefDoc, TResult>))
        //    where TApplication : EastFive.Api.Azure.AzureApplication
        //{
        //    return GetRequestContext<TResult>(application, HttpMethod.Post,
        //        (request, context) =>
        //        {
        //            var resource = Activator.CreateInstance<TResource>();
        //            var properties = typeof(TResource)
        //                .GetProperties()
        //                .Select(propInfo => propInfo.GetCustomAttribute<JsonPropertyAttribute, KeyValuePair<string, PropertyInfo>?>(
        //                    (jsonPropAttr) => jsonPropAttr.PropertyName.PairWithValue(propInfo),
        //                    () => default(KeyValuePair<string, PropertyInfo>?)))
        //                .SelectWhereHasValue()
        //                .ToDictionary();
        //            var parameters = operation
        //                .Method
        //                .GetParameters()
        //                .Select(param => param.GetCustomAttribute<EastFive.Api.QueryValidationAttribute, KeyValuePair<string, ParameterInfo>?>(
        //                    (attr) => param.PairWithKey(attr.Name),
        //                    () => default(KeyValuePair<string, ParameterInfo>?)))
        //                .SelectWhereHasValue()
        //                .ToDictionary();
        //            var stackTrace = new System.Diagnostics.StackTrace();
        //            //stackTrace.GetFrame(0).GetMethod().Para
        //            var values = new object[] { value1, value2, value3 };

        //            var azureApplication = application as TApplication;

        //            var dictionary = properties
        //                .IntersectKeys(parameters,
        //                    (propInfo, paramInfo) =>
        //                    {
        //                        var value = values[paramInfo.Position];
        //                        var valueConverted = application.CastResourceProperty(value, propInfo.PropertyType);
        //                        return valueConverted.PairWithKey(propInfo);
        //                    })
        //                .SelectValues()
        //                .ToDictionary();

        //            resource.PopulateType(dictionary);

        //            return operation(value1, value2, value3, azureApplication, context.urlHelper,
        //                CreatedResponse(onCreated, resource, request),
        //                BadRequestResponse(onBadRequest, request),
        //                AlreadyExistsResponse(onExists, request),
        //                ReferencedDocumentDoesNotExistsResponse<TRefDoc, TResult>(() => onRefDoesNotExists(default(TRefDoc)), request));
        //        });
        //}

        //private static Controllers.CreatedResponse CreatedResponse<TResource, TResult>(this Func<TResource, TResult> onCreated, HttpRequestMessage request,
        //    Expression<Func<RequestContext,
        //            Controllers.CreatedResponse,
        //            Task<HttpResponseMessage>>> operation)
        //{
        //    return () =>
        //    {
        //        var resource = Activator.CreateInstance<TResource>();
        //        var body = operation.Body;
        //        var methodCall = body as MethodCallExpression;

        //        var propertyLookup = typeof(TResource)
        //            .GetProperties()
        //            .Where(prop => prop.ContainsCustomAttribute<JsonPropertyAttribute>())
        //            .Select(prop => prop.PairWithKey(prop.GetCustomAttribute(
        //                (JsonPropertyAttribute attribute) => attribute.PropertyName,
        //                () => string.Empty)))
        //            .ToDictionary();

        //        var parameters = methodCall.Method.GetParameters();
        //        var parameterLookup = parameters
        //            .Select(paramInfo => paramInfo.GetCustomAttribute<QueryValidationAttribute, KeyValuePair<string, string>?>(
        //                qvAttr => qvAttr.Name.PairWithKey(paramInfo.Name),
        //                () => default(KeyValuePair<string, string>?)))
        //            .SelectWhereHasValue()
        //            .ToDictionary();

        //        var arguments = methodCall.Arguments
        //            .Zip(parameters, (arg, param) => arg.PairWithValue(param))
        //            .Where(argParam => parameterLookup.ContainsKey(argParam.Value.Name))
        //            .Where(argParam => propertyLookup.ContainsKey(parameterLookup[argParam.Value.Name]))
        //            .Select(
        //                argParam =>
        //                {
        //                    var argument = argParam.Key;
        //                    var value = ResolveMemberExpression(argument);
        //                    var type = argument.Type;
        //                    var propInfo = propertyLookup[parameterLookup[argParam.Value.Name]];
        //                    propInfo.SetValue(resource, value);

        //                    return value;
        //                })
        //            .ToArray();

        //        var attached = new AttachedHttpResponseMessage<TResult>(
        //            onCreated(resource),
        //            request.CreateResponse(HttpStatusCode.Created));
        //        return attached;
        //    };
        //}



        #endregion

    }
}
