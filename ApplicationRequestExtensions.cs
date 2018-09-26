using BlackBarLabs.Extensions;
using EastFive.Collections.Generic;
using EastFive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

using EastFive;
using EastFive.Reflection;
using EastFive.Extensions;
using EastFive.Api.Controllers;
using BlackBarLabs.Api.Resources;

namespace EastFive.Api.Tests
{
    public static class ApplicationRequestExtensions
    {
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

        private static HttpRequestMessage GetRequest<TApplication>(this ITestApplication<TApplication> application, HttpMethod method)
            where TApplication : EastFive.Api.Azure.AzureApplication
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

        private interface IReturnResult
        {
            TResult GetResultCasted<TResult>();
        }

        private class AttachedHttpResponseMessage<TResult> : HttpResponseMessage, IReturnResult
        {
            public TResult Result { get; }
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

        

        //public static Task<TResult> PostAsync<TApplication, T1, T2, T3, T4, T5, T6, T7, T8, TRefDoc1, TRefDoc2, TResource, TResult>(
        //    this ITestApplication<TApplication> application,
        //        Func<
        //            T1, T2, T3, T4, T5, T6, T7, T8,
        //            EastFive.Api.Controllers.Security, TApplication,
        //            EastFive.Api.Controllers.CreatedResponse,
        //            EastFive.Api.Controllers.BadRequestResponse,
        //            EastFive.Api.Controllers.AlreadyExistsResponse,
        //            EastFive.Api.Controllers.ReferencedDocumentDoesNotExistsResponse<TRefDoc1>,
        //            EastFive.Api.Controllers.ReferencedDocumentDoesNotExistsResponse<TRefDoc2>,
        //            Task<HttpResponseMessage>> operation,
        //        T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8,
        //    Func<TResource, TResult> onCreated = default(Func<TResource, TResult>),
        //    Func<TResult> onBadRequest = default(Func<TResult>),
        //    Func<TResult> onExists = default(Func<TResult>),
        //    Func<TRefDoc1, TResult> onRefDoesNotExists1 = default(Func<TRefDoc1, TResult>),
        //    Func<TRefDoc2, TResult> onRefDoesNotExists2 = default(Func<TRefDoc2, TResult>))
        //    where TApplication : EastFive.Api.Azure.AzureApplication
        //{
        //    return GetRequestContext<TApplication, TResult>(application, HttpMethod.Post,
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

        //            return operation(value1, value2, value3, value4, value5, value6, value7, value8, azureApplication, context.urlHelper,
        //                CreatedResponse(onCreated, resource, request),
        //                BadRequestResponse(onBadRequest, request),
        //                AlreadyExistsResponse(onExists, request),
        //                ReferencedDocumentDoesNotExistsResponse<TRefDoc1, TResult>(() => onRefDoesNotExists1(default(TRefDoc1)), request),
        //                ReferencedDocumentDoesNotExistsResponse<TRefDoc2, TResult>(() => onRefDoesNotExists2(default(TRefDoc2)), request));
        //        });
        //}

