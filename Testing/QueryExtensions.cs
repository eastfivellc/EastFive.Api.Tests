using EastFive.Api.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public static class QueryExtensions
    {
        public static async Task<TResult> OnContentsAsync<TResource, TResult>(
            this IQueryable<TResource> requestQuery,
            HttpMethod method,
            Func<TResource[], TResult> onContents)
        {
            var response = await requestQuery
                .ApplyMethod(method)
                .SendAsync();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var request = (requestQuery as RequestMessage<TResource>);
            var httpApp = request.InvokeApplication as HttpApplication;
            var converter = new BindConvert(httpApp);
            var resources = JsonConvert.DeserializeObject<TResource[]>(jsonContent, converter);
            return onContents(resources);
        }

        private static IQueryable<TResource> ApplyMethod<TResource>(
            this IQueryable<TResource> urlQuery, HttpMethod method)
        {
            if (method.Method == HttpMethod.Get.Method)
                return urlQuery.HttpGet();
            if (method.Method == HttpMethod.Delete.Method)
                return urlQuery.HttpDelete();
            if (method.Method == HttpMethod.Options.Method)
                return urlQuery.HttpOptions();
            throw new ArgumentException($"Method `{method}` is not supported.");
        }

        private static IQueryable<TResource> ApplyMethod<TResource>(
            this IQueryable<TResource> urlQuery, HttpMethod method, TResource resource)
        {
            if (method.Method == HttpMethod.Post.Method)
                return urlQuery.HttpPost(resource);
            if (method.Method == HttpMethod.Put.Method)
                return urlQuery.HttpPut(resource);
            if (method.Method.ToLower() == "patch")
                return urlQuery.HttpPatch(resource);
            throw new ArgumentException($"Method `{method}` is not supported.");
        }
    }
}
