using System;
using System.IO;
using System.Reflection;

namespace NetRx
{
    public class HotLoader : IDisposable
    {
        public Assembly LastLoaded { get; private set; }
        public FileSystemWatcher FileWatcher { get; private set; }
            
        public delegate void HotLoadEvent(Assembly prevAssembly, Assembly newAssembly);
        public event HotLoadEvent OnHotLoadOccurred;

        public HotLoader(string hotDll)
        {
            FileWatcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(hotDll),
                Filter = Path.GetFileName(hotDll),
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };
        }

        public void Watch()
        {
            FileWatcher.Changed += (sender, args) => HotLoad(args.FullPath);
            HotLoad(Path.Combine(FileWatcher.Path, FileWatcher.Filter));
        }

        public void Dispose()
        {
            FileWatcher?.Dispose();
        }
        
        private void HotLoad(string filePath)
        {
            Log("Hot Loading " + Path.GetFileName(filePath));
            
            var newLoaded = Assembly.Load(File.ReadAllBytes(filePath));

            OnHotLoadOccurred?.Invoke(LastLoaded, newLoaded);
            LastLoaded = newLoaded;
        }
        private static void Log(object o) => Console.WriteLine("[HotLoader] " + o);
    }
}