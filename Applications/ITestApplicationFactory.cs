using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public interface ITestApplicationFactory
    {
        ITestApplication GetUnauthorizedSession();

        ITestApplication GetAuthorizedSession(string token);
    }
}
