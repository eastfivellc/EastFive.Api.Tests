using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests
{
    public static class TestSessionHelpers
    {
        public static void MockViewResponse(this HttpApplication session)
        {
            //session.SetInstigator(
            //    typeof(EastFive.Api.Controllers.ViewRenderer),
            //    (httpApp, request, paramInfo, success) =>
            //    {
            //        EastFive.Api.Controllers.ViewRenderer dele =
            //        (filePath, content) =>
            //        {
            //            return "<html><head><title>TEST MOCK</title></head><body>Hello World</body></html>";
            //        };
            //        return success((object)dele);
            //    });


            session.AddOrUpdateInstantiation(
                typeof(EastFive.Api.ViewPathResolver),
                (app) =>
                {
                    EastFive.Api.ViewPathResolver callback =
                        (viewName) =>
                        {
                            var loadedAssemblies = Assembly.GetAssembly(typeof(TestSessionHelpers));
                            var apiDllLocation = new FileInfo(loadedAssemblies.Location);
                            var apiProjectDir = apiDllLocation.Directory
                                .Parent
                                .Parent
                                .Parent
                                .GetDirectories().First(d => d.Name.EndsWith("PDMS.Api"))
                                .FullName;
                            var viewFileInfo = new FileInfo(Path.Combine(apiProjectDir, viewName.TrimStart(new char[] { '\\', '~', '/' })));

                            return viewFileInfo.FullName;
                        };
                    return callback.AsTask<object>();
                });
        }
    }
}
