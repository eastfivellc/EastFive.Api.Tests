using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests.Mocks
{
    public static class ProvideLoginExtensions
    {
        public static IQueryable<EastFive.Api.Tests.Redirection> Where(
            this IQueryable<EastFive.Api.Tests.Redirection> redirections,
            string userKey, Guid stateId)
        {
            var token = ProvideLoginMock.GetToken(userKey);
            return redirections
                .Where(redir => redir.state == stateId)
                .Where(redir => redir.token == token);
        }

        public static IQueryable<EastFive.Api.Tests.Redirection> Where(
            this IQueryable<EastFive.Api.Tests.Redirection> redirections, string userKey)
        {
            var token = ProvideLoginMock.GetToken(userKey);
            return redirections
                .Where(redir => redir.token == token);
        }
    }
}
