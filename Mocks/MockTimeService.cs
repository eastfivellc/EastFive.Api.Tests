using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Api.Tests.Mocks
{
    public class MockTimeService : EastFive.Api.Services.ITimeService
    {
        private TimeSpan offset;
        public MockTimeService()
        {
            offset = TimeSpan.FromSeconds(0);
        }

        public MockTimeService(DateTime pretendNow)
        {
            offset = pretendNow - DateTime.UtcNow;
        }

        public DateTime Utc
        {
            get
            {
                return DateTime.UtcNow + offset;
            }
        }

        public void UpdateTime(DateTime pretendNow)
        {
            offset = pretendNow - DateTime.UtcNow;
        }
    }
}
