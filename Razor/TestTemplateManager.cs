using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Tests.Razor
{
    public class TestTemplateManager : RazorEngine.Templating.ITemplateManager
    {
        public TestTemplateManager()
        {
        }

        public RazorEngine.Templating.ITemplateSource Resolve(RazorEngine.Templating.ITemplateKey key)
        {
            var viewName = key.Name;
            var loadedAssemblies = Assembly.GetAssembly(typeof(EastFive.Api.Tests.TestSessionHelpers));
            var apiDllLocation = new FileInfo(loadedAssemblies.Location);
            var apiProjectDir = apiDllLocation.Directory
                .Parent
                .Parent
                .Parent
                .GetDirectories()
                .First(
                    d =>
                    {
                        if (!d.Name.EndsWith(".Api"))
                            return false;
                        if (d.Name.StartsWith("EastFive"))
                            return false;
                        if (d.Name.StartsWith("Black"))
                            return false;
                        return true;
                    })
                .FullName;
            var viewFileInfo = new FileInfo(Path.Combine(apiProjectDir, viewName.TrimStart(new char[] { '\\', '~', '/' })));

            var path = viewFileInfo.FullName;
            string content = File.ReadAllText(path);
            return new RazorEngine.Templating.LoadedTemplateSource(content, path);
        }

        public RazorEngine.Templating.ITemplateKey GetKey(string name, RazorEngine.Templating.ResolveType resolveType, RazorEngine.Templating.ITemplateKey context)
        {
            return new RazorEngine.Templating.NameOnlyTemplateKey(name, resolveType, context);
        }

        public void AddDynamic(RazorEngine.Templating.ITemplateKey key, RazorEngine.Templating.ITemplateSource source)
        {
            throw new NotImplementedException("dynamic templates are not supported!");
        }
    }
}
