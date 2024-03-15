using System.IO;
using System.Reflection;

namespace NetRx
{
    public static class AssemblyExtensions
    {
        public static HotLoader CreateHotLoader(this Assembly assembly, string hotDllFile) => 
            new HotLoader(Path.Combine(Path.GetDirectoryName(assembly.Location) ?? "", hotDllFile));
    }
}