using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackBarLabs.Web;
using EastFive.Api.Services;

namespace BlackBarLabs.Api.Tests
{
    public class MockMailService : EastFive.Api.Services.ISendMessageService
    {
        public delegate Task SendEmailMessageDelegate(string templateName, string toAddress, string toName, string fromAddress, string fromName,
            string subject, IDictionary<string, string> substitutionsSingle);

        public SendEmailMessageDelegate SendEmailMessageCallback { get; set; }
        
        public async Task<TResult> SendEmailMessageAsync<TResult>(string templateName, string toAddress, string toName, string fromAddress, string fromName,
                string subject, IDictionary<string, string> substitutionsSingle,
                IDictionary<string, IDictionary<string, string>[]> substituationsMultiple,
            Func<string, TResult> onSuccess,
            Func<TResult> onServiceUnavailable,
            Func<string, TResult> onFailed)
        {
            await this.SendEmailMessageCallback.Invoke(templateName,
                toAddress, toName, fromAddress, fromName,
                subject, substitutionsSingle);
            return onSuccess(Guid.NewGuid().ToString());
        }

        public Task<SendMessageTemplate[]> ListTemplatesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
