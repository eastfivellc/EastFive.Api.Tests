using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackBarLabs.Web;
using EastFive.Api.Services;
using BlackBarLabs.Extensions;
using EastFive.Web.Services;

namespace BlackBarLabs.Api.Tests
{
    public class MockMailService : ISendMessageService
    {
        public const string OrderTemplate1Name = "Order Template1";
        public const string OrderTemplate2Name = "Order Template2";

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
            return (new SendMessageTemplate[]
            {
                new SendMessageTemplate
                {
                    name = OrderTemplate1Name,
                    externalTemplateId = Guid.NewGuid().ToString("N"),
                },
                new SendMessageTemplate
                {
                    name = OrderTemplate2Name,
                    externalTemplateId = Guid.NewGuid().ToString("N"),
                },
            }).ToTask();
        }
    }
}
