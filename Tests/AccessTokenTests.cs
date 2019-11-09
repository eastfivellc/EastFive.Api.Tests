using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Routing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using EastFive.Extensions;
using EastFive;
using EastFive.Azure.Functions;
using System.Threading;

namespace EastFive.Api.Tests
{
    [TestClass]
    public class AccessTokenTests
    {
        [TestMethod]
        public void AccessTokenAccountWorks()
        {
            var httpApp = InvokeTestApplication.Init();
            var queryId = Guid.NewGuid();

            var query = httpApp
                .GetRequest<UrlGeneration.QueryableResource>()
                .ById(queryId)
                .Where(qr => qr.text == "match")
                .Where(qr => !qr.dtOptional.HasValue)
                .Location();
            var sessionId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var duration = TimeSpan.FromSeconds(15);
            var expirationUtc = DateTime.UtcNow + duration;
            Assert.IsTrue(query.SignWithAccessTokenAccount(sessionId, accountId, expirationUtc,
                (signedUrl) =>
                {
                    Assert.IsTrue(signedUrl.ValidateAccessTokenAccount(
                        accessToken =>
                        {
                            Assert.AreEqual(sessionId, accessToken.sessionId);
                            Assert.AreEqual(accountId, accessToken.accountId);
                            Assert.IsTrue(Math.Abs((expirationUtc - accessToken.expirationUtc).TotalSeconds) < 2);
                            return true;
                        },
                        () => false,
                        () => false));

                    Assert.IsTrue(query.ValidateAccessTokenAccount(
                        (accessToken) => false,
                        onAccessTokenNotProvided: () => true,
                        onAccessTokenInvalid: () => false,
                        onAccessTokenExpired: () => false,
                        onInvalidSignature: () => false));

                    var changedUrl = signedUrl.SetQueryParam(UrlGeneration.QueryableResource.StringPropertyName, "match_diff");
                    Assert.IsTrue(changedUrl.ValidateAccessTokenAccount(
                        (accessToken) => false,
                        onAccessTokenNotProvided: () => false,
                        onAccessTokenInvalid: () => false,
                        onAccessTokenExpired: () => false,
                        onInvalidSignature:() => true));

                    Thread.Sleep(duration + TimeSpan.FromSeconds(1));
                    Assert.IsTrue(signedUrl.ValidateAccessTokenAccount(
                        (accessToken) => false,
                        onAccessTokenNotProvided: () => false,
                        onAccessTokenInvalid: () => false,
                        onAccessTokenExpired: () => true,
                        onInvalidSignature: () => false));

                    return true;
                }));
        }
    }
}
