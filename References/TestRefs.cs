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
            if (objectType.IsSubClassOfGeneric(typeof(IRefOptional<>)))
                return true;
            if (objectType.IsSubClassOfGeneric(typeof(IDictionary<,>)))
                return true;
            if (objectType.IsSubclassOf(typeof(Type)))
                return true;
            if (objectType.IsSubClassOfGeneric(typeof(IRef<>)))
                return true;
            if (objectType.IsSubClassOfGeneric(typeof(IRefObj<>)))
                return true;
            if (objectType.IsSubClassOfGeneric(typeof(IRefOptional<>)))
                return true;
            if (objectType.IsSubClassOfGeneric(typeof(IRefObjOptional<>)))
                return true;
            if (objectType.IsSubClassOfGeneric(typeof(IRefs<>)))
                return true;
            // THis doesn't work because it will serialize the whole object as a single GUID
            //if (objectType.IsSubClassOfGeneric(typeof(IReferenceable)))
            //    return true;
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Guid GetGuid()
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var guidString = reader.Value as string;
                    return Guid.Parse(guidString);
                }
                throw new Exception();
            }

            Guid? GetGuidMaybe()
            {
                if (reader.TokenType == JsonToken.Null)
                    return default(Guid?);
                return GetGuid();
            }

            Guid[] GetGuids()
            {
                if (reader.TokenType == JsonToken.Null)
                    return new Guid[] { };

                IEnumerable<Guid> Enumerate()
                {
                    while (reader.TokenType != JsonToken.EndArray)
                    {
                        if (!reader.Read())
                            yield break;
                        var guidStr = reader.ReadAsString();
                        yield return Guid.Parse(guidStr);
                    }
                }
                return Enumerate().ToArray();
            }

            if (objectType.IsSubClassOfGeneric(typeof(IReferenceable)))
            {
                
                if (objectType.IsSubClassOfGeneric(typeof(IRef<>)))
                {
                    var id = GetGuid();
                    var refType = typeof(TestRef<>).MakeGenericType(objectType.GenericTypeArguments);
                    return Activator.CreateInstance(refType, id);
                }
            }

            if (objectType.IsSubClassOfGeneric(typeof(IReferenceableOptional)))
            {
                if (objectType.IsSubClassOfGeneric(typeof(IRefOptional<>)))
                {
                    var id = GetGuidMaybe();
                    var refType = typeof(TestRefOptional<>).MakeGenericType(objectType.GenericTypeArguments);
                    return Activator.CreateInstance(refType, id);
                }
            }

            if (objectType.IsSubClassOfGeneric(typeof(IReferences)))
            {
                if (objectType.IsSubClassOfGeneric(typeof(IRefs<>)))
                {
                    var ids = GetGuids();
                    var refType = typeof(TestRefs<>).MakeGenericType(objectType.GenericTypeArguments);
                    return Activator.CreateInstance(refType, ids);
                }
            }
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // THis doesn't work because it will serialize the whole object as a single GUID if (value is IReferenceable)
            if(value.GetType().IsSubClassOfGeneric(typeof(IRef<>)))
            {
                var id = (value as IReferenceable).id;
                writer.WriteValue(id);
            }
            if (value.GetType().IsSubClassOfGeneric(typeof(IRefObj<>)))
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
            if (value is IReferenceableOptional)
            {
                var id = (value as IReferenceableOptional).id;
                writer.WriteValue(id);
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

    public class TestRefObj<TType> : IRefObj<TType>
        where TType : class
    {
        public TestRefObj(Guid id)
        {
            this.id = id;
        }

        public Guid id
        {
            get;
            private set;
        }

        public bool resolved => false;

        Func<TType> IRefObj<TType>.value => throw new NotImplementedException();

        public Task ResolveAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class TestRefs<TType> : IRefs<TType>
        where TType : struct
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

        public IRef<TType>[] refs
        {
            get
            {
                return ids.Select(id => new TestRef<TType>(id)).ToArray();
            }
        }
    }

    public class TestRefOptional<TType> : IRefOptional<TType>
        where TType : struct
    {
        public TestRefOptional(Guid? id)
        {
            this.id = id;
        }

        public Guid? id
        {
            get;
            private set;
        }

        public IRef<TType> Ref
        {
            get
            {
                if (!this.HasValue)
                    throw new Exception("Attempt to de-option empty value");
                return new TestRef<TType>(this.id.Value);
            }
        }

        public TType? value => throw new NotImplementedException();

        public bool resolved => false;

        public bool HasValue => this.id.HasValue;

        public Task ResolveAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class TestRefObjOptional<TType> : IRefObjOptional<TType>
        where TType : class
    {
        public TestRefObjOptional(Guid? id)
        {
            this.id = id;
        }

        public Guid? id
        {
            get;
            private set;
        }

        public IRefObj<TType> Ref
        {
            get
            {
                if (!this.HasValue)
                    throw new Exception("Attempt to de-option empty value");
                return new TestRefObj<TType>(this.id.Value);
            }
        }

        public bool resolved => false;

        public bool HasValue => this.id.HasValue;

        TType IRefObjOptional<TType>.value => throw new NotImplementedException();

        public Task ResolveAsync()
        {
            throw new NotImplementedException();
        }
    }
}
