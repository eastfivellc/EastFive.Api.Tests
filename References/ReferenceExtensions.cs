using EastFive.Extensions;
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
            where TType : struct
        {
            return new TestRefs<TType>(guids);
        }

        public static IRefOptional<TType> AsRefOptional<TType>(this Guid guid)
            where TType : struct
        {
            return guid.AsOptional().AsRefOptional<TType>();
        }

        public static IRefOptional<TType> AsRefOptional<TType>(this Guid? guid)
            where TType : struct
        {
            return new TestRefOptional<TType>(guid);
        }
    }
}
