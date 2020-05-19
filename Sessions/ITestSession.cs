using EastFive.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace BlackBarLabs.Api.Tests
{
    public interface ITestSession
    {
        Task<IHttpResponse> GetAsync<TController>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>));

        Task<TResult> GetAsync<TController, TResult>(object resource,
                HttpActionDelegate<object, TResult> callback);

        Task<IHttpResponse> PostAsync<TController>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>));
        
        Task<IHttpResponse> PostMultipartAsync<TController>(Action<MultipartContent> multipartContentCallback);

        Task<IHttpResponse> PutAsync<TController>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>));

        Task<IHttpResponse> PutMultipartAsync<TController>(Action<MultipartContent> multipartContentCallback);

        Task<IHttpResponse> DeleteAsync<TController>(object resource,
                Action<IHttpRequest> mutateRequest = default(Action<IHttpRequest>));
        
        Task<TResult> OptionsAsync<TController, TResult>(object resource,
                Func<IHttpResponse, HttpMethod[], TResult> callback);

        Task<TResult> OptionsAsync<TController, TResult>(
                Func<IHttpResponse, HttpMethod[], TResult> callback);

        Dictionary<string, string> Headers { get; set; }

        T GetRequestPropertyFetch<T>(string propertyKey);

        void UpdateRequestPropertyFetch<T>(string propertyKey, T propertyValue, out T currentValue);
    }
}
