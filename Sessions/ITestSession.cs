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
        [Obsolete]
        Task<TResult> GetAsync<TController, TResult>(
                Func<HttpResponseMessage, TResult> callback)
            where TController : ApiController;
        
        Task<HttpResponseMessage> GetAsync<TController>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController;

        [Obsolete]
        Task<TResult> GetAsync<TController, TResult>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController;

        Task<TResult> GetAsync<TController, TResult>(object resource,
                HttpActionDelegate<object, TResult> callback)
            where TController : ApiController;

        Task<HttpResponseMessage> PostAsync<TController>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController;
        
        Task<HttpResponseMessage> PostAsync<T1>(Func<T1, Task<HttpResponseMessage>> action, object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>));

        Task<HttpResponseMessage> PostAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, Task<HttpResponseMessage>> action, object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>));

        Task<HttpResponseMessage> PostAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, Task<HttpResponseMessage>> action,
            object resource,
            T1 param1 = default(T1),
            T1 param2 = default(T1),
            T1 param3 = default(T1),
            Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>));

        Task<HttpResponseMessage> PostAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, Task<HttpResponseMessage>> action, object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>));

        Task<HttpResponseMessage> PostMultipartAsync<TController>(Action<MultipartContent> multipartContentCallback)
            where TController : ApiController;

        Task<HttpResponseMessage> PutAsync<TController>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController;

        Task<HttpResponseMessage> PutMultipartAsync<TController>(Action<MultipartContent> multipartContentCallback)
            where TController : ApiController;

        Task<HttpResponseMessage> DeleteAsync<TController>(object resource,
                Action<HttpRequestMessage> mutateRequest = default(Action<HttpRequestMessage>))
            where TController : ApiController;
        
        Task<TResult> OptionsAsync<TController, TResult>(object resource,
                Func<HttpResponseMessage, HttpMethod[], TResult> callback)
            where TController : ApiController;

        Task<TResult> OptionsAsync<TController, TResult>(
                Func<HttpResponseMessage, HttpMethod[], TResult> callback)
            where TController : ApiController;

        Dictionary<string, string> Headers { get; set; }

        T GetRequestPropertyFetch<T>(string propertyKey);

        void UpdateRequestPropertyFetch<T>(string propertyKey, T propertyValue, out T currentValue);
    }
}
