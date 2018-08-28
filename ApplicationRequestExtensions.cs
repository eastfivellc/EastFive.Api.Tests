using BlackBarLabs.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

namespace EastFive.Api.Tests
{
    public static class ApplicationRequestExtensions
    {
        public struct RequestContext
        {
            public HttpRequestMessage request;
            public UrlHelper urlHelper;
            public Controllers.Security security;
        }

        private static HttpRequestMessage GetRequest<TApplication>(this TApplication application, HttpMethod method)
            where TApplication : ITestApplication
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

        private class AttachedHttpResponseMessage<TResult> : HttpResponseMessage
        {
            public TResult Result { get; }
            public HttpResponseMessage Inner { get; }

            public AttachedHttpResponseMessage(TResult result, HttpResponseMessage inner)
            {
                this.Result = result;
                this.Inner = inner;
            }
        }

        #region Response types

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

        private static Controllers.AlreadyExistsResponse AlreadyExistsResponse<TResult>(this Func<TResult> onAlreadyExists, HttpRequestMessage request)
        {
            return () => new AttachedHttpResponseMessage<TResult>(
                    onAlreadyExists(),
                    request.CreateResponse(System.Net.HttpStatusCode.Conflict));
        }

        private static Controllers.ReferencedDocumentDoesNotExistsResponse ReferencedDocumentDoesNotExistsResponse<TResult>(this Func<TResult> onReferenedDoesNotExists, HttpRequestMessage request)
        {
            return () => new AttachedHttpResponseMessage<TResult>(
                    onReferenedDoesNotExists(),
                    request.CreateResponse(System.Net.HttpStatusCode.BadRequest));
        }

        private static Controllers.ContentResponse ContentResponse<TResource, TResult>(this Func<HttpResponseMessage, TResource, TResult> onContent, HttpRequestMessage request)
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

        private static Controllers.NotFoundResponse NotFoundResponse<TResult>(this Func<HttpResponseMessage, TResult> onNotFound, HttpRequestMessage request)
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

        private static Controllers.MultipartAcceptArrayResponseAsync MultipartAcceptArrayResponseAsync<TResource, TResult>(
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

        private static Controllers.ReferencedDocumentNotFoundResponse MultipartReferencedNotFoundResponse<TResult>(
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

        #endregion

        private static Task<TResult> GetRequestContext<TApplication, TResult>(this TApplication application, HttpMethod method,
            Func<HttpRequestMessage, RequestContext, Task<HttpResponseMessage>> callback)
            where TApplication : ITestApplication
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
                    var attachedResponse = response as AttachedHttpResponseMessage<TResult>;
                    return attachedResponse.Result;
                },
                (why) =>
                {
                    Assert.Fail(why);
                    throw new Exception(why);
                });
        }

        public static Task<TResult> PostDependentAsync<TApplication, TResult>(this TApplication application,
                Func<RequestContext,
                    Controllers.CreatedResponse,
                    Controllers.AlreadyExistsResponse,
                    Controllers.ReferencedDocumentDoesNotExistsResponse,
                    Task<HttpResponseMessage>> operation,
                Func<TResult> onCreated,
                Func<TResult> onAlreadyExists,
                Func<TResult> onReferenedDoesNotExists)
            where TApplication : ITestApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Post,
                (request, context) => operation(
                        context,
                        onCreated.CreatedResponse(request),
                        onAlreadyExists.AlreadyExistsResponse(request),
                        onReferenedDoesNotExists.ReferencedDocumentDoesNotExistsResponse(request)));
        }

        public static Task<TResult> GetMultipartSpecifiedAsync<TResource, TApplication, TResult>(this TApplication application,
                Func<RequestContext,
                    EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse)
            where TApplication : ITestApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Get,
                (request, context) => operation(context, onResponse.MultipartAcceptArrayResponseAsync(request)));
        }

        public static Task<TResult> GetByIdAsync<TResource, TApplication, TResult>(this TApplication application,
                Func<RequestContext,
                    Controllers.ContentResponse,
                    Controllers.NotFoundResponse,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource, TResult> onResponse,
                Func<HttpResponseMessage, TResult> onNotFound)
            where TApplication : ITestApplication
            where TResource : class
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.ContentResponse(request),
                    onNotFound.NotFoundResponse(request)));
        }

        public static Task<TResult> GetByRelatedAsync<TResource, TApplication, TResult>(this TApplication application,
                Func<RequestContext,
                    Controllers.MultipartAcceptArrayResponseAsync,
                    Controllers.ReferencedDocumentNotFoundResponse,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse,
                Func<HttpResponseMessage, TResult> onReferencedNotFound)
            where TApplication : ITestApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.MultipartAcceptArrayResponseAsync(request),
                    onReferencedNotFound.MultipartReferencedNotFoundResponse(request)));
        }

        public static Task<TResult> GetAllAsync<TResource, TApplication, TResult>(this TApplication application,
                Func<RequestContext,
                    Controllers.MultipartAcceptArrayResponseAsync,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse)
            where TApplication : ITestApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.MultipartAcceptArrayResponseAsync(request)));
        }

        public static Task<HttpResponseMessage> PostAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, Task<HttpResponseMessage>> action, object resource, Action<HttpRequestMessage> mutateRequest = null)
        {
            throw new NotImplementedException();
        }
    }
}
