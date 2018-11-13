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
        public static TResult CastResourceProperty<TResult>(this ITestApplication application, object value, Type propertyType,
            Func<object, TResult> onCasted,
            Func<TResult> onNotMapped = default(Func<TResult>))
        {
            var valueType = value.GetType();
            if (propertyType.IsAssignableFrom(valueType))
                return onCasted(value);

            if (propertyType.IsAssignableFrom(typeof(BlackBarLabs.Api.Resources.WebId)))
            {
                if (value is Guid)
                {
                    var guidValue = (Guid)value;
                    var webIdValue = (BlackBarLabs.Api.Resources.WebId)guidValue;
                    return onCasted(webIdValue);
                }
            }

            if (propertyType.IsAssignableFrom(typeof(string)))
            {
                if (value is Guid)
                {
                    var guidValue = (Guid)value;
                    var stringValue = guidValue.ToString();
                    return onCasted(stringValue);
                }
                if (value is BlackBarLabs.Api.Resources.WebId)
                {
                    var webIdValue = value as BlackBarLabs.Api.Resources.WebId;
                    var guidValue = webIdValue.ToGuid().Value;
                    var stringValue = guidValue.ToString();
                    return onCasted(stringValue);
                }
                if (value is bool)
                {
                    var boolValue = (bool)value;
                    var stringValue = boolValue.ToString();
                    return onCasted(stringValue);
                }
            }

            if(onNotMapped.IsDefaultOrNull())
                throw new Exception($"Cannot create {propertyType.FullName} from {value.GetType().FullName}");
            return onNotMapped();
        }

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

            IHttpRoute [] mvcRoutes = routesMvcCSV.Split(new[] { ',' }).Select(
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
                    
                    var response = await EastFive.Api.Modules.ControllerHandler.DirectSendAsync(application as EastFive.Api.HttpApplication, request, default(CancellationToken),
                        (requestBack, token) =>
                        {
                            Assert.Fail($"Failed to invoke {fvcAttr.Route}");
                            throw new Exception();
                        });

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

        public static void AssignQueryValue<T>(this T param, T value)
        {

        }

        private static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
                Expression<Action<TResource>>[] parameters,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefNotFoundType = default(Func<Type, TResult>))
        {
            application.ContentResponse(onContent);
            application.MultipartContentResponse(onContents);
            application.BadRequestResponse(onBadRequest);
            application.RefNotFoundTypeResponse(onRefNotFoundType);
            return application.MethodAsync<TResource, TResult, TResult>(HttpMethod.Get,
                (request) =>
                {
                    var queryParams = parameters
                        .Select(param => param.GetAssignment(
                            (propInfo, value) =>
                                propInfo.GetCustomAttribute<JsonPropertyAttribute, string>(
                                            jsonAttr => jsonAttr.PropertyName,
                                            () => propInfo.Name)
                                .PairWithValue((string)application.CastResourceProperty(value, typeof(String)))))
                        .ToDictionary();

                    request.RequestUri = request.RequestUri.SetQuery(queryParams);
                    return request;
                },
                (TResult result) =>
                {
                    return result;
                });

        }

        public static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefNotFoundType = default(Func<Type, TResult>))
        {
            return application.GetAsync(new Expression<Action<TResource>>[] {  },
                onContent: onContent,
                onContents: onContents,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefNotFoundType: onRefNotFoundType);
        }


        public static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
                Expression<Action<TResource>> param1,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefNotFoundType = default(Func<Type, TResult>))
        {
            return application.GetAsync(new[] { param1 },
                onContent: onContent,
                onContents: onContents,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefNotFoundType: onRefNotFoundType);
        }

        public static Task<TResult> GetAsync<TResource, TResult>(this ITestApplication application,
                Expression<Action<TResource>> param1,
                Expression<Action<TResource>> param2,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefNotFoundType = default(Func<Type, TResult>))
        {
            return application.GetAsync(
                    new Expression<Action<TResource>>[] { param1, param2 },
                onContent: onContent,
                onContents: onContents,
                onBadRequest: onBadRequest,
                onNotFound: onNotFound,
                onRefNotFoundType: onRefNotFoundType);
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
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<TResult> onNotImplemented = default(Func<TResult>))
        {
            application.CreatedResponse(onCreated);
            application.BadRequestResponse(onBadRequest);
            application.AlreadyExistsResponse(onExists);
            application.RefNotFoundTypeResponse(onRefDoesNotExistsType);

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
                });
        }

        public static Task<TResult> PatchAsync<TResource, TResult>(this ITestApplication application,
                TResource resource,
            Func<TResult> onUpdated = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<TResult> onUnauthorized = default(Func<TResult>),
            Func<string, TResult> onFailure = default(Func<string, TResult>))
        {
            application.NoContentResponse(onUpdated);
            application.NotFoundResponse(onNotFound);
            application.UnauthorizedResponse(onUnauthorized);
            application.GeneralConflictResponse(onFailure);
            return application.MethodAsync<TResource, TResult, TResult>(new HttpMethod("patch"),
                (request) =>
                {
                    request.Content = new StreamContent(JsonConvert.SerializeObject(resource).ToStream());
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    return request;
                },
                (TResult result) =>
                {
                    return result;
                });
        }
        
        #region Response types

        private interface IReturnResult
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
        
        private static void ContentResponse<TResource, TResult>(this ITestApplication application,
            Func<TResource, TResult> onContent)
        {
            if (!onContent.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(EastFive.Api.Controllers.ContentResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        EastFive.Api.Controllers.ContentResponse created =
                            (content, mimeType) =>
                            {
                                if (!(content is TResource))
                                    Assert.Fail($"Could not cast {content.GetType().FullName} to {typeof(TResource).FullName}.");
                                var resource = (TResource)content;
                                var result = onContent(resource);
                                return new AttachedHttpResponseMessage<TResult>(result);
                            };
                        return onSuccess(created);
                    });
        }

        private static void BadRequestResponse<TResult>(this ITestApplication application,
            Func<TResult> onBadRequest)
        {
            if (!onBadRequest.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(EastFive.Api.Controllers.BadRequestResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        EastFive.Api.Controllers.BadRequestResponse badRequest =
                            () =>
                            {
                                var result = onBadRequest();
                                return new AttachedHttpResponseMessage<TResult>(result);
                            };
                        return onSuccess(badRequest);
                    });
        }
        
        private static void RefNotFoundTypeResponse<TResult>(this ITestApplication application,
            Func<Type, TResult> referencedDocDoesNotExists)
        {
            if (referencedDocDoesNotExists.IsDefaultOrNull())
                return;

            application.SetInstigatorGeneric(
                typeof(ReferencedDocumentDoesNotExistsResponse<>),
                (type, thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    var scope = new CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>(referencedDocDoesNotExists);
                    var multipartResponseMethodInfoGeneric = typeof(CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>)
                        .GetMethod("RefNotFoundTypeResponseGeneric", BindingFlags.Public | BindingFlags.Instance);
                    var multipartResponseMethodInfoBound = multipartResponseMethodInfoGeneric
                        .MakeGenericMethod(type.GenericTypeArguments);
                    var dele = Delegate.CreateDelegate(type, scope, multipartResponseMethodInfoBound);
                    return onSuccess((object)dele);
                });
        }

        private static void CreatedResponse<TResult>(this ITestApplication application,
            Func<TResult> onCreated)
        {
            if (onCreated.IsDefaultOrNull())
                return;

            application.SetInstigator(
                typeof(EastFive.Api.Controllers.CreatedResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.CreatedResponse created = () => new AttachedHttpResponseMessage<TResult>(onCreated());
                    return onSuccess(created);
                });
            
        }
        
        private static void AlreadyExistsResponse<TResult>(this ITestApplication application,
            Func<TResult> onAlreadyExists)
        {
            if (onAlreadyExists.IsDefaultOrNull())
                return;
            
            if (!onAlreadyExists.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(EastFive.Api.Controllers.AlreadyExistsResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        EastFive.Api.Controllers.AlreadyExistsResponse exists = () => new AttachedHttpResponseMessage<TResult>(onAlreadyExists());
                        return onSuccess(exists);
                    });
        }


        private static void NotImplementedResponse<TResult>(this ITestApplication application,
            Func<TResult> onNotImplemented)
        {
            if (onNotImplemented.IsDefaultOrNull())
                return;

            application.SetInstigator(
                typeof(EastFive.Api.Controllers.NotImplementedResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.Controllers.NotImplementedResponse notImplemented = () => new AttachedHttpResponseMessage<TResult>(onNotImplemented());
                    return onSuccess(notImplemented);
                });
        }

        public class CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>
        {
            private Func<Type, TResult> callback;

            public CallbackWrapperReferencedDocumentDoesNotExistsResponse(Func<Type, TResult> onContents)
            {
                this.callback = onContents;
            }

            public HttpResponseMessage RefNotFoundTypeResponseGeneric<TResource>()
            {
                var result = callback(typeof(TResource));
                return new AttachedHttpResponseMessage<TResult>(result);
            }
        }

        private static void RefNotFoundTypeResponseGeneric<T>()
        {

        }

        private static void MultipartContentResponse<TResource, TResult>(this ITestApplication application,
            Func<TResource[], TResult> onContents)
        {
            if (onContents.IsDefaultOrNull())
                return;

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
                                var result = onContents(resources);
                                return new AttachedHttpResponseMessage<TResult>(result).ToTask<HttpResponseMessage>();
                            };
                    return onSuccess(created);
                });

            application.SetInstigatorGeneric(
                typeof(EastFive.Api.Controllers.MultipartResponseAsync<>),
                (type, thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    var scope = new CallbackWrapper<TResource, TResult>(onContents);
                    var multipartResponseMethodInfoGeneric = typeof(CallbackWrapper<TResource, TResult>).GetMethod("MultipartResponseAsyncGeneric", BindingFlags.Public | BindingFlags.Instance);
                    var multipartResponseMethodInfoBound = multipartResponseMethodInfoGeneric; // multipartResponseMethodInfoGeneric.MakeGenericMethod(type.GenericTypeArguments);
                    var dele = Delegate.CreateDelegate(type, scope, multipartResponseMethodInfoBound);
                    return onSuccess((object)dele);
                });
        }

        public class CallbackWrapper<TResource, TResult>
        {
            private Func<TResource[], TResult> callback;

            public CallbackWrapper(Func<TResource[], TResult> onContents)
            {
                this.callback = onContents;
            }
            
            public async Task<HttpResponseMessage> MultipartResponseAsyncGeneric(IEnumerableAsync<TResource> resources)
            {
                // TODO: try catch
                //if (!(content is TResource))
                //    Assert.Fail($"Could not cast {content.GetType().FullName} to {typeof(TResource).FullName}.");
                var resourcesArray = await resources.ToArrayAsync();
                var result = callback(resourcesArray);
                return new AttachedHttpResponseMessage<TResult>(result);
            }
            
        }
        
        private static void NoContentResponse<TResult>(this ITestApplication application,
            Func<TResult> onNoContent)
        {
            if (!onNoContent.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(NoContentResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        NoContentResponse created =
                            () =>
                            {
                                var result = onNoContent();
                                return new AttachedHttpResponseMessage<TResult>(result);
                            };
                        return onSuccess(created);
                    });
        }

        private static void NotFoundResponse<TResult>(this ITestApplication application,
            Func<TResult> onNotFound)
        {
            if (!onNotFound.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(NotFoundResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        NotFoundResponse created =
                            () =>
                            {
                                var result = onNotFound();
                                return new AttachedHttpResponseMessage<TResult>(result);
                            };
                        return onSuccess(created);
                    });
        }

        private static void UnauthorizedResponse<TResult>(this ITestApplication application,
            Func<TResult> onUnauthorized)
        {
            if (!onUnauthorized.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(UnauthorizedResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        UnauthorizedResponse created =
                            () =>
                            {
                                var result = onUnauthorized();
                                return new AttachedHttpResponseMessage<TResult>(result);
                            };
                        return onSuccess(created);
                    });
        }

        private static void GeneralConflictResponse<TResult>(this ITestApplication application,
            Func<string, TResult> onGeneralConflictResponse)
        {
            if (!onGeneralConflictResponse.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(GeneralConflictResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        GeneralConflictResponse created =
                            (reason) =>
                            {
                                var result = onGeneralConflictResponse(reason);
                                return new AttachedHttpResponseMessage<TResult>(result);
                            };
                        return onSuccess(created);
                    });
        }
        
        #region Depricated

        private static EastFive.Api.Controllers.CreatedResponse CreatedResponse<TResource, TResult>(Func<TResource, TResult> onCreated, TResource resource, HttpRequestMessage request)
        {
            return () =>
            {
                var result = onCreated(resource);
                var attached = new AttachedHttpResponseMessage<TResult>(
                    result,
                    request.CreateResponse(System.Net.HttpStatusCode.Created));
                return attached;
            };
        }

        private static EastFive.Api.Controllers.BadRequestResponse BadRequestResponse<TResult>(Func<TResult> onBadRequest, HttpRequestMessage request)
        {
            return () => new AttachedHttpResponseMessage<TResult>(
                    onBadRequest(),
                    request.CreateResponse(System.Net.HttpStatusCode.BadRequest));
        }

        private static EastFive.Api.Controllers.AlreadyExistsResponse AlreadyExistsResponse<TResult>(this Func<TResult> onAlreadyExists, HttpRequestMessage request)
        {
            return () => new AttachedHttpResponseMessage<TResult>(
                    onAlreadyExists(),
                    request.CreateResponse(System.Net.HttpStatusCode.Conflict));
        }

        private static EastFive.Api.Controllers.ReferencedDocumentDoesNotExistsResponse<TRef> ReferencedDocumentDoesNotExistsResponse<TRef, TResult>(
            this Func<TResult> onReferenedDoesNotExists, HttpRequestMessage request)
        {
            return () => new AttachedHttpResponseMessage<TResult>(
                    onReferenedDoesNotExists(),
                    request.CreateResponse(System.Net.HttpStatusCode.BadRequest));
        }

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
        public static Task<TResult> GetMultipartSpecifiedAsync<TResource, TResult>(this ITestApplication application,
                Func<RequestContext,
                    EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse)
        {
            return application.GetRequestContext<TResult>(HttpMethod.Get,
                (request, context) => operation(context, onResponse.MultipartAcceptArrayResponseAsync(request)));
        }


        [Obsolete]
        public static Task<TResult> GetByIdAsync<TResource, TApplication, TResult>(this ITestApplication application,
                Func<RequestContext,
                    Controllers.ContentResponse,
                    Controllers.NotFoundResponse,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource, TResult> onResponse,
                Func<HttpResponseMessage, TResult> onNotFound)
            where TApplication : EastFive.Api.Azure.AzureApplication
            where TResource : class
        {
            return application.GetRequestContext<TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.ContentResponse(request),
                    onNotFound.NotFoundResponse(request)));
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
