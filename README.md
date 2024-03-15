
# <Center>NetRx</Center>
<Center>Offers a way for developers to Hot-Reload their NET Standard 2.1+ & NET Framework 4.7.1+ DLLs from a primary project.</Center>

## Getting Started
Head to the Releases page and download the latest release, these are the necessary dependencies you will need to reference in your project <b><u>and</u></b> have resolved in your runtime environment (Generally this means loaded as a dependency and next to your mod). 

Your main project should reference those downloaded Dlls. You should also create a second project of an identical framework to your main project and reference your main project <u>from</u> the secondary project.

#### Main Project
In my example I've created this interface in my Main Project that my secondary project's class will inherit from, this allows me to cast newly loaded types back down to a common interface.
```csharp
public interface IHotLoadClass
{
    public int TestProp { get; }
    public string TestString { get; set; }
    public void Dispose();
    public void Test();
}
```

#### Secondary Project
In my secondary project I'll implement the interface I've referenced from my main project.
```csharp
public class HotLoadable : IHotLoadClass
{
    private void Log(object o) => Console.WriteLine("[HotLoadable] " + o);
    
    public int TestProp { get; set; } = -1;
    public string TestString { get; set; } = "Hello";
    public void Dispose()
    {
        Log("Dispose called! 1");
    }

    public void Test()
    {
        Log("Test called! 1");
    }
}
```

#### Main Project
Finally, back in my Main Project, somewhere I want to initialize my other DLL watcher, I can create a new `HotLoader` class one of two ways:
```csharp
HotLoader hotLoader = new HotLoader(string pathToHotDll);
```
Or with the shorthand that will use the relative path next to your assembly (This assumes <b><u>TypeInMainAssembly</u></b> exists in your Main Project)
```csharp
HotLoader hotLoader = typeof(TypeInMainAssembly).Assembly.CreateHotLoader(string fileNameWithExtension)
```

Then we can bind to the event for HotLoading.
```csharp
IHotLoadClass HotLoadClass = null;
hotLoader.OnHotLoadOccurred += (prevAssembly, newAssembly) =>
{
    // prevAssembly is null the first load.
    Log(prevAssembly != null ? "Hot reload detected!" : "New load detected!");
    // Dispose of old if it exists
    HotLoadClass?.Dispose();
    // Try to get any IHotLoadClass from the new DLL
    var hotLoadImplementation = newAssembly.DefinedTypes.FirstOrDefault(typeInfo =>
        typeof(IHotLoadClass).IsAssignableFrom(typeInfo));
    if (hotLoadImplementation != null)
        // Create new hotLoadImplementation
        HotLoadClass = (IHotLoadClass)Activator.CreateInstance(hotLoadImplementation);

    // Call some test method if it exists
    HotLoadClass?.Test();
};
```
And actually start watching for updates
```csharp
hotLoader.Watch();
```

### Finally
The Secondary Project should be setup to copy it's dll to the runtime directory with any extension *except* `.dll` as some runtimes might auto-load it, therefore locking the file. I've chosen the extension `.hotdll` for personal use. 

Now when you build your second project the `*.hotdll` will be copied over and detected by the hotLoader, which will load it by bytes and provide that and the prior assembly to the `OnHotLoadOccurred` event.