        public static Task<TResult> PostAsync<TApplication, T1, T2, T3, TResource, TRefDoc, TResult>(
            this ITestApplication<TApplication> application,
                Func<
                    T1, T2, T3,
                    TApplication,
                    System.Web.Http.Routing.UrlHelper,
                    EastFive.Api.Controllers.CreatedResponse,
                    EastFive.Api.Controllers.BadRequestResponse,
                    EastFive.Api.Controllers.AlreadyExistsResponse,
                    EastFive.Api.Controllers.ReferencedDocumentDoesNotExistsResponse<TRefDoc>,
                    Task<HttpResponseMessage>> operation,
                T1 value1, T2 value2, T3 value3,
            Func<TResource, TResult> onCreated = default(Func<TResource, TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<TRefDoc, TResult> onRefDoesNotExists = default(Func<TRefDoc, TResult>))
            where TApplication : EastFive.Api.Azure.AzureApplication
        {
            return GetRequestContext<TApplication, TResult>(application, HttpMethod.Post,
                (request, context) =>
                {
                    var resource = Activator.CreateInstance<TResource>();
                    var properties = typeof(TResource)
                        .GetProperties()
                        .Select(propInfo => propInfo.GetCustomAttribute<JsonPropertyAttribute, KeyValuePair<string, PropertyInfo>?>(
                            (jsonPropAttr) => jsonPropAttr.PropertyName.PairWithValue(propInfo),
                            () => default(KeyValuePair<string, PropertyInfo>?)))
                        .SelectWhereHasValue()
                        .ToDictionary();
                    var parameters = operation
                        .Method
                        .GetParameters()
                        .Select(param => param.GetCustomAttribute<EastFive.Api.QueryValidationAttribute, KeyValuePair<string, ParameterInfo>?>(
                            (attr) => param.PairWithKey(attr.Name),
                            () => default(KeyValuePair<string, ParameterInfo>?)))
                        .SelectWhereHasValue()
                        .ToDictionary();
                    var stackTrace = new System.Diagnostics.StackTrace();
                    //stackTrace.GetFrame(0).GetMethod().Para
                    var values = new object[] { value1, value2, value3 };

                    var azureApplication = application as TApplication;

                    var dictionary = properties
                        .IntersectKeys(parameters,
                            (propInfo, paramInfo) =>
                            {
                                var value = values[paramInfo.Position];
                                var valueConverted = application.CastResourceProperty(value, propInfo.PropertyType);
                                return valueConverted.PairWithKey(propInfo);
                            })
                        .SelectValues()
                        .ToDictionary();

                    resource.PopulateType(dictionary);

                    return operation(value1, value2, value3, azureApplication, context.urlHelper,
                        CreatedResponse(onCreated, resource, request),
                        BadRequestResponse(onBadRequest, request),
                        AlreadyExistsResponse(onExists, request),
                        ReferencedDocumentDoesNotExistsResponse<TRefDoc, TResult>(() => onRefDoesNotExists(default(TRefDoc)), request));
                });
        }

        #region Response types
        
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

        #endregion

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

        private static Controllers.CreatedResponse CreatedResponse<TResource, TResult>(this Func<TResource, TResult> onCreated, HttpRequestMessage request,
            Expression<Func<RequestContext,
                    Controllers.CreatedResponse,
                    Task<HttpResponseMessage>>> operation)
        {
            return () =>
            {
                var resource = Activator.CreateInstance<TResource>();
                var body = operation.Body;
                var methodCall = body as MethodCallExpression;
                
                var propertyLookup = typeof(TResource)
                    .GetProperties()
                    .Where(prop => prop.ContainsCustomAttribute<JsonPropertyAttribute>())
                    .Select(prop => prop.PairWithKey(prop.GetCustomAttribute(
                        (JsonPropertyAttribute attribute) => attribute.PropertyName,
                        () => string.Empty)))
                    .ToDictionary();

                var parameters = methodCall.Method.GetParameters();
                var parameterLookup = parameters
                    .Select(paramInfo => paramInfo.GetCustomAttribute<QueryValidationAttribute, KeyValuePair<string, string>?>(
                        qvAttr => qvAttr.Name.PairWithKey(paramInfo.Name),
                        () => default(KeyValuePair<string, string>?)))
                    .SelectWhereHasValue()
                    .ToDictionary();

                var arguments = methodCall.Arguments
                    .Zip(parameters, (arg, param) => arg.PairWithValue(param))
                    .Where(argParam => parameterLookup.ContainsKey(argParam.Value.Name))
                    .Where(argParam => propertyLookup.ContainsKey(parameterLookup[argParam.Value.Name]))
                    .Select(
                        argParam =>
                        {
                            var argument = argParam.Key;
                            var value = ResolveMemberExpression(argument);
                            var type = argument.Type;
                            var propInfo = propertyLookup[parameterLookup[argParam.Value.Name]];
                            propInfo.SetValue(resource, value);

                            return value;
                        })
                    .ToArray();

                var attached = new AttachedHttpResponseMessage<TResult>(
                    onCreated(resource),
                    request.CreateResponse(HttpStatusCode.Created));
                return attached;
            };
        }

        private static KeyValuePair<Type, object>[] ResolveArgs<T>(Expression<Func<T, object>> expression)
        {
            var body = (System.Linq.Expressions.MethodCallExpression)expression.Body;
            var values = new List<KeyValuePair<Type, object>>();
            
            return values.ToArray();
        }

        private static object ResolveMemberExpression(Expression expression)
        {
            if (expression is MemberExpression)
            {
                return GetValue((MemberExpression)expression);
            }

            if (expression is UnaryExpression)
            {
                // if casting is involved, Expression is not x => x.FieldName but x => Convert(x.Fieldname)
                return GetValue((MemberExpression)((UnaryExpression)expression).Operand);
            }

            if (expression is ParameterExpression)
            {
                // if casting is involved, Expression is not x => x.FieldName but x => Convert(x.Fieldname)
                var value = Expression.Lambda(expression as ParameterExpression).Compile().DynamicInvoke();
                return value;
            }
            
            if (expression is LambdaExpression)
            {
                var lambdaExpression = expression as System.Linq.Expressions.LambdaExpression;
                return null;
            }

            try
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                return value;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private static object GetValue(MemberExpression exp)
        {
            // expression is ConstantExpression or FieldExpression
            if (exp.Expression is ConstantExpression)
            {
                return (((ConstantExpression)exp.Expression).Value)
                        .GetType()
                        .GetField(exp.Member.Name)
                        .GetValue(((ConstantExpression)exp.Expression).Value);
            }

            if (exp.Expression is MemberExpression)
            {
                return GetValue((MemberExpression)exp.Expression);
            }

            if (exp.Expression is MethodCallExpression)
            {
                try
                {
                    var value = Expression.Lambda(exp.Expression as MethodCallExpression).Compile().DynamicInvoke();
                    return value;
                } catch (Exception ex)
                {
                    ex.GetType();
                }
            }

            throw new NotImplementedException();
        }
        
        #endregion
        
        public static Task<TResult> GetRequestContext<TApplication, TResult>(this ITestApplication<TApplication> application, HttpMethod method,
            Func<HttpRequestMessage, RequestContext, Task<HttpResponseMessage>> callback)
            where TApplication : EastFive.Api.Azure.AzureApplication
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

        private static Task<TResult> GetRequestContext<TApplication, TResult>(this ITestApplication<TApplication> application, HttpMethod method,
            Func<Type, TResult> onOtherResponse,
            Func<HttpRequestMessage, RequestContext, Task<HttpResponseMessage>> callback) 
            where TApplication : EastFive.Api.Azure.AzureApplication
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
                        onAlreadyExists = () => new AttachedHttpResponseMessage<TResult>(
                            onOtherResponse(typeof(Controllers.AlreadyExistsResponse)), default(HttpResponseMessage)),
                        onRelationshipAlreadyExists = (id) => new AttachedHttpResponseMessage<TResult>(
                            onOtherResponse(typeof(Controllers.AlreadyExistsReferencedResponse)), default(HttpResponseMessage)),
                        onReferencedDocumentNotFound = () => new AttachedHttpResponseMessage<TResult>(
                            onOtherResponse(typeof(Controllers.ReferencedDocumentNotFoundResponse)), default(HttpResponseMessage)),
                        //onReferencedDocumentDoesNotExists = () => new AttachedHttpResponseMessage<TResult>(
                        //    onOtherResponse(typeof(Controllers.ReferencedDocumentDoesNotExistsResponse)), default(HttpResponseMessage)),
                        onUnauthorized = () => new AttachedHttpResponseMessage<TResult>(
                            onOtherResponse(typeof(Controllers.ReferencedDocumentNotFoundResponse)), default(HttpResponseMessage)),
                        onGeneralConflict = (why) => new AttachedHttpResponseMessage<TResult>(
                            onOtherResponse(typeof(Controllers.ReferencedDocumentNotFoundResponse)), default(HttpResponseMessage)),
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

        public static Task<TResult> PostAsync<TApplication, TResult>(this ITestApplication<TApplication> application,
                Func<RequestContext,
                    Controllers.CreatedResponse,
                    Task<HttpResponseMessage>> operation,
                Func<TResult> onCreated)
            where TApplication : EastFive.Api.Azure.AzureApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Post,
                (request, context) => operation(
                        context,
                        onCreated.CreatedResponse(request)));
        }

