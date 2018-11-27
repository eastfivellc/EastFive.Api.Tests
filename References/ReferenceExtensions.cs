using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public static class ReferenceExtensions
    {
        public static IRef<TType> AsRef<TType>(this Guid guid)
            where TType : struct
        {
            return new TestRef<TType>(guid);
        }

        public static IRef<TType> AsRef<TType>(this TType type)
            where TType : struct, IReferenceable
        {
            return new TestRef<TType>(type.id);
        }

        public static IRefs<TType> AsRefs<TType>(this Guid [] guids)
        {
            return new TestRefs<TType>(guids);
        }
    }
}
