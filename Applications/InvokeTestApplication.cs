using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

namespace EastFive.Api.Tests
{
    public class InvokeTestApplication : InvokeApplicationDirect
    {
        public override string[] ApiRoutes
        {
            get
            {
                var routesApiCSV = EastFive.Web.Configuration.Settings.GetString(
                    EastFive.Api.Tests.AppSettings.RoutesApi,
                (routesApiFound) => routesApiFound,
                (why) => "DefaultApi");

                var routesApi = routesApiCSV.Split(new[] { ',' }).ToArray();
                return routesApi;
            }
        }

        public override string[] MvcRoutes
        {
            get
            {
                var routesMvcCSV = EastFive.Web.Configuration.Settings.GetString(
                        EastFive.Api.Tests.AppSettings.RoutesMvc,
                    (routesApiFound) => routesApiFound,
                    (why) => "Default");

                return routesMvcCSV.Split(new[] { ',' });
            }
        }

        protected override IApplication Application => application;
        private IApplication application;

        protected InvokeTestApplication(IApplication application, Uri serverUrl) : base(serverUrl)
        {
            this.application = application;
        }

        public static InvokeTestApplication Init()
        {
            var application = new HttpApplication();

            var hostingLocation = Web.Configuration.Settings.GetUri(
                    AppSettings.ServerUrl,
                (hostingLocationFound) => hostingLocationFound,
                (whyUnspecifiedOrInvalid) => new Uri("http://example.com"));
            return new InvokeTestApplication(application, hostingLocation);
        }
    }
}
