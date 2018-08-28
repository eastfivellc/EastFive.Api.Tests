using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public interface ITestApplication
    {
        Guid ActorId { get; }
        IDictionary<string, string> Headers { get; }
    }
}
