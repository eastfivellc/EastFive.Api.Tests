using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public class RestApplicationFactory : ITestApplicationFactory
    {
        public ITestApplication GetAuthorizedSession(string token)
        {
            return new RestTestApplication(token);
        }

        public ITestApplication GetUnauthorizedSession()
        {
            return new RestTestApplication();
        }
    }
}
