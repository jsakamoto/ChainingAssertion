using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    internal static class AppDomain
    {
        internal static class CurrentDomain
        {
            public static IEnumerable<Assembly> GetAssemblies()
            {
                var context = DependencyContext.Default;
                var runtimeLibNames = context.RuntimeLibraries.Select(lib => lib.Name);
                var compileLibNames = context.CompileLibraries.Select(lib => lib.Name);
                var libNames = runtimeLibNames.Union(compileLibNames);
                var assemblies = libNames.Select(libName =>
                {
                    try { return Assembly.Load(new AssemblyName(libName)); }
                    catch { return null; }
                })
                .Where(asm => asm != null);

                return assemblies;
            }
        }
    }
}
