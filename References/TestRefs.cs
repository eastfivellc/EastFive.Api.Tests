using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EastFive.Linq.Async;

namespace EastFive.Api.Tests
{
    public class TestRefs<TType> : IRefs<TType>
    {
        public TestRefs(Guid[] ids)
        {
            this.ids = ids;
        }

        public Guid[] ids
        {
            get;
            private set;
        }

        public IEnumerableAsync<TType> Values => throw new NotImplementedException();
    }
}
