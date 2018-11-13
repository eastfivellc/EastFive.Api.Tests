using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EastFive.Linq.Async;
using Newtonsoft.Json;

namespace EastFive.Api.Tests
{
    public class RefConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var canConvert = objectType.IsSubClassOfGeneric(typeof(TestRef<>));
            return canConvert;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return (existingValue as IReferenceable).id;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var id = (value as IReferenceable).id;
            writer.WriteValue(id);
        }
    }

    public class TestRef<TType> : IRef<TType>
        where TType : struct
    {
        public TestRef(Guid id)
        {
            this.id = id;
        }

        public Guid id
        {
            get;
            private set;
        }

        public TType? value => throw new NotImplementedException();

        public bool resolved => false;
        
        public Task ResolveAsync()
        {
            throw new NotImplementedException();
        }
    }

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
