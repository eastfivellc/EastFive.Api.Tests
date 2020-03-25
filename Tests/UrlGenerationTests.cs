using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using EastFive.Extensions;
using EastFive;
using EastFive.Azure.Functions;

namespace EastFive.Api.Tests
{
    [TestClass]
    public class UrlGeneration
    {
        [TestMethod]
        public void GenerateUrls()
        {
            var httpApp = InvokeTestApplication.Init();
            var queryId = Guid.NewGuid();
            var queryIdStr = queryId.ToString("N");

            var query = httpApp
                .GetRequest<QueryableResource>()
                .ById(queryId)
                //.Where(qr => qr.queryableRef == queryId.AsRef<QueryableResource>())
                .Where(qr => qr.text == "match")
                .Where(qr => !qr.dtOptional.HasValue)
                .Location();
            Assert.AreEqual(
                $"http://example.com/api/MyQueryableResource/{queryIdStr}?string=match&dtOptional=null",
                query.AbsoluteUri);

            //var invocationMessage = await httpApp
            //    .GetRequest<QueryableResource>()
            //    .Where(qr => qr.queryableRef == queryId.AsRef<QueryableResource>())
            //    .FunctionAsync();
        }

        [FunctionViewController4(
            Route = "MyQueryableResource",
            Resource = typeof(QueryableResource),
            ContentType = "x-application/queryableresource",
            ContentTypeVersion = "0.1")]
        public struct QueryableResource : IReferenceable
        {
            [JsonIgnore]
            public Guid id => queryableRef.id;

            public const string QueryablePropertyName = "id";
            [ApiProperty(PropertyName = QueryablePropertyName)]
            [JsonProperty(PropertyName = QueryablePropertyName)]
            public IRef<QueryableResource> queryableRef;

            public const string DtOptionalPropertyName = "dtOptional";
            [ApiProperty(PropertyName = DtOptionalPropertyName)]
            [JsonProperty(PropertyName = DtOptionalPropertyName)]
            public DateTime? dtOptional;

            public const string StringPropertyName = "string";
            [ApiProperty(PropertyName = StringPropertyName)]
            [JsonProperty(PropertyName = StringPropertyName)]
            public string text;

            public const string GuidPropertyName = "guid";
            [ApiProperty(PropertyName = GuidPropertyName)]
            [JsonProperty(PropertyName = GuidPropertyName)]
            public Guid exampleId;

            public const string ValidPropertyName = "valid";
            [ApiProperty(PropertyName = ValidPropertyName)]
            [JsonProperty(PropertyName = ValidPropertyName)]
            public bool valid;

            public const string RefExternalPropertyName = "ref_external";
            [ApiProperty(PropertyName = RefExternalPropertyName)]
            [JsonProperty(PropertyName = RefExternalPropertyName)]
            public IRef<OtherResource> refExternal;

            public const string RefExternalMaybePropertyName = "ref_external_maybe";
            [ApiProperty(PropertyName = RefExternalMaybePropertyName)]
            [JsonProperty(PropertyName = RefExternalMaybePropertyName)]
            public IRefOptional<OtherResource> refExternalMaybe;

            public const string NumberPropertyName = "number";
            [ApiProperty(PropertyName = NumberPropertyName)]
            [JsonProperty(PropertyName = NumberPropertyName)]
            public int number;

            public const string NumberMaybePropertyName = "number_maybe";
            [ApiProperty(PropertyName = NumberMaybePropertyName)]
            [JsonProperty(PropertyName = NumberMaybePropertyName)]
            public int numberMaybe;
        }

        [FunctionViewController4(
            Route = "OtherResource",
            Resource = typeof(OtherResource),
            ContentType = "x-application/other-resource",
            ContentTypeVersion = "0.1")]
        public struct OtherResource : IReferenceable
        {
            [JsonIgnore]
            public Guid id => otherResourceRef.id;

            public const string OtherResourcePropertyName = "id";
            [ApiProperty(PropertyName = OtherResourcePropertyName)]
            [JsonProperty(PropertyName = OtherResourcePropertyName)]
            public IRef<OtherResource> otherResourceRef;
        }
    }
}
