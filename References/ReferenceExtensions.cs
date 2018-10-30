using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public static class ReferenceExtensions
    {
        public static IRefs<TType> AsRefs<TType>(this Guid [] guids)
        {
            return new TestRefs<TType>(guids);
        }
    }
}
