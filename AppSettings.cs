using EastFive.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    [Config]
    public static class AppSettings
    {
        [ConfigKey("The url specified in the request for testing.",
            DeploymentOverrides.Optional,
            Location = "Any made up value for testing",
            MoreInfo = "This is not a real URL. It is just the URL the server sees when it access where the request was made.",
            DeploymentSecurityConcern = false)]
        public const string ServerUrl = "EastFive.Api.Test.ServerUrl";
        
        [ConfigKey("The name of route under which the MVC API controllers are routed for MVC",
            DeploymentOverrides.Optional,
            DeploymentSecurityConcern = false,
            MoreInfo = "Comma delimmited for multiple.")]
        public const string RoutesApi = "EastFive.Api.Test.RoutesApi";
        
        [ConfigKey("The name of route under which the MVC controllers are routed for MVC",
            DeploymentOverrides.Optional,
            DeploymentSecurityConcern = false,
            MoreInfo = "Comma delimmited for multiple.")]
        public const string RoutesMvc = "EastFive.Api.Test.RoutesMvc";
    }
}
