using BlackBarLabs.Api.Resources;
using BlackBarLabs.Api.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public static class AssertApi
    {
        public static void StatusCodeIs(HttpStatusCode statusCode, HttpResponseMessage response)
        {
            response.Assert(statusCode);
        }

        public static void Created(HttpResponseMessage response)
        {
            StatusCodeIs(HttpStatusCode.Created, response);
        }

        public static void Accepted(HttpResponseMessage response)
        {
            StatusCodeIs(HttpStatusCode.Accepted, response);
        }

        public static void Conflict(HttpResponseMessage response)
        {
            StatusCodeIs(HttpStatusCode.Conflict, response);
        }

        public static void NotFound(HttpResponseMessage response)
        {
            StatusCodeIs(HttpStatusCode.NotFound, response);
        }

        public static HttpResponseMessage Success(HttpResponseMessage response)
        {
            Assert.IsTrue(response.IsSuccessStatusCode, response.ReasonPhrase);
            return response;
        }
        
        public static Task<TResult> Created<TResource, TResult>(
            Func<HttpActionDelegate<TResource, TResult>, Task<TResult>> action,
            HttpActionDelegate<TResource, TResult> callback)
        {
            return action(
                (response, resource) =>
                {
                    StatusCodeIs(HttpStatusCode.Created, response);
                    return callback(response, resource);
                });
        }

        public static WebId AreEqual(WebId expected, WebId actual)
        {
            expected.AssertEquals(actual);
            return actual;
        }
    }
}
