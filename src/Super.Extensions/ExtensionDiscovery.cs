using System.Reflection;

namespace Super.Extensions;

/// <summary>
/// Finds and registers <see cref="IActivityExtension"/> implementations automatically, so a no-code
/// host (UiPath) only needs the single <see cref="SuperInitialize"/> activity. It scans loaded
/// assemblies and (once) the application base directory for <c>Super.Extensions.*.dll</c> so installed
/// extension packages are picked up even if none of their types were referenced directly.
/// </summary>
internal static class ExtensionDiscovery
{
    private static bool _diskScanned;

    public static void RegisterDiscovered()
    {
        EnsureCandidateAssembliesLoaded();

        var existing = SuperExtensions.All.Select(e => e.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var type in EnumerateExtensionTypes())
        {
            try
            {
                if (Activator.CreateInstance(type) is IActivityExtension extension && existing.Add(extension.Name))
                {
                    SuperExtensions.Register(extension);
                }
            }
            catch (Exception ex)
            {
                SuperRuntime.Log($"failed to create extension {type.FullName}: {ex.Message}", System.Diagnostics.TraceEventType.Warning);
            }
        }
    }

    private static IEnumerable<Type> EnumerateExtensionTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type?[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type is { IsAbstract: false, IsInterface: false }
                    && typeof(IActivityExtension).IsAssignableFrom(type)
                    && type.GetConstructor(Type.EmptyTypes) is not null)
                {
                    yield return type;
                }
            }
        }
    }

    private static void EnsureCandidateAssembliesLoaded()
    {
        if (_diskScanned)
        {
            return;
        }
        _diskScanned = true;

        // Scan both the app base directory and the folder this assembly was loaded from (UiPath places a
        // package's DLLs together), so installed extension packages are found even if unreferenced.
        foreach (var dir in CandidateDirectories())
        {
            try
            {
                foreach (var dll in Directory.EnumerateFiles(dir, "Super.*.dll"))
                {
                    // Skip design-time WPF assemblies: they have no runtime extensions and may not load
                    // outside Studio (e.g. on a Linux robot).
                    if (dll.EndsWith(".Design.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        var name = AssemblyName.GetAssemblyName(dll).Name;
                        if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase)))
                        {
                            Assembly.LoadFrom(dll);
                        }
                    }
                    catch
                    {
                        // Ignore individual files that can't be loaded.
                    }
                }
            }
            catch
            {
                // Directory not enumerable; loaded-assembly scan still applies.
            }
        }
    }

    private static IEnumerable<string> CandidateDirectories()
    {
        var dirs = new List<string?>
        {
            AppContext.BaseDirectory,
            Path.GetDirectoryName(typeof(ExtensionDiscovery).Assembly.Location),
        };
        return dirs.Where(d => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase)!;
    }
}
