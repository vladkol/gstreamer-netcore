namespace Gst
{
#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml.Linq;

    partial class Application
    {
        internal class ModuleInitializer
        {
            static ModuleInitializer()
            {
                NativeLibMapper.InitNativeLibMapping();
            }
        }

        private static readonly ModuleInitializer _initializer = new ModuleInitializer();
    }

    internal static class NativeLibMapper
    {
        private static XElement mappingDocument = null;
        public static void InitNativeLibMapping()
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), MapAndLoad);
        }

        private static IntPtr MapAndLoad(string libraryName, Assembly assembly, DllImportSearchPath? dllImportSearchPath)
        {
            string mappedName = null;
            if(mappingDocument == null)
            {
                using(var stream = assembly.GetManifestResourceStream("Gst.gstreamer_sharp.sources.gstreamer-sharp.dll.config"))
                {
                    mappingDocument = XElement.Load(stream);
                }
            }

            mappedName = MapLibraryName(assembly.Location, libraryName, out mappedName) ? mappedName : libraryName;

            IntPtr handle = IntPtr.Zero;
            if(!NativeLibrary.TryLoad(mappedName, assembly, dllImportSearchPath, out handle))
            {
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if(mappedName.StartsWith("lib", true, null))
                    {
                        NativeLibrary.TryLoad(mappedName.Substring(3), assembly, dllImportSearchPath, out handle);
                    }
                    else
                    {
                        NativeLibrary.TryLoad($"lib{mappedName}", assembly, dllImportSearchPath, out handle);
                    }
                }
            }
            return handle;
        }

        private static bool MapLibraryName(string assemblyLocation, string originalLibName, out string mappedLibName)
        {
            mappedLibName = null;
            
            if (mappingDocument == null)
            {
                return false;
            }

            string os = "linux";
            if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                os = "osx";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = "windows";
            }

            XElement root = mappingDocument;
            var map =
                (from el in root.Elements("dllmap")
                 where (string)el.Attribute("dll") == originalLibName 
                 && (string)el.Attribute("os") == os
                 select el).FirstOrDefault();

            if (map != null)
                mappedLibName = map.Attribute("target").Value;

            return (mappedLibName != null);
        }
    }
#endif
}