using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImmediateReflection;

namespace NetRx
{
    public static class AssemblyExtensions
    {
        public static Dictionary<string, Assembly> MostRecentlyLoaded { get; } = new Dictionary<string, Assembly>();
        public static List<FileSystemWatcher> DllWatchers { get; } = new List<FileSystemWatcher>();
        
        public delegate void HotLoadEvent(Assembly prevAssembly, Assembly newAssembly);
        public static event HotLoadEvent LoadOccurred;

        /// <summary>
        /// Watches a file with type .hotdll next to Assembly for changes and triggers a hot load event when that is updated.
        /// </summary>
        /// <param name="assembly">The assembly to look next to/for to watch for changes.</param>
        /// <param name="onLoad">The event handler to be invoked when the assembly is loaded.</param>
        /// <param name="invokeOnLoad">Specifies whether to invoke the <c>LoadOccurred</c> event when the assembly is initially loaded.</param>
        public static void Watch(this Assembly assembly, HotLoadEvent onLoad, bool invokeOnLoad = true)
        {
            var filePath = assembly.Location.Replace(".dll", ".hotdll");
            Log("Creating Watcher for " + Path.GetFileName(filePath));
            
            MostRecentlyLoaded[assembly.GetName().Name] = assembly;
            OnLoad(assembly, onLoad);
            if (invokeOnLoad) 
                LoadOccurred?.Invoke(null, assembly);
            
            DllWatchers.Add(new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(filePath),
                Filter = Path.GetFileName(filePath),
                EnableRaisingEvents = true, 
                NotifyFilter = NotifyFilters.LastWrite
            });
            DllWatchers[DllWatchers.Count - 1].Changed += (sender, args) => HotLoad(args.FullPath);
        }

        public static void OnLoad(this Assembly assem, HotLoadEvent @event)
        {
            LoadOccurred += (assemFrom, assemTo) =>
            {
                if (assemTo.GetName().Name == assem.GetName().Name)
                    @event.Invoke(assemFrom, assemTo);
            };
        }

        public delegate object MethodCall(params object[] args);
        /// <summary>
        /// Provides a wrapper to get & then invoke a method on an instance contained within the <see cref="ObjectWrapper"/>
        /// </summary>
        /// <param name="wrapper">This wrapper</param>
        /// <param name="methodName">The name of the method to get.</param>
        /// <returns></returns>
        public static MethodCall Method(this ObjectWrapper wrapper, string methodName) => 
            args => wrapper.Type
                .GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                ?.Invoke(wrapper.Object, args);

        public delegate THot MemberGet<out THot>();
        public delegate void MemberSet<in THot>(THot value);
        /// <summary>
        /// Provides a Get/Set option for searching for Properties or Fields w/ Type
        /// </summary>
        /// <param name="wrapper">This wrapper.</param>
        /// <param name="memberName">The property or field name to search for.</param>
        /// <typeparam name="THot">The type of the member or properties value.</typeparam>
        /// <returns>A <see cref="ValueTuple{T1}"/> that contains a .Get <see cref="MemberGet{THot}"/> and .Set <see cref="MemberSet{THot}"/>.</returns>
        public static (MemberGet<THot> Get, MemberSet<THot> Set) Member<THot>(this ObjectWrapper wrapper, string memberName)
        {
            var member = wrapper[memberName];
            switch (member)
            {
                case ImmediateField field:
                    return new ValueTuple<MemberGet<THot>, MemberSet<THot>>(
                        () => (THot)field.GetValue(wrapper.Object),
                        value => field.SetValue(wrapper.Object, value));
                case ImmediateProperty property:
                    return new ValueTuple<MemberGet<THot>, MemberSet<THot>>(
                        () => (THot)property.GetValue(wrapper.Object),
                        value => property.SetValue(wrapper.Object, value));
                default: return default(ValueTuple<MemberGet<THot>, MemberSet<THot>>);
            }
        }

        /// <summary>
        /// Provides an efficient way to create objects utilizing <see cref="ImmediateReflection"/>.
        /// </summary>
        /// <param name="assem">The assembly to get the type from.</param>
        /// <param name="args">Any arguments to pass to the constructor.</param>
        /// <typeparam name="THot">The type to get the <see cref="Type.FullName"/> from.</typeparam>
        /// <returns></returns>
        public static ObjectWrapper CreateHotType<THot>(this Assembly assem, params object[] args) =>
            new ObjectWrapper(assem.GetHotType<THot>().New(args));
        /// <summary>
        /// Gets the Type from the Assembly with the FullName that matches typeof(THot)
        /// </summary>
        /// <param name="assem"></param>
        /// <typeparam name="THot"></typeparam>
        /// <returns></returns>
        public static ImmediateType GetHotType<THot>(this Assembly assem) =>
            TypeAccessor.Get(assem.GetType(typeof(THot).FullName ?? ""));
        /// <summary>
        /// Gets the last Assembly loaded by the short Name of the assembly.
        /// </summary>
        /// <param name="assem">Assembly to acquire the latest copy of from <see cref="MostRecentlyLoaded"/></param>
        /// <returns>The most recently loaded assembly with a matching name.</returns>
        public static Assembly LastLoaded(this Assembly assem) =>
            MostRecentlyLoaded.TryGetValue(assem.GetName().Name, out var ret) ? ret : assem;

        
        private static void HotLoad(string filePath)
        {
            var assem = Assembly.Load(File.ReadAllBytes(filePath));
            var key = assem.GetName().Name;
            
            if (MostRecentlyLoaded.TryGetValue(key, out var value))
                Log("Reloading " + Path.GetFileName(filePath).Replace(".hotdll", ".dll"));

            MostRecentlyLoaded[key] = assem;

            LoadOccurred?.Invoke(value, assem);
        }
        private static void Log(object o) => Console.WriteLine("[NetRx] " + o);

    }
}