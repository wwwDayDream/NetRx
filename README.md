# NetRx
Offers a way for developers to Hot-Reload their NET Standard 2.1+ & NET Framework 4.7.1+ DLLs from a primary project.

## Packaged With
NetRx is packaged with [ImmediateReflection](https://github.com/KeRNeLith/ImmediateReflection) licensed under the [MIT License](https://www.mit.edu/~amini/LICENSE.md), which is an amazing tool that speeds up Reflection calls drastically.
Given the nature of being unable to Type the newly loaded DLL's objects to the previous Type (The cast from new type to old type fails.), we have to use Reflection to access methods and members of said instances.  

## Getting Started
Head to the Releases page and download the latest release, these are the necessary dependencies you will need to reference in your project <b><u>and</u></b> have resolved in your runtime environment (Generally this means loaded as a dependency and next to your mod). 

You'll be creating a new project of the same TargetFramework as your Main Project. Content in here will be initially loaded as a reference but further builds to an alternate file will reload the new build into the AppDomain.

The Main Project should reference this new project's output dll or use a Project Reference if it exists within the same solution.
The Main Project and Secondary Project(s) should output their own DLL's to the runtime directory; <u>Additionally, the secondary project(s) should output a second copy of the file with the `.dll` replaced with `.hotdll`.</u> This "hot dll" will not be File Locked so you can overwrite it while the process is running, as opposed to the regular '.dll' that's loaded as a reference.

With a type created inside the new project you can access it's assembly and call the [Watch overload from `NetRx`](#extension-methods) which will look for the `.hotdll` located next to that assembly.

(Below we assume a type exists in this referenced project called "UpdateableClass" with a Dispose method and a constructor)
```csharp

ObjectWrapper TestClass;
typeof(UpdateableClass).Assembly.Watch((previousAssembly, newAssembly) =>
{
    // Use the Method overload for our ObjectWrapper(If it exists, hence the ?) to call the Dispose method.
    TestClass?.Method(nameof(UpdateableClass.Dispose))?.Invoke();
    // Use the CreateHotType overload to create a hot type from the new assembly. 
    TestClass = newAssembly.CreateHotType<UpdateableClass>();
    
    // Here you could use the Method overload or Member overload (for properties and fields) or even just fenangle the raw (object)ObjectWrapper.Object!
}
```

## Reference :: `static class AssemblyExtensions`
#### Properties

- `MostRecentlyLoaded`: A dictionary of all assemblies that have been loaded, tracked by `Assembly.GetName().Name`.
- `DllWatchers`: A list of all `FileSystemWatcher` instances that are monitoring `.hotdll` files.

```C#
public static Dictionary<string, Assembly> MostRecentlyLoaded { get; }
public static List<FileSystemWatcher> DllWatchers { get; }
```

#### Public Delegates

- `HotLoadEvent`: Defines an event to be raised when an assembly is loaded or re-loaded.

```C#
public delegate void HotLoadEvent(Assembly prevAssembly, Assembly newAssembly);
``` 

- `MethodCall`: Represents a method in an `ObjectWrapper`.

```C#
public delegate object MethodCall(params object[] args);
```

- `MemberGet` and `MemberSet`: Delegates which respectively gets and sets the value of a member on an instance.

```C#
public delegate THot MemberGet<out THot>();
public delegate void MemberSet<in THot>(THot value);
```

#### Extension Methods

- `Watch`: This method monitors a `.hotdll` file for changes and triggers an event when it is updated.

```C# 
public static void Watch(this Assembly assembly, HotLoadEvent onLoad, bool invokeOnLoad = true)
```

- `OnLoad`: This method invokes the passed `HotLoadEvent` when a matching `LoadOccurred` event is raised.
- This is automatically called by the `Watch` method with your `onLoad` entry.

```C# 
public static void OnLoad(this Assembly assem, HotLoadEvent @event)
```

- `Method`: This <u>returned MethodCall</u> gets and invokes a method on an instance wrapped in an `ObjectWrapper`.

```C# 
public static MethodCall Method(this ObjectWrapper wrapper, string methodName)
```

- `Member`: This method provides a Get/Set option for Properties or Fields wrapped in the ObjectWrapper.

```C#
public static (MemberGet<THot> Get, MemberSet<THot> Set) Member<THot>(this ObjectWrapper wrapper, string memberName)
```

- `CreateHotType`: This method creates an object by getting its type from the assembly by the given hot type `THot`'s FullName.

```C#
public static ObjectWrapper CreateHotType<THot>(this Assembly assem, params object[] args)
```

- `GetHotType`: Gets the `Type` from the Assembly that matches the fullname of `THot`.

```C#
public static ImmediateType GetHotType<THot>(this Assembly assem)
```

- `LastLoaded`: Returns the most recently loaded assembly with a matching name.

```C#
public static Assembly LastLoaded(this Assembly assem)
```