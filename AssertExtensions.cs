using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Api.Tests
{
    public static class AssertExtensions
    {
        public static HttpResponseMessage AssertSuccessPut(this HttpResponseMessage response)
        {
            if (HttpStatusCode.Accepted != response.StatusCode &&
                HttpStatusCode.OK != response.StatusCode &&
                HttpStatusCode.NoContent != response.StatusCode)
            {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Status code: [{0}]\rReason:{1}",
                    response.StatusCode, response.ReasonPhrase);
            }
            return response;
        }

        public static async Task AssertSuccessPutAsync(this Task<HttpResponseMessage> responseTask)
        {
            var response = await responseTask;
            response.AssertSuccessPut();
        }

        public static HttpResponseMessage AssertSuccessDelete(this HttpResponseMessage response)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(
                HttpStatusCode.Accepted == response.StatusCode ||
                HttpStatusCode.NoContent == response.StatusCode ||
                HttpStatusCode.OK == response.StatusCode,
                response.ReasonPhrase);
            return response;
        }

        public static async Task AssertSuccessDeleteAsync(this Task<HttpResponseMessage> responseTask)
        {
            var response = await responseTask;
            response.AssertSuccessDelete();
        }

        public static HttpResponseMessage Assert(this HttpResponseMessage response, HttpStatusCode responseStatusCode)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(
                responseStatusCode, response.StatusCode, response.ReasonPhrase);
            return response;
        }

        public static async Task<HttpResponseMessage> AssertAsync(this Task<HttpResponseMessage> responseTask, HttpStatusCode responseStatusCode)
        {
            var response = await responseTask;
            if (response.StatusCode != responseStatusCode)
            {
                var reason = $"Status code: [{response.StatusCode}]\rReason:{response.ReasonPhrase}";
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(
                    responseStatusCode, response.StatusCode, reason);
            }
            return await responseTask;
        }

        public static void AssertToMinute(this DateTime time1, DateTime time2)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(time1.DayOfYear, time2.DayOfYear);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(time1.Hour, time2.Hour);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(time1.Minute, time2.Minute);
        }

        public static void AssertContains<T>(this IEnumerable<T> items, Func<T, bool> comparison)
        {
            foreach(var item in items)
            {
                if (comparison(item))
                    return;
            }
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Items does not contain item");
        }

        public static void AssertAll<T>(this IEnumerable<T> items, Func<T, bool> assertValid)
        {
            foreach (var item in items)
            {
                if (!assertValid(item))
                    Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Item {0} is not valid", item.ToString());
            }
        }

        public static Resources.WebId AssertEquals(this Resources.WebId item1, Resources.WebId item2)
        {
            if(default(Resources.WebId) == item1)
            {
                if(default(Resources.WebId) != item2)
                    Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"{item1} not equal to {item2.UUID}");
                return item1;
            }

            if (default(Resources.WebId) == item2)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"{item1.UUID} not equal to {item2}");

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(item1.UUID, item2.UUID);
            return item1;
        }

        public static T AssertIdEquals<T>(this T resource, Resources.WebId item2)
            where T : BlackBarLabs.Api.ResourceBase
        {
            resource.Id.AssertEquals(item2);
            return resource;
        }
        public static T1 AssertPropertyEquals<T1, T2>(this T1 resource1,
                T1 resource2,
                Func<T1, T2> propertyDefinition)
            where T1 : BlackBarLabs.Api.ResourceBase
        {
            var value1 = propertyDefinition(resource1);
            var value2 = propertyDefinition(resource2);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(value1, value2);
            return resource1;
        }
    }
}
