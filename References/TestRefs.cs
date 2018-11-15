using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EastFive.Linq.Async;
using EastFive.Reflection;
using Newtonsoft.Json;

namespace EastFive.Api.Tests
{
    public class RefConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsSubClassOfGeneric(typeof(TestRef<>)))
                return true;
            if (objectType.IsSubClassOfGeneric(typeof(TestRefs<>)))
                return true;
            if (objectType.IsSubClassOfGeneric(typeof(IDictionary<,>)))
                return true;
            if (objectType.IsSubclassOf(typeof(Type)))
                return true;
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return (existingValue as IReferenceable).id;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IReferenceable)
            {
                var id = (value as IReferenceable).id;
                writer.WriteValue(id);
            }
            if (value is IReferences)
            {
                writer.WriteStartArray();
                Guid[] ids = (value as IReferences).ids
                    .Select(
                        id =>
                        {
                            writer.WriteValue(id);
                            return id;
                        })
                    .ToArray();
                writer.WriteEndArray();
            }
            if (value.GetType().IsSubClassOfGeneric(typeof(IDictionary<,>)))
            {
                writer.WriteStartObject();
                foreach (var kvpObj in value.DictionaryKeyValuePairs())
                {
                    var keyValue = kvpObj.Key;
                    var propertyName = (keyValue is IReferenceable)?
                        (keyValue as IReferenceable).id.ToString("N")
                        :
                        keyValue.ToString();
                    writer.WritePropertyName(propertyName);

                    var valueValue = kvpObj.Value;
                    writer.WriteValue(valueValue);
                }
                writer.WriteEndObject();
            }
            if (value is Type)
            {
                var stringType = (value as Type).GetClrString();
                writer.WriteValue(stringType);
            }
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