        public static Task<TResult> PostAsync<TApplication, TResource, TResult>(this ITestApplication<TApplication> application,
                Expression<Func<RequestContext,
                    Controllers.CreatedResponse,
                    Task<HttpResponseMessage>>> operation,
                Func<TResource, TResult> onCreated,
                Func<Type, TResult> onOtherResponse)
            where TApplication : EastFive.Api.Azure.AzureApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Post,
                onOtherResponse,
                (request, context) => operation.Compile()(
                        context,
                        onCreated.CreatedResponse(request, operation)));
        }
        
        public static Task<TResult> GetMultipartSpecifiedAsync<TResource, TApplication, TResult>(this ITestApplication<TApplication> application,
                Func<RequestContext,
                    EastFive.Api.Controllers.MultipartAcceptArrayResponseAsync,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse)
            where TApplication : EastFive.Api.Azure.AzureApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Get,
                (request, context) => operation(context, onResponse.MultipartAcceptArrayResponseAsync(request)));
        }

        public static Task<TResult> GetByIdAsync<TResource, TApplication, TResult>(this ITestApplication<TApplication> application,
                Func<RequestContext,
                    Controllers.ContentResponse,
                    Controllers.NotFoundResponse,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource, TResult> onResponse,
                Func<HttpResponseMessage, TResult> onNotFound)
            where TApplication : EastFive.Api.Azure.AzureApplication
            where TResource : class
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.ContentResponse(request),
                    onNotFound.NotFoundResponse(request)));
        }

        public static Task<TResult> GetByRelatedAsync<TResource, TApplication, TResult>(this ITestApplication<TApplication> application,
                Func<RequestContext,
                    Controllers.MultipartAcceptArrayResponseAsync,
                    Controllers.ReferencedDocumentNotFoundResponse,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse,
                Func<HttpResponseMessage, TResult> onReferencedNotFound)
            where TApplication : EastFive.Api.Azure.AzureApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.MultipartAcceptArrayResponseAsync(request),
                    onReferencedNotFound.MultipartReferencedNotFoundResponse(request)));
        }

        public static Task<TResult> GetAllAsync<TResource, TApplication, TResult>(this ITestApplication<TApplication> application,
                Func<RequestContext,
                    Controllers.MultipartAcceptArrayResponseAsync,
                    Task<HttpResponseMessage>> operation,
                Func<HttpResponseMessage, TResource[], TResult> onResponse)
            where TApplication : EastFive.Api.Azure.AzureApplication
        {
            return application.GetRequestContext<TApplication, TResult>(HttpMethod.Get,
                (request, context) => operation(context,
                    onResponse.MultipartAcceptArrayResponseAsync(request)));
        }
    }
}
