using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackBarLabs.Web;

namespace BlackBarLabs.Api.Tests
{
    public class MockMailService : EastFive.Api.Services.ISendMessageService
    {
        public delegate Task SendEmailMessageDelegate(string toAddress, string toName, string fromAddress, string fromName,
            string templateName, IDictionary<string, string> substitutionsSingle, IDictionary<string, string[]> substitutionsMultiple);

        public SendEmailMessageDelegate SendEmailMessageCallback { get; set; }
        
        public async Task<TResult> SendEmailMessageAsync<TResult>(string toAddress, string toName, string fromAddress, string fromName,
            string templateName, IDictionary<string, string> substitutionsSingle, IDictionary<string, string[]> substitutionsMultiple, Func<string, TResult> onSuccess, Func<TResult> onServiceUnavailable, Func<string, TResult> onFailed)
        {
            await this.SendEmailMessageCallback.Invoke(
                toAddress, toName, fromAddress, fromName,
                templateName, substitutionsSingle, substitutionsMultiple);
            return onSuccess(Guid.NewGuid().ToString());
        }
    }
}
