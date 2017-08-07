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
        public static void AssertStatusCode(HttpStatusCode statusCode, HttpResponseMessage response)
        {
            response.Assert(statusCode);
        }

        public static void AssertCreated(HttpResponseMessage response)
        {
            AssertStatusCode(HttpStatusCode.Created, response);
        }

        public static void AssertAccepted(HttpResponseMessage response)
        {
            AssertStatusCode(HttpStatusCode.Accepted, response);
        }

        public static void AssertConflict(HttpResponseMessage response)
        {
            AssertStatusCode(HttpStatusCode.Conflict, response);
        }

        public static void AssertSuccess(HttpResponseMessage response)
        {
            Assert.IsTrue(response.IsSuccessStatusCode, response.ReasonPhrase);
        }

        public static Task<TResult> AssertCreated<TResource, TResult>(
            Func<HttpActionDelegate<TResource, TResult>, Task<TResult>> action,
            HttpActionDelegate<TResource, TResult> callback)
        {
            return action(
                (response, resource) =>
                {
                    AssertStatusCode(HttpStatusCode.Created, response);
                    return callback(response, resource);
                });
        }
    }
}
